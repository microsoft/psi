// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    using System;
    using System.Threading;

    /// <summary>
    /// A generic ordered queue that sorts items based on the specified Comparer.
    /// </summary>
    /// <typeparam name="T">Type of item in the list.</typeparam>
    public abstract class PriorityQueue<T> : IDisposable
    {
        // the head of the ordered work item list is always empty
        private readonly PriorityQueueNode head = new PriorityQueueNode(0);
        private readonly PriorityQueueNode emptyHead = new PriorityQueueNode(0);
        private readonly Comparison<T> comparer;
        private readonly ManualResetEvent empty = new ManualResetEvent(true);
        private IPerfCounterCollection<PriorityQueueCounters> counters;
        private int count;
        private int nextId;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueue{T}"/> class.
        /// </summary>
        /// <param name="name">Priority queue name.</param>
        /// <param name="comparer">Comparison function.</param>
        public PriorityQueue(string name, Comparison<T> comparer)
        {
            this.comparer = comparer;
        }

        /// <summary>
        /// Gets count of items in queue.
        /// </summary>
        public int Count => this.count;

        internal WaitHandle Empty => this.empty;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.empty.Dispose();
        }

        /// <summary>
        /// Try peeking at first item; returning indication of success.
        /// </summary>
        /// <param name="workitem">Work item populated if successful.</param>
        /// <returns>Indication of success.</returns>
        public bool TryPeek(out T workitem)
        {
            workitem = default(T);
            if (this.count == 0)
            {
                return false;
            }

            // lock the head and the first work item
            var previous = this.head;
            int retries = previous.Lock();
            var current = previous.Next;
            if (current == null)
            {
                return false;
            }

            current.Lock();
            workitem = current.Workitem;
            current.Release();
            previous.Release();

            return true;
        }

        /// <summary>
        /// Try to dequeue work item; returning indication of success.
        /// </summary>
        /// <param name="workitem">Work item populated if successful.</param>
        /// <param name="getAnyMatchingItem">Whether to match any item (or only first).</param>
        /// <returns>Indication of success.</returns>
        public bool TryDequeue(out T workitem, bool getAnyMatchingItem = true)
        {
            // keep looping until we either get a work item, or the list changed under us
            workitem = default(T);
            bool found = false;
            if (this.count == 0)
            {
                return false;
            }

            // as we traverse the list of nodes, we use two locks, on previous and current, to ensure consistency
            // start by taking a lock on the head first, which is immutable (not an actual work item)
            var previous = this.head;
            int retries = previous.Lock();
            var current = previous.Next;
            while (current != null)
            {
                retries += current.Lock();

                // we got the node, now see if it's ready
                if (this.DequeueCondition(current.Workitem))
                {
                    // save the work item
                    workitem = current.Workitem;
                    found = true;

                    // release the list
                    previous.Next = current.Next;

                    // add the node to the empty list
                    current.Workitem = default(T);
                    retries += this.emptyHead.Lock();
                    current.Next = this.emptyHead.Next;
                    this.emptyHead.Next = current;
                    this.emptyHead.Release();
                    current.Release();

                    this.counters?.Increment(PriorityQueueCounters.DequeueingCount);
                    this.counters?.Decrement(PriorityQueueCounters.WorkitemCount);
                    break;
                }

                // keep going through the list
                previous.Release();
                previous = current;
                current = current.Next;

                if (!getAnyMatchingItem)
                {
                    break;
                }
            }

            previous.Release();

            if (found && Interlocked.Decrement(ref this.count) == 0)
            {
                this.empty.Set();
            }

            this.counters?.IncrementBy(PriorityQueueCounters.DequeuingRetries, retries);
            return found;
        }

        /// <summary>
        /// Enqueue work item.
        /// </summary>
        /// <remarks>
        /// Enqueuing is O(n), but since we re-enqueue the oldest originating time many times as it is processed by the pipeline,
        /// the dominant operation is to enqueue at the beginning of the queue.
        /// </remarks>
        /// <param name="workitem">Work item to enqueue.</param>
        public void Enqueue(T workitem)
        {
            // reset the empty signal as needed
            if (this.count == 0)
            {
                this.empty.Reset();
            }

            // take the head of the empty list
            int retries = this.emptyHead.Lock();
            var newNode = this.emptyHead.Next;
            if (newNode != null)
            {
                this.emptyHead.Next = newNode.Next;
            }
            else
            {
                newNode = new PriorityQueueNode(Interlocked.Increment(ref this.nextId));
            }

            this.emptyHead.Release();

            newNode.Workitem = workitem;

            // insert it in the right place
            retries += this.Enqueue(newNode);
            this.counters?.Increment(PriorityQueueCounters.EnqueueingCount);
            this.counters?.Increment(PriorityQueueCounters.WorkitemCount);
            this.counters?.IncrementBy(PriorityQueueCounters.EnqueueingRetries, retries);
        }

        /// <summary>
        /// Enable performance counters.
        /// </summary>
        /// <param name="name">Instance name.</param>
        /// <param name="perf">Performance counters implementation (platform specific).</param>
        public void EnablePerfCounters(string name, IPerfCounters<PriorityQueueCounters> perf)
        {
            const string Category = "Microsoft Psi scheduler queue";

            if (this.counters != null)
            {
                throw new InvalidOperationException("Perf counters are already enabled for this scheduler");
            }

#pragma warning disable SA1118 // Parameter must not span multiple lines
            perf.AddCounterDefinitions(
                Category,
                new Tuple<PriorityQueueCounters, string, string, PerfCounterType>[]
                {
                    Tuple.Create(PriorityQueueCounters.WorkitemCount, "Workitem queue count", "The number of work items in the global queue", PerfCounterType.NumberOfItems32),
                    Tuple.Create(PriorityQueueCounters.EnqueuingTime, "Enqueuing time", "The time to enqueue a work item", PerfCounterType.NumberOfItems32),
                    Tuple.Create(PriorityQueueCounters.DequeueingTime, "Dequeuing time", "The time to dequeuing a work item", PerfCounterType.NumberOfItems32),
                    Tuple.Create(PriorityQueueCounters.EnqueueingRetries, "Enqueuing retry average", "The number of retries per work item enqueue operation.", PerfCounterType.AverageCount64),
                    Tuple.Create(PriorityQueueCounters.EnqueueingCount, "Enqueue count", "The base counter for computing the work item enqueuing retry count.", PerfCounterType.AverageBase),
                    Tuple.Create(PriorityQueueCounters.DequeuingRetries, "Dequeuing retry average", "The number of retries per work item dequeue operation.", PerfCounterType.AverageCount64),
                    Tuple.Create(PriorityQueueCounters.DequeueingCount, "Dequeue count", "The base counter for computing the work item enqueuing retry count.", PerfCounterType.AverageBase),
                });
#pragma warning restore SA1118 // Parameter must not span multiple lines

            this.counters = perf.Enable(Category, name);
        }

        /// <summary>
        /// Predicate function condition under which to dequeue.
        /// </summary>
        /// <param name="item">Candidate item.</param>
        /// <returns>Whether to dequeue.</returns>
        protected abstract bool DequeueCondition(T item);

        private int Enqueue(PriorityQueueNode node)
        {
            // we'll insert the node between "previous" and "next"
            var previous = this.head;
            int retries = previous.Lock();
            var next = this.head.Next;
            while (next != null && this.comparer(node.Workitem, next.Workitem) > 0)
            {
                retries += next.Lock();
                previous.Release();
                previous = next;
                next = previous.Next;
            }

            node.Next = previous.Next;
            previous.Next = node;

            // increment the count and signal the empty queue if needed, before releasing the previous node
            // If we didn't and this was a 0-1 transition of this.count, another thread could dequeue and go to -1,
            // we would still bring it back to 0, but we would miss signaling the empty queue.
            if (Interlocked.Increment(ref this.count) == 1)
            {
                this.empty.Reset();
            }

            previous.Release();
            return retries;
        }

#pragma warning disable SA1401 // Fields must be private
        private class PriorityQueueNode
        {
            public T Workitem;
            private readonly SynchronizationLock simpleLock;
            private PriorityQueueNode next;
            private int id;

            public PriorityQueueNode(int id)
            {
                this.id = id;
                this.simpleLock = new SynchronizationLock(this, false);
            }

            public PriorityQueueNode Next
            {
                get { return this.next; }

                set
                {
                    if (value != null && value.id == this.id)
                    {
                        throw new InvalidOperationException("A node is pointing to itself.");
                    }

                    this.next = value;
                }
            }

            public int Lock() => this.simpleLock.Lock();

            public void Release() => this.simpleLock.Release();
        }
#pragma warning restore SA1401 // Fields must be private
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi.Scheduling;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Represents a stream of messages.
    /// An emitter is similar to a .Net Event, in that it is used to propagate information to a set of subscriber that is only known at runtime.
    /// While a subscriber to an event is of type delegate, a subscriber to an emitter is of type <see cref="Receiver{T}"/> (which wraps a delegate).
    /// </summary>
    /// <typeparam name="T">The type of messages in the stream.</typeparam>
    [Serializer(typeof(Emitter<>.NonSerializer))]
    public class Emitter<T> : IEmitter, IProducer<T>
    {
        private readonly object owner;
        private readonly Pipeline pipeline;
        private readonly int id;
        private int nextSeqId;
        private SynchronizationLock syncContext;
        private Envelope lastEnvelope;
        private volatile Receiver<T>[] receivers = new Receiver<T>[0];
        private IPerfCounterCollection<EmitterCounters> counters;

        /// <summary>
        /// Initializes a new instance of the <see cref="Emitter{T}"/> class.
        /// This constructor is intended to be used by the framework.
        /// </summary>
        /// <param name="id">The id of this stream.</param>
        /// <param name="owner">The owning component</param>
        /// <param name="syncContext">The synchronization context this emitter operates in</param>
        /// <param name="pipeline">The pipeline to associate with</param>
        internal Emitter(int id, object owner, SynchronizationLock syncContext, Pipeline pipeline)
        {
            this.id = id;
            this.owner = owner;
            this.syncContext = syncContext;
            this.pipeline = pipeline;
        }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public int Id => this.id;

        /// <inheritdoc />
        public Pipeline Pipeline => this.pipeline;

        /// <summary>
        /// Gets a value indicating whether this emitter has subscribers.
        /// </summary>
        public bool HasSubscribers => this.receivers.Count() > 0;

        /// <inheritdoc />
        public object Owner => this.owner;

        /// <inheritdoc />
        Emitter<T> IProducer<T>.Out => this;

        internal SynchronizationLock SyncContext => this.syncContext;

        /// <summary>
        /// Close emitter (unsubscribing receivers).
        /// </summary>
        public void Close()
        {
            lock (this.receivers)
            {
                foreach (var receiver in this.receivers)
                {
                    receiver.OnUnsubscribe();
                }

                this.receivers = new Receiver<T>[0];
            }
        }

        /// <summary>
        /// Synchronously calls all subscribers.
        /// When the call returns, the message is assumed to be unchanged and reusable (that is, no downstream component is referencing it or any of its parts).
        /// </summary>
        /// <param name="message">The message to post</param>
        /// <param name="originatingTime">The time of the real-world event that led to the creation of this message</param>
        public void Post(T message, DateTime originatingTime)
        {
            var e = this.CreateEnvelope(originatingTime);
            this.Deliver(new Message<T>(message, e));
        }

        /// <inheritdoc />
        public string DebugView(string debugName = null)
        {
            return DebugExtensions.DebugView(this, debugName);
        }

        /// <summary>
        /// Enable performance counters.
        /// </summary>
        /// <param name="name">Instance name.</param>
        /// <param name="perf">Performance counters implementation (platform specific).</param>
        public void EnablePerfCounters(string name, IPerfCounters<EmitterCounters> perf)
        {
            const string Category = "Microsoft Psi message submission";

            if (this.counters != null)
            {
                throw new InvalidOperationException("Perf counters are already enabled for emitter " + this.Name);
            }

#pragma warning disable SA1118 // Parameter must not span multiple lines
            perf.AddCounterDefinitions(
                Category,
                new Tuple<EmitterCounters, string, string, PerfCounterType>[]
                {
                    Tuple.Create(EmitterCounters.MessageCount, "Total messages / second", "Number of messages received per second", PerfCounterType.RateOfCountsPerSecond32),
                    Tuple.Create(EmitterCounters.MessageLatency, "Message latency (microseconds)", "The end-to-end latency, from originating time to the time when processing completed.", PerfCounterType.NumberOfItems32),
                });
#pragma warning restore SA1118 // Parameter must not span multiple lines

            this.counters = perf.Enable(Category, name);
        }

        /// <summary>
        /// Allows a receiver to subscribe to messages from this emitter.
        /// </summary>
        /// <param name="receiver">The receiver subscribing to this emitter.</param>
        /// <param name="deliveryPolicy">The desired policy to use when delivering messages to the specified receiver.</param>
        internal void Subscribe(Receiver<T> receiver, DeliveryPolicy deliveryPolicy)
        {
            receiver.OnSubscribe(this, deliveryPolicy);

            lock (this.receivers)
            {
                var newSet = this.receivers.Concat(new[] { receiver }).ToArray();
                this.receivers = newSet;
            }
        }

        internal void Unsubscribe(Receiver<T> receiver)
        {
            lock (this.receivers)
            {
                var newSet = this.receivers.Except(new[] { receiver }).ToArray();
                this.receivers = newSet;
            }

            receiver.OnUnsubscribe();
        }

        internal int GetNextId()
        {
            return Interlocked.Increment(ref this.nextSeqId);
        }

        internal void Deliver(T data, Envelope e)
        {
            e.SourceId = this.Id;
            this.Deliver(new Message<T>(data, e));
        }

        private void Deliver(Message<T> msg)
        {
            if (this.lastEnvelope.SequenceId != 0)
            {
                // make sure the data is consistent
                if (msg.Envelope.OriginatingTime < this.lastEnvelope.OriginatingTime || msg.Envelope.Time < this.lastEnvelope.Time || msg.Envelope.SequenceId <= this.lastEnvelope.SequenceId)
                {
                    throw new InvalidOperationException("Attempted to post a message that is out of order: " + this.Name);
                }
            }

            this.lastEnvelope = msg.Envelope;

            if (this.counters != null)
            {
                this.counters.Increment(EmitterCounters.MessageCount);
                this.counters.RawValue(EmitterCounters.MessageLatency, (msg.Time - msg.OriginatingTime).Ticks / 10);
            }

            // capture the "receivers" member to avoid locking
            var activeSet = this.receivers;
            foreach (var rec in activeSet)
            {
                rec.Receive(msg);
            }
        }

        private Envelope CreateEnvelope(DateTime originatingTime)
        {
            return new Envelope(originatingTime, this.pipeline.GetCurrentTime(), this.Id, this.GetNextId());
        }

        private class NonSerializer : NonSerializer<Emitter<T>>
        {
        }
    }
}

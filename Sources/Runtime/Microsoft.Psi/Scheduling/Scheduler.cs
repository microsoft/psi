// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    using System;
    using System.Threading;

    /// <summary>
    /// Maintains a queue of workitems and schedules worker threads to empty them.
    /// </summary>
    public sealed class Scheduler
    {
        private readonly string name;
        private readonly SimpleSemaphore threadSemaphore;
        private readonly Func<Exception, bool> errorHandler;
        private readonly bool allowSchedulingOnExternalThreads;
        private readonly ManualResetEvent stopped = new ManualResetEvent(true);
        private readonly AutoResetEvent futureAdded = new AutoResetEvent(false);

        // the queue of pending workitems, ordered by start time
        private readonly WorkItemQueue globalWorkitems;
        private readonly FutureWorkItemQueue futureWorkitems;
        private Thread futuresThread;
        private IPerfCounterCollection<SchedulerCounters> counters;
        private bool forcedShutdownRequested;
        private ThreadLocal<WorkItem?> nextWorkitem = new ThreadLocal<WorkItem?>();
        private ThreadLocal<bool> isSchedulerThread = new ThreadLocal<bool>(() => false);
        private ThreadLocal<DateTime> currentWorkitemTime = new ThreadLocal<DateTime>(() => DateTime.MaxValue);
        private Clock clock;
        private bool delayFutureWorkitemsUntilDue;
        private DateTime finalizeTime = DateTime.MaxValue;
        private bool started = false;
        private bool completed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        /// <param name="errorHandler">Error handler function.</param>
        /// <param name="threadCount">Scheduler thread count.</param>
        /// <param name="allowSchedulingOnExternalThreads">Allow scheduling on external threads.</param>
        /// <param name="name">Scheduler name.</param>
        /// <param name="clock">Optional clock to use (defaults to virtual time offset of DateTime.MinValue and dilation factor of 0).</param>
        public Scheduler(Func<Exception, bool> errorHandler, int threadCount = 0, bool allowSchedulingOnExternalThreads = false, string name = "default", Clock clock = null)
        {
            this.name = name;
            this.errorHandler = errorHandler;
            this.threadSemaphore = new SimpleSemaphore((threadCount == 0) ? Environment.ProcessorCount * 2 : threadCount);
            this.allowSchedulingOnExternalThreads = allowSchedulingOnExternalThreads;
            this.globalWorkitems = new WorkItemQueue(name);
            this.futureWorkitems = new FutureWorkItemQueue(name + "_future", this);

            // set virtual time such that any scheduled item appears to be in the future and gets queued in the future workitem queue
            // the time will change when the scheduler is started, and the future workitem queue will be drained then as appropriate
            this.clock = clock ?? new Clock(DateTime.MinValue, 0);
            this.delayFutureWorkitemsUntilDue = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scheduler"/> class.
        /// </summary>
        /// <param name="parent">Parent scheduler from which to inherit settings.</param>
        internal Scheduler(Scheduler parent)
            : this(parent.errorHandler, parent.threadSemaphore.MaxThreadCount, parent.allowSchedulingOnExternalThreads, $"Sub{parent.name}", new Clock(parent.clock))
        {
        }

        /// <summary>
        /// Gets the scheduler clock.
        /// </summary>
        public Clock Clock => this.clock;

        internal bool IsStarted
        {
            get
            {
                return !this.stopped.WaitOne(0);
            }
        }

        /// <summary>
        /// Attempts to execute the delegate immediately, on the calling thread, without scheduling.
        /// </summary>
        /// <typeparam name="T">Action argument type</typeparam>
        /// <param name="synchronizationObject">Synchronization object</param>
        /// <param name="action">Action to execute</param>
        /// <param name="argument">Action argument</param>
        /// <param name="startTime">Scheduled start time</param>
        /// <returns>Success flag</returns>
        public bool TryExecute<T>(SynchronizationLock synchronizationObject, Action<T> action, T argument, DateTime startTime)
        {
            if (this.forcedShutdownRequested || startTime > this.finalizeTime)
            {
                return true;
            }

            if (startTime > this.clock.GetCurrentTime() && this.delayFutureWorkitemsUntilDue)
            {
                return false;
            }

            if (!this.isSchedulerThread.Value && !this.allowSchedulingOnExternalThreads)
            {
                // this thread is not ours, so return
                return false;
            }

            // try to acquire a lock on the sync context
            // however, if this thread already has the lock, we have to give up to keep the no-reentrancy guarantee
            if (!TryGetExclusiveLock(synchronizationObject))
            {
                return false;
            }

            try
            {
                action(argument);
                this.counters?.Increment(SchedulerCounters.WorkitemsPerSecond);

                this.counters?.Increment(SchedulerCounters.ImmediateWorkitemsPerSecond);
            }
            catch (Exception e) when (this.errorHandler(e))
            {
            }
            finally
            {
                ReleaseExclusiveLock(synchronizationObject);
            }

            return true;
        }

        /// <summary>
        /// Attempts to execute the delegate immediately, on the calling thread, without scheduling.
        /// </summary>
        /// <param name="synchronizationObject">Synchronization object</param>
        /// <param name="action">Action to execute</param>
        /// <param name="startTime">Scheduled start time</param>
        /// <returns>Success flag</returns>
        public bool TryExecute(SynchronizationLock synchronizationObject, Action action, DateTime startTime)
        {
            if (this.forcedShutdownRequested || startTime > this.finalizeTime)
            {
                return true;
            }

            if (startTime > this.clock.GetCurrentTime() && this.delayFutureWorkitemsUntilDue)
            {
                return false;
            }

            if (!this.isSchedulerThread.Value && !this.allowSchedulingOnExternalThreads)
            {
                // this thread is not ours, so return
                return false;
            }

            // try to acquire a lock
            // however, if this thread already has the lock, we have to give up to keep the no-reentrancy guarantee
            if (!TryGetExclusiveLock(synchronizationObject))
            {
                return false;
            }

            this.ExecuteAndRelease(synchronizationObject, action);
            this.counters?.Increment(SchedulerCounters.ImmediateWorkitemsPerSecond);
            return true;
        }

        /// <summary>
        /// Enqueues a workitem and, if possible, kicks off a worker thread to pick it up
        /// </summary>
        /// <param name="synchronizationObject">Synchronization object</param>
        /// <param name="action">Action to execute</param>
        /// <param name="startTime">Scheduled start time</param>
        /// <param name="asContinuation">Flag whether to execute once current operation completes</param>
        /// <param name="allowSchedulingPastFinalization">Allow scheduling past finalization time.</param>
        public void Schedule(SynchronizationLock synchronizationObject, Action action, DateTime startTime, bool asContinuation = true, bool allowSchedulingPastFinalization = false)
        {
            if (this.forcedShutdownRequested || (startTime > this.finalizeTime && !allowSchedulingPastFinalization) || this.completed)
            {
                return;
            }

            if (synchronizationObject == null || action == null)
            {
                throw new ArgumentNullException();
            }

            WorkItem wi = new WorkItem() { SyncLock = synchronizationObject, Callback = action, StartTime = startTime };

            // if the workitem not yet due, add it to the future workitem queue
            if ((startTime > this.clock.GetCurrentTime() && this.delayFutureWorkitemsUntilDue) || !this.started)
            {
                this.futureWorkitems.Enqueue(wi);
                this.futureAdded.Set();
                return;
            }

            // try to kick off another thread now, without going through the global queue
            if (TryGetExclusiveLock(synchronizationObject))
            {
                if (this.threadSemaphore.TryEnter())
                {
                    // there are threads available, which means the items in the global queue, if any, are locked
                    // start a thread to do the work
                    this.counters?.Increment(SchedulerCounters.ImmediateWorkitemsPerSecond);
                    ThreadPool.QueueUserWorkItem(this.Run, wi);
                    return;
                }

                ReleaseExclusiveLock(synchronizationObject);
            }

            // try to schedule on the local thread, to be executed once the current operation finishes, as long as the new workitem time is the same or less as the current one
            if (asContinuation && this.isSchedulerThread.Value && this.nextWorkitem.Value == null && wi.StartTime <= this.currentWorkitemTime.Value)
            {
                // we own the thread, so schedule the work in the local queue
                this.nextWorkitem.Value = wi;
                this.counters?.Increment(SchedulerCounters.LocalQueueCount);
                return;
            }

            // last resort, add the workitem to the global queue
            this.globalWorkitems.Enqueue(wi);

            // if a thread became available, it might have missed the workitem being enqueued, so retry to make sure
            if (this.threadSemaphore.TryEnter())
            {
                if (this.globalWorkitems.TryDequeue(out wi))
                {
                    this.counters?.Increment(SchedulerCounters.GlobalWorkitemsPerSecond);
                    ThreadPool.QueueUserWorkItem(this.Run, wi);
                }
                else
                {
                    this.threadSemaphore.Exit();
                }
            }
        }

        /// <summary>
        /// Prevent further scheduling.
        /// </summary>
        /// <param name="synchronizationObject">Synchronization object</param>
        public void Freeze(SynchronizationLock synchronizationObject)
        {
            synchronizationObject.Hold();
        }

        /// <summary>
        /// Allow further scheduling.
        /// </summary>
        /// <param name="synchronizationObject">Synchronization object</param>
        public void Thaw(SynchronizationLock synchronizationObject)
        {
            synchronizationObject.Release();
        }

        /// <summary>
        /// Start scheduler.
        /// </summary>
        /// <param name="clock">Scheduler clock.</param>
        /// <param name="delayFutureWorkitemsUntilDue">Delay future workitems until scheduled time.</param>
        public void Start(Clock clock, bool delayFutureWorkitemsUntilDue)
        {
            if (!this.stopped.WaitOne(0))
            {
                throw new InvalidOperationException("The scheduler was already started.");
            }

            // if no clock is specified, schedule everything without delay
            this.delayFutureWorkitemsUntilDue = delayFutureWorkitemsUntilDue;
            this.clock = clock;
            this.started = true;
            this.stopped.Reset();
            this.StartFuturesThread();
        }

        /// <summary>
        /// Reject any new scheduling and block until running threads finish their current work.
        /// </summary>
        /// <remarks>Assumes source components have been shut down.</remarks>
        /// <param name="abandonPendingWorkitems">Whether to abandon queued/future workitems</param>
        public void Stop(bool abandonPendingWorkitems = false)
        {
            if (this.stopped.WaitOne(0))
            {
                return;
            }

            this.forcedShutdownRequested = abandonPendingWorkitems;
            this.stopped.Set();
            this.futuresThread.Join();
            this.clock = new Clock(DateTime.MinValue, 0);
            this.completed = true;
            this.delayFutureWorkitemsUntilDue = true;
        }

        /// <summary>
        /// Enable performance counters.
        /// </summary>
        /// <param name="name">Instance name.</param>
        /// <param name="perf">Performance counters implementation (platform specific).</param>
        public void EnablePerfCounters(string name, IPerfCounters<SchedulerCounters> perf)
        {
            const string Category = "Microsoft Psi scheduler";

            if (this.counters != null)
            {
                throw new InvalidOperationException("Perf counters are already enabled for this scheduler");
            }

#pragma warning disable SA1118 // Parameter must not span multiple lines
            perf.AddCounterDefinitions(
                Category,
                new Tuple<SchedulerCounters, string, string, PerfCounterType>[]
                {
                    Tuple.Create(SchedulerCounters.LocalToGlobalPromotions, "Local-to-global promotions", "The percentage of workitems promoted to the global queue", PerfCounterType.AverageCount64),
                    Tuple.Create(SchedulerCounters.LocalQueueCount, "Local workitem count", "The number of messages in the thread-local queues", PerfCounterType.AverageBase),
                    Tuple.Create(SchedulerCounters.WorkitemsPerSecond, "Workitems / second", "The number of workitems executed per second", PerfCounterType.RateOfCountsPerSecond32),
                    Tuple.Create(SchedulerCounters.GlobalWorkitemsPerSecond, "Global workitems / second", "The number of workitems from the global queue executed per second", PerfCounterType.RateOfCountsPerSecond32),
                    Tuple.Create(SchedulerCounters.LocalWorkitemsPerSecond, "Local workitems / second", "The number of workitems from the thread-local queues executed per second", PerfCounterType.RateOfCountsPerSecond32),
                    Tuple.Create(SchedulerCounters.ImmediateWorkitemsPerSecond, "Immediate workitems / second", "The number of workitems executed synchronously without enqueuing, per second", PerfCounterType.RateOfCountsPerSecond32),
                    Tuple.Create(SchedulerCounters.ActiveThreads, "Active threads", "The count of active threads", PerfCounterType.NumberOfItems32)
                });
#pragma warning restore SA1118 // Parameter must not span multiple lines

            this.counters = perf.Enable(Category, name);
        }

        /// <summary>
        /// Pause scheduler until all queues have drained.
        /// </summary>
        internal void PauseForQuiescence()
        {
            if (this.stopped.WaitOne(0) || !this.started || this.completed)
            {
                return;
            }

            // let queues drain
            this.stopped.Set();
            try
            {
                this.futuresThread.Join();
            }
            catch (ThreadStateException)
            {
                // thread never started?
            }
        }

        /// <summary>
        /// Resume scheduler after having paused for quiescence.
        /// </summary>
        internal void ResumeAfterQuiescence()
        {
            if (this.stopped.WaitOne(0) && this.started && !this.completed)
            {
                this.stopped.Reset();
                this.StartFuturesThread();
            }
        }

        /// <summary>
        /// Notify scheduler of latest message time enqueued or scheduled.
        /// </summary>
        /// <param name="finalizeTime">Originating time after which messages will no longer be scheduled.</param>
        internal void NotifyPipelineFinalizing(DateTime finalizeTime)
        {
            this.finalizeTime = finalizeTime;
        }

        private static bool TryGetExclusiveLock(SynchronizationLock syncLock)
        {
            return syncLock.TryLock();
        }

        private static bool TryGetExclusiveLock(WorkItem wi)
        {
            return TryGetExclusiveLock(wi.SyncLock);
        }

        private static void ReleaseExclusiveLock(SynchronizationLock synchronizationObject)
        {
            synchronizationObject.Release();
        }

        private void StartFuturesThread()
        {
            this.futuresThread = new Thread(new ThreadStart(this.ProcessFutureQueue)) { IsBackground = true };
            Platform.Specific.SetApartmentState(this.futuresThread, ApartmentState.MTA);
            this.futuresThread.Start();
        }

        // a thread enters Run as a result of a workitem being enqueued when the thread limit is not yet reached
        // a thread exits Run when it traverses the workitem queue and no workitem is ready to execute (which means other threads are executing them).
        private void Run(object workItem)
        {
            this.counters?.Increment(SchedulerCounters.ActiveThreads);
            this.isSchedulerThread.Value = true;
            var threadName = Thread.CurrentThread.Name;
            bool completed = false;

            try
            {
                WorkItem wi;
                if (workItem != null)
                {
                    wi = (WorkItem)workItem;
                }
                else
                {
                    // we got scheduled without a workitem, as a result of some other thread crashing
                    // try to get another item from the global queue
                    if (!this.globalWorkitems.TryDequeue(out wi))
                    {
                        // no work left
                        completed = true;
                        return;
                    }
                }

                // unless asked to shut down, keep trying to get items until no other workitems are ready
                while (!this.forcedShutdownRequested)
                {
                    this.currentWorkitemTime.Value = wi.StartTime;
                    this.ExecuteAndRelease(wi.SyncLock, wi.Callback);

                    // process the next local wi, if one is present
                    if (this.nextWorkitem.Value != null)
                    {
                        wi = (WorkItem)this.nextWorkitem.Value;
                        this.nextWorkitem.Value = null;
                        this.counters?.Decrement(SchedulerCounters.LocalQueueCount);
                        if (wi.SyncLock.TryLock())
                        {
                            this.counters?.Increment(SchedulerCounters.LocalWorkitemsPerSecond);
                            continue;
                        }

                        // it's locked, so let someone else have it
                        this.Schedule(wi.SyncLock, wi.Callback, wi.StartTime, false);
                        this.counters?.Increment(SchedulerCounters.LocalToGlobalPromotions);
                    }

                    // try to get another item from the global queue
                    if (!this.globalWorkitems.TryDequeue(out wi))
                    {
                        // no work left
                        completed = true;
                        return;
                    }

                    this.counters?.Increment(SchedulerCounters.GlobalWorkitemsPerSecond);
                }
            }
            finally
            {
                this.isSchedulerThread.Value = false;
                this.currentWorkitemTime.Value = DateTime.MaxValue;
                this.counters?.Decrement(SchedulerCounters.ActiveThreads);

                if (completed)
                {
                    this.threadSemaphore.Exit();
                }
                else
                {
                    // we got here because of an exception. let the exception bubble but start another thread.
                    ThreadPool.QueueUserWorkItem(this.Run, null);
                }
            }
        }

        private void ExecuteAndRelease(SynchronizationLock synchronizationObject, Action action)
        {
            try
            {
                action();
                this.counters?.Increment(SchedulerCounters.WorkitemsPerSecond);
            }
            catch (Exception e) when (this.errorHandler(e))
            {
            }
            finally
            {
                ReleaseExclusiveLock(synchronizationObject);
            }
        }

        // schedules workitems due from the future queue
        private void ProcessFutureQueue()
        {
            int waitTimeout = -1;

            var workReadyHandles = new EventWaitHandle[] { this.stopped, this.futureAdded };
            var allHandles = new[] { this.threadSemaphore.Empty, this.globalWorkitems.Empty, this.futureWorkitems.Empty };
            var spinWait = default(SpinWait);
            while (true)
            {
                int waitResult = WaitHandle.WaitAny(workReadyHandles, waitTimeout);
                if (waitResult == 0)
                {
                    // waitResult == 0 means this.stopped is signaled
                    if (this.forcedShutdownRequested)
                    {
                        // this.stopped is signaled and we need to exit right away.
                        return;
                    }

                    // Check that all workitems have been processed, keep processing otherwise
                    if (WaitHandle.WaitAll(allHandles, 0))
                    {
                        // all work is done
                        return;
                    }

                    // check if there are any queued future work items
                    if (this.futureWorkitems.Count == 0)
                    {
                        // spin to ensure we yield to other scheduler threads
                        spinWait.SpinOnce();
                    }
                }

                // get any items that are due
                WorkItem wi;
                while (this.futureWorkitems.TryDequeue(out wi, false))
                {
                    this.Schedule(wi.SyncLock, wi.Callback, wi.StartTime, false);
                }

                if (this.futureWorkitems.TryPeek(out wi))
                {
                    // the result could be a negative value if some other thread captured "now" before us and added the item to the future queue after the while loop above exited
                    waitTimeout = (int)this.clock.ToRealTime(wi.StartTime - this.clock.GetCurrentTime()).TotalMilliseconds;
                    if (waitTimeout < 0)
                    {
                        waitTimeout = 0;
                    }
                }
                else
                {
                    waitTimeout = -1;
                }
            }
        }
    }
}

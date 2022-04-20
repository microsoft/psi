// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    /// <summary>
    /// A workitem priority queue that dequeues workitems based on the scheduler's clock.
    /// </summary>
    internal class FutureWorkItemQueue : PriorityQueue<WorkItem>
    {
        private Scheduler scheduler;

        public FutureWorkItemQueue(string name, Scheduler scheduler)
            : base(name, WorkItem.PriorityCompare)
        {
            this.scheduler = scheduler;
        }

        protected override bool DequeueCondition(WorkItem item)
        {
            // Dequeue work item if it is due for execution, or will never be executed
            // due to the scheduler context being finalized before its execution time.
            return item.StartTime <= this.scheduler.Clock.GetCurrentTime()
                || item.StartTime > item.SchedulerContext.FinalizeTime;
        }
    }
}

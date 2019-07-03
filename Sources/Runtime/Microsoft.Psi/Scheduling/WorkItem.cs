// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    using System;

    /// <summary>
    /// A workitem that can be scheduled for execution by the scheduler.
    /// </summary>
    internal struct WorkItem
    {
        public SchedulerContext SchedulerContext;
        public SynchronizationLock SyncLock;
        public DateTime StartTime;
        public Action Callback;

        public static int PriorityCompare(WorkItem w1, WorkItem w2)
        {
            return DateTime.Compare(w1.StartTime, w2.StartTime);
        }

        public bool TryLock()
        {
            return this.SyncLock.TryLock();
        }
    }
}

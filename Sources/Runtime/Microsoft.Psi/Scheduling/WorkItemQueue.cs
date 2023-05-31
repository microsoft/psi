// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    /// <summary>
    /// A workitem priority queue that locks workitems before dequeueing.
    /// </summary>
    internal class WorkItemQueue : PriorityQueue<WorkItem>
    {
        public WorkItemQueue(string name)
            : base(name, WorkItem.PriorityCompare)
        {
        }

        protected override bool DequeueCondition(WorkItem item)
        {
            return item.TryLock();
        }
    }
}

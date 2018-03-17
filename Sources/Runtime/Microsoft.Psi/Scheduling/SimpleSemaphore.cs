// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    using System.Threading;

    /// <summary>
    /// Implements a semaphore class that limits the number of threads entering a resource and provides an event when all threads finished.
    /// </summary>
    public class SimpleSemaphore
    {
        private readonly int maxCount;
        private int count;
        private ManualResetEvent empty;
        private ManualResetEvent available;

        public SimpleSemaphore(int maxThreadCount)
        {
            this.maxCount = maxThreadCount;
            this.empty = new ManualResetEvent(true);
            this.available = new ManualResetEvent(true);
        }

        public WaitHandle Empty => this.empty;

        public WaitHandle Available => this.available;

        public bool TryEnter()
        {
            this.empty.Reset();
            var newCount = Interlocked.Increment(ref this.count);
            if (newCount > this.maxCount)
            {
                this.Exit();
                return false;
            }

            return true;
        }

        public void Exit()
        {
            var newCount = Interlocked.Decrement(ref this.count);
            if (newCount == 0)
            {
                this.empty.Set();
            }
        }
    }
}

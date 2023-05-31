// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    using System;
    using System.Threading;

    /// <summary>
    /// Implements a semaphore class that limits the number of threads entering a resource and provides an event when all threads finished.
    /// </summary>
    public class SimpleSemaphore : IDisposable
    {
        private readonly ManualResetEvent empty;
        private readonly ManualResetEvent available;
        private readonly int maxThreadCount;
        private int count;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleSemaphore"/> class.
        /// </summary>
        /// <param name="maxThreadCount">Maximum number of threads.</param>
        public SimpleSemaphore(int maxThreadCount)
        {
            this.maxThreadCount = maxThreadCount;
            this.empty = new ManualResetEvent(true);
            this.available = new ManualResetEvent(true);
        }

        /// <summary>
        /// Gets empty state wait handle.
        /// </summary>
        public WaitHandle Empty => this.empty;

        /// <summary>
        /// Gets availability wait handle.
        /// </summary>
        public WaitHandle Available => this.available;

        /// <summary>
        /// Gets maximum number of threads.
        /// </summary>
        public int MaxThreadCount => this.maxThreadCount;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.empty.Dispose();
            this.available.Dispose();
        }

        /// <summary>
        /// Try to enter the semaphore.
        /// </summary>
        /// <returns>Success.</returns>
        public bool TryEnter()
        {
            this.empty.Reset();
            var newCount = Interlocked.Increment(ref this.count);
            if (newCount > this.maxThreadCount)
            {
                this.Exit();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Exit the semaphore.
        /// </summary>
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

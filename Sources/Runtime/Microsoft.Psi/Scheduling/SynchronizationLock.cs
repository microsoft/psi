// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Scheduling
{
    using System;
    using System.Threading;

    /// <summary>
    /// Implements a simple lock. Unlike Monitor, this class doesn't enforce thread ownership.
    /// </summary>
    public sealed class SynchronizationLock
    {
        private int counter;
        private object owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynchronizationLock"/> class.
        /// </summary>
        /// <param name="owner">Owner object.</param>
        /// <param name="locked">Locked flag.</param>
        public SynchronizationLock(object owner, bool locked = false)
        {
            this.owner = owner;
            this.counter = locked ? 1 : 0;
        }

        /// <summary>
        /// Prevents anybody else from locking the lock, regardless of current state (i.e. NOT exclusive).
        /// </summary>
        public void Hold()
        {
            Interlocked.Increment(ref this.counter);
        }

        /// <summary>
        /// Attempts to take exclusive hold of the lock.
        /// </summary>
        /// <returns>True if no one else was holding the lock.</returns>
        public bool TryLock()
        {
            var v = Interlocked.CompareExchange(ref this.counter, 1, 0);
            return v == 0;
        }

        /// <summary>
        /// Spins until the lock is acquired, with no back-off.
        /// </summary>
        /// <returns>Number of spins before the lock was acquired.</returns>
        public int Lock()
        {
            SpinWait sw = default(SpinWait);
            while (!this.TryLock())
            {
                sw.SpinOnce();
            }

            return sw.Count;
        }

        /// <summary>
        /// Releases the hold on the lock.
        /// </summary>
        public void Release()
        {
            var v = Interlocked.Decrement(ref this.counter);
            if (v < 0)
            {
                throw new InvalidOperationException("The lock hold was released too many times.");
            }
        }
    }
}
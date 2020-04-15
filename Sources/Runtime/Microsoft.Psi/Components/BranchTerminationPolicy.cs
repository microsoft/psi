// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Static class containing branch termination functions for the parallel sparse operators.
    /// </summary>
    /// <typeparam name="TBranchKey">The key type.</typeparam>
    /// <typeparam name="TBranchIn">The input message type.</typeparam>
    public static class BranchTerminationPolicy<TBranchKey, TBranchIn>
    {
        /// <summary>
        /// Constructs a branch termination policy function instance to terminate when corresponding key is not longer present.
        /// </summary>
        /// <remarks>This is the default policy.</remarks>
        /// <returns>Function indicating whether and when (originating time) to terminate the given branch.</returns>
        /// <remarks>The closing time for the branch is the time of the last message that contains the key.</remarks>
        public static Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> WhenKeyNotPresent()
        {
            var lastOriginatingTimes = new Dictionary<TBranchKey, DateTime>();
            return (key, message, originatingTime) =>
            {
                if (message.ContainsKey(key))
                {
                    lastOriginatingTimes[key] = originatingTime;
                    return (false, DateTime.MaxValue);
                }

                return (true, lastOriginatingTimes[key]);
            };
        }

        /// <summary>
        /// Constructs a branch termination policy function instance to terminate after a number of messages have elapsed
        /// and the corresponding key was no longer present.
        /// </summary>
        /// <param name="count">The number of messages that have to elapse with the key not present for the branch to close.</param>
        /// <returns>Function indicating whether and when (originating time) to terminate the given branch.</returns>
        /// <remarks>The closing time for the branch is the time of the count-th message that does not contain the key.</remarks>
        public static Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> AfterKeyNotPresent(int count)
        {
            var lastOriginatingTimes = new Dictionary<TBranchKey, DateTime>();
            var counters = new Dictionary<TBranchKey, int>();
            return (key, message, originatingTime) =>
            {
                if (!counters.ContainsKey(key))
                {
                    counters.Add(key, 0);
                }

                if (message.ContainsKey(key))
                {
                    lastOriginatingTimes[key] = originatingTime;
                    return (false, DateTime.MaxValue);
                }
                else
                {
                    if (counters[key] >= count)
                    {
                        return (true, lastOriginatingTimes[key]);
                    }
                    else
                    {
                        counters[key]++;
                        lastOriginatingTimes[key] = originatingTime;
                        return (false, DateTime.MaxValue);
                    }
                }
            };
        }

        /// <summary>
        /// Constructs a branch termination policy function instance that never terminates the branch.
        /// </summary>
        /// <returns>Function indicating whether and when (originating time) to terminate the given branch.</returns>
        public static Func<TBranchKey, Dictionary<TBranchKey, TBranchIn>, DateTime, (bool, DateTime)> Never()
        {
            return (key, message, originatingTime) => (false, DateTime.MaxValue);
        }
    }
}

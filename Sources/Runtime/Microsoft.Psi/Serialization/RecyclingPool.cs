// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Message recycling pool class.
    /// </summary>
    public class RecyclingPool
    {
        /// <summary>
        /// Creates an appropriate recycling pool for the specified type.
        /// </summary>
        /// <param name="debugTrace">An optional debug trace to capture for debugging purposes.</param>
        /// <typeparam name="T">The type of objects to store in the recycling pool.</typeparam>
        /// <returns>A new recycling pool.</returns>
        public static IRecyclingPool<T> Create<T>(StackTrace debugTrace = null)
        {
            if (!Serializer.IsImmutableType<T>())
            {
                return new Cloner<T>(debugTrace);
            }

            return FakeCloner<T>.Default;
        }

        /// <summary>
        /// Maintains a cache of unused instances that can be use as cloning or deserialization targets.
        /// This class is not thread safe.
        /// </summary>
        /// <typeparam name="T">The type of instances that can be cached by this cloner.</typeparam>
        private class Cloner<T> : IRecyclingPool<T>
        {
            private const int MaxAllocationsWithoutRecycling = 100;
            private readonly SerializationContext serializationContext = new ();
            private readonly Stack<T> free = new (); // not ConcurrentStack because ConcurrentStack performs an allocation for each Push. We want to be allocation free.
#if TRACKLEAKS
            private readonly StackTrace debugTrace;
            private bool recycledOnce;
#endif

            private int outstandingAllocationCount;

            public Cloner(StackTrace debugTrace = null)
            {
#if TRACKLEAKS
                this.debugTrace = debugTrace ?? new StackTrace(true);
#endif
            }

            /// <summary>
            /// Gets the number of available allocations that have been already returned to the pool.
            /// </summary>
            public int AvailableAllocationCount => this.free.Count;

            /// <summary>
            /// Gets the number of allocations that have not yet been returned to the pool.
            /// </summary>
            public int OutstandingAllocationCount => this.outstandingAllocationCount;

            /// <summary>
            /// Returns the next available cached object.
            /// </summary>
            /// <returns>An unused cached object that can be reused as a target for cloning or deserialization.</returns>
            public T Get()
            {
                T clone;
                lock (this.free)
                {
                    if (this.free.Count > 0)
                    {
                        clone = this.free.Pop();
                    }
                    else
                    {
                        clone = default;
                    }

                    this.outstandingAllocationCount++;
                }
#if TRACKLEAKS
                // alert if the component is not recycling messages
                if (!this.recycledOnce && this.outstandingAllocationCount == MaxAllocationsWithoutRecycling && this.debugTrace != null)
                {
                    var sb = new StringBuilder("\\psi output **********************************************");
                    sb.AppendLine($"This component is not recycling messages {typeof(T)} (no recycling after {this.outstandingAllocationCount} allocations). Constructor stack trace below:");
                    foreach (var frame in this.debugTrace.GetFrames())
                    {
                        sb.AppendLine($"{frame.GetFileName()}({frame.GetFileLineNumber()}): {frame.GetMethod().DeclaringType}.{frame.GetMethod().Name}");
                    }

                    sb.AppendLine("**********************************************************");
                    Debug.WriteLine(sb.ToString());
                }
#endif
                return clone;
            }

            /// <summary>
            /// Returns an unused object back to the pool.
            /// The caller must guarantee that the entire object tree (the object and any of the objects it references) are not in use anymore.
            /// </summary>
            /// <param name="freeInstance">The object to return to the pool.</param>
            public void Recycle(T freeInstance)
            {
                lock (this.free)
                {
                    Serializer.Clear(ref freeInstance, this.serializationContext);
                    this.serializationContext.Reset();

                    this.free.Push(freeInstance);
                    this.outstandingAllocationCount--;
#if TRACKLEAKS
                    this.recycledOnce = true;
#endif
                }
            }
        }

        /// <summary>
        /// Used for immutable types.
        /// </summary>
        /// <typeparam name="T">The immutable type.</typeparam>
        private class FakeCloner<T> : IRecyclingPool<T>
        {
            public static readonly IRecyclingPool<T> Default = new FakeCloner<T>();

            public int OutstandingAllocationCount => 0;

            public int AvailableAllocationCount => 0;

            public T Get() => default;

            public void Recycle(T freeInstance)
            {
            }
        }
    }
}

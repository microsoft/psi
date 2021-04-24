// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Provides a base abstract class for stream adapters.
    /// </summary>
    /// <typeparam name="TSource">The type of the source message.</typeparam>
    /// <typeparam name="TDestination">The type of the destination message.</typeparam>
    public abstract class StreamAdapter<TSource, TDestination> : IStreamAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamAdapter{TSource, TDestination}"/> class.
        /// When used, subtypes should set the adapter field in their ctor.
        /// </summary>
        protected StreamAdapter()
        {
            this.DestinationType = typeof(TDestination);
            this.SourceType = typeof(TSource);
        }

        /// <inheritdoc />
        public Type DestinationType { get; }

        /// <inheritdoc />
        public Type SourceType { get; }

        /// <summary>
        /// Gets the allocator for reading source objects.
        /// </summary>
        public virtual Func<TSource> SourceAllocator => null;

        /// <summary>
        /// Gets the deallocator for reading source objects.
        /// </summary>
        public virtual Action<TSource> SourceDeallocator =>
            source =>
            {
                if (source is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };

        /// <inheritdoc/>
        Func<dynamic> IStreamAdapter.SourceAllocator => this.SourceAllocator != null ? () => this.SourceAllocator() : null;

        /// <inheritdoc/>
        Action<dynamic> IStreamAdapter.SourceDeallocator => this.SourceDeallocator != null ? t => this.SourceDeallocator(t) : null;

        /// <summary>
        /// Adapts a source message receiver to a destination message receiver.
        /// </summary>
        /// <param name="receiver">Source message receiver.</param>
        /// <returns>Destination message receiver.</returns>
        public Action<TSource, Envelope> AdaptReceiver(Action<TDestination, Envelope> receiver)
        {
            return (data, env) =>
            {
                var dest = this.GetAdaptedValue(data, env);
                receiver(dest, env);
            };
        }

        /// <summary>
        /// Gets the adapted value.
        /// </summary>
        /// <param name="source">The source value.</param>
        /// <param name="envelope">The source envelope.</param>
        /// <returns>The adapted value.</returns>
        public abstract TDestination GetAdaptedValue(TSource source, Envelope envelope);

        /// <summary>
        /// Disposes an adapter object that was created by this adapter.
        /// </summary>
        /// <param name="destination">The adapted object.</param>
        /// <remaks>This method is to be overriden by derived stream adapters that allocate
        /// new objects in the process of constructing the adapter object.</remaks>
        public virtual void Dispose(TDestination destination)
        {
        }
    }
}

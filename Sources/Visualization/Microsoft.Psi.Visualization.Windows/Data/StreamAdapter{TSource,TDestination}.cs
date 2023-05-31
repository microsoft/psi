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
    /// <remarks>
    /// Apart from providing a method that does the data adaptation, the
    /// <see cref="StreamAdapter{TSource, TDestination}"/> class (as well as derived classes) also
    /// collaboratively contribute to the lifetime management of the objects passing through.
    /// Specifically, when implementing a stream adapter, the developer may override
    /// the <see cref="StreamAdapter{TSource, TDestination}.SourceAllocator"/> and
    /// <see cref="StreamAdapter{TSource, TDestination}.SourceDeallocator"/> to enable the data reading
    /// layer to use specific allocators (e.g. shared pools) and deallocation procedures for the objects
    /// read from disk. In addition, in the
    /// <see cref="StreamAdapter{TSource, TDestination}.GetAdaptedValue(TSource, Envelope)"/>
    /// method the framework and stream adapters collaboratively ensure that the input parameter remains
    /// valid (allocated) throughout the execution of the entire stream adapter chain, all the way to
    /// the visualizer. This means that the developer may select a subfield of the input and pass it as
    /// output w/o having to clone. At the same time, the developer should not deallocate the input. If
    /// however the developer creates new instances of objects in the process of producing an output,
    /// then the <see cref="StreamAdapter{TSource, TDestination}.Dispose(TDestination)"/> method should
    /// also be overriden and should implement the on-demand disposal of these output objects (which are
    /// passed back to this method by the framework.)
    /// </remarks>
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

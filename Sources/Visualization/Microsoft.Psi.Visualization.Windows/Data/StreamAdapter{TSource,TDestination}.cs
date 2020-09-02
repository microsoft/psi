// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;

    /// <summary>
    /// Represents an object that adapts messages from one type to another.
    /// </summary>
    /// <typeparam name="TSource">The type of the source message.</typeparam>
    /// <typeparam name="TDestination">The type of the destination message.</typeparam>
    public class StreamAdapter<TSource, TDestination> : IStreamAdapter
    {
        /// <summary>
        /// Gets default stream adapter.
        /// </summary>
        public static readonly IStreamAdapter Default = new StreamAdapter<TDestination, TDestination>((src, env) => src);

        /// <summary>
        /// Flag indicating whether type parameter TSrc is Shared{} or not.
        /// </summary>
        public static readonly bool SourceIsSharedType = typeof(TSource).IsGenericType && typeof(TSource).GetGenericTypeDefinition() == typeof(Shared<>);

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamAdapter{TSource, TDestination}"/> class.
        /// When used, subtypes should set the adapter field in their ctor.
        /// </summary>
        protected StreamAdapter()
        {
            this.DestinationType = typeof(TDestination);
            this.Pool = PoolManager.Instance.GetPool<TSource>();
            this.SourceType = typeof(TSource);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamAdapter{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="adapterFn">Adapter function.</param>
        protected StreamAdapter(Func<TSource, Envelope, TDestination> adapterFn)
            : this()
        {
            this.AdapterFn = adapterFn ?? throw new ArgumentNullException(nameof(adapterFn));
        }

        /// <summary>
        /// Gets the allocator.
        /// </summary>
        public Func<TSource> Allocator
        {
            get
            {
                if (this.Pool == null)
                {
                    return null;
                }
                else
                {
                    return () => (TSource)this.Pool.GetOrCreate();
                }
            }
        }

        /// <inheritdoc />
        public Type DestinationType { get; }

        /// <inheritdoc />
        public IPool Pool { get; private set; }

        /// <inheritdoc />
        public Type SourceType { get; }

        /// <summary>
        /// Gets or sets the adapter function for the stream adapter.
        /// </summary>
        protected Func<TSource, Envelope, TDestination> AdapterFn { get; set; }

        /// <summary>
        /// Adapts source data to destination data.
        /// </summary>
        /// <param name="data">Source data.</param>
        /// <returns>Destination data.</returns>
        public TDestination AdaptData(TSource data)
        {
            return this.AdapterFn(data, default(Envelope));
        }

        /// <summary>
        /// Adapts a source message receiver to a destination message receiver.
        /// </summary>
        /// <param name="receiver">Source message receiver.</param>
        /// <returns>Destination message receiver.</returns>
        public Action<TSource, Envelope> AdaptReceiver(Action<TDestination, Envelope> receiver)
        {
            return (data, env) =>
            {
                var dest = this.AdapterFn(data, env);

                // Release the reference to the source data if it's shared.
                if (SourceIsSharedType && data != null)
                {
                    (data as IDisposable).Dispose();
                }

                receiver(dest, env);
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Pool?.Dispose();
            this.Pool = null;
        }
    }
}

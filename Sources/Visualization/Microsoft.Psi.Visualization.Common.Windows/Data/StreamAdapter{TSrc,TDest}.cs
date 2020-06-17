// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an object that adapts messages from one type to another.
    /// </summary>
    /// <typeparam name="TSrc">The type of the source message.</typeparam>
    /// <typeparam name="TDest">The type of the destination message.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class StreamAdapter<TSrc, TDest> : IStreamAdapter
    {
        /// <summary>
        /// Gets default stream adapter.
        /// </summary>
        public static readonly IStreamAdapter Default = new StreamAdapter<TDest, TDest>((src, env) => src);

        /// <summary>
        /// Flag indicating whether type parameter TSrc is Shared{} or not.
        /// </summary>
        public static readonly bool SourceIsSharedType = typeof(TSrc).IsGenericType && typeof(TSrc).GetGenericTypeDefinition() == typeof(Shared<>);

        private readonly Func<TSrc, Envelope, TDest> adapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamAdapter{TSrc, TDest}"/> class.
        /// </summary>
        /// <param name="adapter">Adapter function.</param>
        protected StreamAdapter(Func<TSrc, Envelope, TDest> adapter)
        {
            this.adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            this.DestinationType = typeof(TDest);
            this.Pool = PoolManager.Instance.GetPool<TSrc>();
            this.SourceType = typeof(TSrc);
        }

        /// <summary>
        /// Gets the allocator.
        /// </summary>
        public Func<TSrc> Allocator
        {
            get
            {
                if (this.Pool == null)
                {
                    return null;
                }
                else
                {
                    return () => (TSrc)this.Pool.GetOrCreate();
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
        /// Adapts source data to destination data.
        /// </summary>
        /// <param name="data">Source data.</param>
        /// <returns>Destination data.</returns>
        public TDest AdaptData(TSrc data)
        {
            return this.adapter(data, default(Envelope));
        }

        /// <summary>
        /// Adapts a source message receiver to a destination message receiver.
        /// </summary>
        /// <param name="receiver">Source message receiver.</param>
        /// <returns>Destination message receiver.</returns>
        public Action<TSrc, Envelope> AdaptReceiver(Action<TDest, Envelope> receiver)
        {
            return (data, env) =>
            {
                var dest = this.adapter(data, env);

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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a partition from a data store.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class StorePartition : Partition, IDisposable
    {
        private StorePartition(Session session, string storeName, string storePath, string name)
            : base(session, storeName, storePath, name, typeof(SimpleReader))
        {
        }

        private StorePartition()
        {
        }

        /// <summary>
        /// Creates a new store partition with the specified parameters.
        /// </summary>
        /// <param name="session">The session that this partition belongs to.</param>
        /// <param name="storeName">The store name of this partition.</param>
        /// <param name="storePath">The store path of this partition.</param>
        /// <param name="name">The partition name.</param>
        /// <returns>The newly created store partition.</returns>
        public static StorePartition Create(Session session, string storeName, string storePath, string name = null)
        {
            using (var pipeline = Pipeline.Create())
            {
                var store = Store.Create(pipeline, storeName, storePath);
            }

            return new StorePartition(session, storeName, storePath, name);
        }

        /// <summary>
        /// Creates a store partition from an exisitng data store.
        /// </summary>
        /// <param name="session">The session that this partition belongs to.</param>
        /// <param name="storeName">The store name of this partition.</param>
        /// <param name="storePath">The store path of this partition.</param>
        /// <param name="name">The partition name.</param>
        /// <returns>The newly created store partition.</returns>
        public static StorePartition CreateFromExistingStore(Session session, string storeName, string storePath, string name = null)
        {
            return new StorePartition(session, storeName, storePath, name);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            (this.Reader as SimpleReader)?.Dispose();
            this.Reader = null;
        }

        /// <summary>
        /// Overridable method to allow derived object to initialize properties as part of object construction or after deserialization.
        /// </summary>
        protected override void InitNew()
        {
            this.Reader = new SimpleReader(this.StoreName, this.StorePath);
            base.InitNew();
        }
    }
}

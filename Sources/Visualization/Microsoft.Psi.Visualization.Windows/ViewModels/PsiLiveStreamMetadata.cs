// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.ViewModels
{
    using System;

    /// <summary>
    /// Metadata object for live streams.
    /// </summary>
    internal class PsiLiveStreamMetadata : IStreamMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PsiLiveStreamMetadata"/> class.
        /// </summary>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="id">The id of the data stream.</param>
        /// <param name="typeName">The type of data of this stream.</param>
        /// <param name="supplementalMetadataTypeName">The type of supplemental metadata for this stream.</param>
        /// <param name="storeName">The store name.</param>
        /// <param name="storePath">The store path.</param>
        public PsiLiveStreamMetadata(string name, int id, string typeName, string supplementalMetadataTypeName, string storeName, string storePath)
        {
            this.Name = name;
            this.Id = id;
            this.TypeName = typeName;
            this.SupplementalMetadataTypeName = supplementalMetadataTypeName;
            this.StoreName = storeName;
            this.StorePath = storePath;
            this.FirstMessageCreationTime = DateTime.MinValue;
            this.LastMessageCreationTime = DateTime.MaxValue;
            this.FirstMessageOriginatingTime = DateTime.MinValue;
            this.LastMessageOriginatingTime = DateTime.MaxValue;
        }

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public int Id { get; private set; }

        /// <inheritdoc />
        public string TypeName { get; private set; }

        /// <inheritdoc />
        public string SupplementalMetadataTypeName { get; private set; }

        /// <inheritdoc />
        public string StoreName { get; private set; }

        /// <inheritdoc />
        public string StorePath { get; private set; }

        /// <inheritdoc />
        public DateTime FirstMessageCreationTime { get; private set; }

        /// <inheritdoc />
        public DateTime LastMessageCreationTime { get; private set; }

        /// <inheritdoc />
        public DateTime FirstMessageOriginatingTime { get; private set; }

        /// <inheritdoc />
        public DateTime LastMessageOriginatingTime { get; private set; }

        /// <inheritdoc />
        public long MessageCount { get; private set; }

        /// <inheritdoc />
        public double AverageMessageSize => 0;

        /// <inheritdoc />
        public double AverageMessageLatencyMs => 0;

        /// <inheritdoc />
        public DateTime OpenedTime => DateTime.MinValue;

        /// <inheritdoc />
        public DateTime ClosedTime => DateTime.MaxValue;

        /// <inheritdoc />
        public bool IsClosed => false;

        /// <inheritdoc />
        public T GetSupplementalMetadata<T>()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Update(Envelope envelope, int size)
        {
            if (this.FirstMessageCreationTime == DateTime.MinValue)
            {
                this.FirstMessageCreationTime = envelope.CreationTime;
            }

            if (this.FirstMessageOriginatingTime == DateTime.MinValue)
            {
                this.FirstMessageOriginatingTime = envelope.CreationTime;
            }

            if (this.LastMessageCreationTime == DateTime.MaxValue || this.LastMessageCreationTime < envelope.CreationTime)
            {
                this.LastMessageCreationTime = envelope.CreationTime;
            }

            if (this.LastMessageOriginatingTime == DateTime.MaxValue || this.LastMessageOriginatingTime < envelope.OriginatingTime)
            {
                this.LastMessageOriginatingTime = envelope.OriginatingTime;
            }
        }

        /// <inheritdoc />
        public void Update(TimeInterval messagesTimeInterval, TimeInterval messagesOriginatingTimeInterval)
        {
            this.FirstMessageCreationTime = messagesTimeInterval.Left;
            this.LastMessageCreationTime = messagesTimeInterval.Right;

            this.FirstMessageOriginatingTime = messagesOriginatingTimeInterval.Left;
            this.LastMessageOriginatingTime = messagesOriginatingTimeInterval.Right;
        }
    }
}

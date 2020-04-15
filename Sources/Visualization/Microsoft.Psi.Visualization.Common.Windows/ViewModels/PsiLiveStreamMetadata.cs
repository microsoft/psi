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
        /// <param name="partitionName">The name of th partition.</param>
        /// <param name="partitionPath">The path of the partition.</param>
        public PsiLiveStreamMetadata(string name, int id, string typeName, string partitionName, string partitionPath)
        {
            this.Name = name;
            this.Id = id;
            this.TypeName = typeName;
            this.PartitionName = partitionName;
            this.PartitionPath = partitionPath;
            this.FirstMessageTime = DateTime.MinValue;
            this.LastMessageTime = DateTime.MaxValue;
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
        public string PartitionName { get; private set; }

        /// <inheritdoc />
        public string PartitionPath { get; private set; }

        /// <inheritdoc />
        public DateTime FirstMessageTime { get; private set; }

        /// <inheritdoc />
        public DateTime LastMessageTime { get; private set; }

        /// <inheritdoc />
        public DateTime FirstMessageOriginatingTime { get; private set; }

        /// <inheritdoc />
        public DateTime LastMessageOriginatingTime { get; private set; }

        /// <inheritdoc />
        public int AverageMessageSize => 0;

        /// <inheritdoc />
        public int AverageLatency => 0;

        /// <inheritdoc />
        public int MessageCount { get; private set; }

        /// <inheritdoc />
        public void Update(Envelope envelope, int size)
        {
            if (this.FirstMessageTime == DateTime.MinValue)
            {
                this.FirstMessageTime = envelope.Time;
            }

            if (this.FirstMessageOriginatingTime == DateTime.MinValue)
            {
                this.FirstMessageOriginatingTime = envelope.Time;
            }

            if (this.LastMessageTime == DateTime.MaxValue || this.LastMessageTime < envelope.Time)
            {
                this.LastMessageTime = envelope.Time;
            }

            if (this.LastMessageOriginatingTime == DateTime.MaxValue || this.LastMessageOriginatingTime < envelope.OriginatingTime)
            {
                this.LastMessageOriginatingTime = envelope.OriginatingTime;
            }
        }

        /// <inheritdoc />
        public void Update(TimeInterval messagesTimeInterval, TimeInterval messagesOriginatingTimeInterval)
        {
            this.FirstMessageTime = messagesTimeInterval.Left;
            this.LastMessageTime = messagesTimeInterval.Right;

            this.FirstMessageOriginatingTime = messagesOriginatingTimeInterval.Left;
            this.LastMessageOriginatingTime = messagesOriginatingTimeInterval.Right;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Json
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents metadata used in storing stream data in a JSON store.
    /// </summary>
    public class JsonStreamMetadata : IStreamMetadata
    {
        private const int TicksPerMicrosecond = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamMetadata"/> class.
        /// </summary>
        public JsonStreamMetadata()
        {
            this.Reset();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamMetadata"/> class.
        /// </summary>
        /// <param name="name">The name of the stream the metadata represents.</param>
        /// <param name="id">The id of the stream the metadata represents.</param>
        /// <param name="typeName">The name of the type of data conatined in the stream the metadata represents.</param>
        /// <param name="partitionName">The name of the partation where the stream is stored.</param>
        /// <param name="partitionPath">The path of the partation where the stream is stored.</param>
        public JsonStreamMetadata(string name, int id, string typeName, string partitionName, string partitionPath)
            : this()
        {
            this.Name = name;
            this.Id = id;
            this.TypeName = typeName;
            this.PartitionName = partitionName;
            this.PartitionPath = partitionPath;
        }

        /// <inheritdoc />
        [JsonProperty(Order = 1)]
        public string Name { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 2)]
        public int Id { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 3)]
        public string TypeName { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 4)]
        public string PartitionName { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 5)]
        public string PartitionPath { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 6)]
        public DateTime FirstMessageTime { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 7)]
        public DateTime LastMessageTime { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 8)]
        public DateTime FirstMessageOriginatingTime { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 9)]
        public DateTime LastMessageOriginatingTime { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 10)]
        public int AverageMessageSize { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 11)]
        public int AverageLatency { get; set; }

        /// <inheritdoc />
        [JsonProperty(Order = 12)]
        public int MessageCount { get; set; }

        /// <summary>
        /// Reset statistics for this stream metadata instance.
        /// </summary>
        public virtual void Reset()
        {
            this.FirstMessageTime = default(DateTime);
            this.LastMessageTime = default(DateTime);
            this.FirstMessageOriginatingTime = default(DateTime);
            this.LastMessageOriginatingTime = default(DateTime);
            this.AverageMessageSize = 0;
            this.AverageLatency = 0;
            this.MessageCount = 0;
        }

        /// <inheritdoc />
        public void Update(Envelope envelope, int size)
        {
            if (this.FirstMessageOriginatingTime == default(DateTime))
            {
                this.FirstMessageOriginatingTime = envelope.OriginatingTime;
                this.FirstMessageTime = envelope.Time;
            }

            this.LastMessageOriginatingTime = envelope.OriginatingTime;
            this.LastMessageTime = envelope.Time;
            this.MessageCount++;
            this.AverageLatency = (int)((((long)this.AverageLatency * (this.MessageCount - 1)) + ((envelope.Time - envelope.OriginatingTime).Ticks / TicksPerMicrosecond)) / this.MessageCount);
            this.AverageMessageSize = (int)((((long)this.AverageMessageSize * (this.MessageCount - 1)) + size) / this.MessageCount);
        }

        /// <inheritdoc />
        public void Update(TimeInterval messagesTimeInterval, TimeInterval messagesOriginatingTimeInterval)
        {
            throw new NotImplementedException();
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.IO;

    /// <summary>
    /// Chunked UDP datagram receiver.
    /// </summary>
    /// <remarks>
    /// Meant to consume UDP datagrams off the wire, having been encoded by UdpChunkSender.
    /// </remarks>
    public class DataUnchunker
    {
        /// <summary>
        /// Function called upon receiving each set of chunks (for reporting, performance counters, testing, ...)
        /// </summary>
        private readonly Action<long> chunkset;

        /// <summary>
        /// Function called upon abandonment of a chunk set (for reporting, performance counters, testing, ...)
        /// </summary>
        private readonly Action<long> abandoned;

        /// <summary>
        /// Maximum size of a datagram.
        /// </summary>
        private readonly int maxDatagramSize;

        /// <summary>
        /// Current chunk ID being assembled.
        /// </summary>
        private long currentId = 0;

        /// <summary>
        /// Number of received chunks for the current ID.
        /// </summary>
        private ushort numReceived = 0;

        /// <summary>
        /// Flag indicating whether the current payload is still being assembled (waiting for chunks).
        /// </summary>
        private bool unfinished;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataUnchunker" /> class.
        /// </summary>
        /// <remarks>
        /// Datagrams may be dropped or received out of order over UDP. Chunk sets (different IDs) may be interleaved.
        /// As datagrams arrive, they are assembled - even when received out of order.
        /// Interleaving causes abandonment. Each time a new ID is seen, the previous one is abandoned. This means
        /// dropped datagrams are abandoned once followed by a full set.
        /// </remarks>
        /// <param name="maxDatagramSize">Maximum size of a datagram (as used when chunking).</param>
        /// <param name="chunksetFn">Function called upon receiving each set of chunks (for reporting, performance counters, testing, ...)</param>
        /// <param name="abandonedFn">Function called upon abandonment of a chunk set (for reporting, performance counters, testing, ...)</param>
        public DataUnchunker(int maxDatagramSize, Action<long> chunksetFn, Action<long> abandonedFn)
        {
            this.Payload = new byte[maxDatagramSize]; // initial size (grows as needed)
            this.maxDatagramSize = maxDatagramSize;
            this.chunkset = chunksetFn;
            this.abandoned = abandonedFn;
        }

        /// <summary>
        /// Gets buffer for assembled payload.
        /// </summary>
        public byte[] Payload { get; private set; }

        /// <summary>
        /// Gets length of payload within buffer.
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// Receive chunk of data.
        /// </summary>
        /// <remarks>Decode header and unpack chunk into assembled Payload buffer.</remarks>
        /// <param name="chunk">Chunk of data (likely from UdpClient).</param>
        /// <returns>Flag indicating whether full payload has been assembles (or else waiting for more chunks).</returns>
        public bool Receive(byte[] chunk)
        {
            var reader = new BinaryReader(new MemoryStream(chunk));
            var id = reader.ReadInt64();
            var count = reader.ReadUInt16();
            var num = reader.ReadUInt16();

            if (!this.unfinished)
            {
                this.currentId = -1; // reset after previous completion
            }

            if (id != this.currentId)
            {
                if (this.unfinished)
                {
                    this.abandoned(this.currentId);
                }

                this.chunkset(id);
                this.currentId = id;
                this.numReceived = 0;
                this.Length = 0;
            }

            var len = chunk.Length - DataChunker.HeaderSize;
            var offset = num * (this.maxDatagramSize - DataChunker.HeaderSize);
            this.Length = Math.Max(this.Length, offset + len);

            if (this.Length > this.Payload.Length)
            {
                var current = this.Payload;
                this.Payload = new byte[this.Length];
                Array.Copy(current, this.Payload, current.Length);
            }

            Array.Copy(chunk, DataChunker.HeaderSize, this.Payload, offset, len);

            return !(this.unfinished = ++this.numReceived < count);
        }
    }
}

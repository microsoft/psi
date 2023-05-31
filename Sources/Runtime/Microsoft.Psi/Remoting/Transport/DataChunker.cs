// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Chunking datagrams for handling large payloads over a size-limited transport.
    /// </summary>
    /// <remarks>
    /// Payloads larger than the max datagram size need to be split and reconstituted on
    /// the receiving end. We add our own header which includes an 8-byte ID, a 2-byte
    /// count of the number of "chunks" to expect, and a 2-byte zero-based number of the
    /// particular chunk.
    /// </remarks>
    public class DataChunker
    {
        /// <summary>
        /// Number of bytes for our chunking header.
        /// </summary>
        public const int HeaderSize = 20 /* IP header */ + 8 /* UDP header */ + 8 /* ID */ + 2 /* count */ + 2 /* number */;

        /// <summary>
        /// Maximum size of a datagram.
        /// </summary>
        private readonly int maxDatagramSize;

        /// <summary>
        /// Number of bytes remaining for payload being sent.
        /// </summary>
        private readonly int payloadSize;

        /// <summary>
        /// Internal buffer used to construct chunked datagrams being sent.
        /// </summary>
        private byte[] buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataChunker"/> class.
        /// </summary>
        /// <param name="maxDatagramSize">Maximum size of a datagram.</param>
        public DataChunker(int maxDatagramSize)
        {
            this.maxDatagramSize = maxDatagramSize;
            this.payloadSize = this.maxDatagramSize - HeaderSize;
            this.buffer = new byte[this.maxDatagramSize];
        }

        /// <summary>
        /// Break the payload into chunks as necessary, encoded with an ID.
        /// </summary>
        /// <param name="id">Unique ID across a reasonable time span.</param>
        /// <param name="payload">Binary payload to be sent.</param>
        /// <param name="length">Length of payload within given buffer.</param>
        /// <returns>Chunks as pairs of byte[], length.</returns>
        public IEnumerable<Tuple<byte[], int>> GetChunks(long id, byte[] payload, int length)
        {
            var count = (ushort)((length / this.maxDatagramSize) + 1);

            var stream = new MemoryStream(this.buffer);
            var writer = new BinaryWriter(stream);
            writer.Write(id);
            writer.Write(count);

            for (ushort num = 0; num < count; num++)
            {
                writer.Write(num);
                var size = num < count - 1 ? this.payloadSize : length % this.payloadSize;
                Array.Copy(payload, num * this.payloadSize, this.buffer, HeaderSize, size);
                yield return new Tuple<byte[], int>(this.buffer, size + HeaderSize);
                stream.Position = stream.Position - 2; // prior to num field
            }
        }
    }
}

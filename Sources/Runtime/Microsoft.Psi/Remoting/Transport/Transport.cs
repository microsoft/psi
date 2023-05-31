// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Remoting
{
    using System;
    using System.IO;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Network transport utility functions.
    /// </summary>
    internal static class Transport
    {
        /// <summary>
        /// Construct transport from `TransportKind`.
        /// </summary>
        /// <param name="kind">Kind of transport.</param>
        /// <returns>Transport instance.</returns>
        public static ITransport TransportOfKind(TransportKind kind)
        {
            switch (kind)
            {
                case TransportKind.NamedPipes:
                    return new NamedPipesTransport();
                case TransportKind.Tcp:
                    return new TcpTransport();
                case TransportKind.Udp:
                    return new UdpTransport();
                default:
                    throw new ArgumentException($"Unknown transport kind: {kind}");
            }
        }

        /// <summary>
        /// Construct transport from name of transport kind.
        /// </summary>
        /// <param name="name">Name of transport kind.</param>
        /// <returns>Transport instance.</returns>
        public static ITransport TransportOfName(string name)
        {
            return TransportOfKind((TransportKind)Enum.Parse(typeof(TransportKind), name));
        }

        /// <summary>
        /// Write session ID (GUID).
        /// </summary>
        /// <param name="id">Session ID.</param>
        /// <param name="writeFn">Function to write raw bytes to transport.</param>
        public static void WriteSessionId(Guid id, Action<byte[], int> writeFn)
        {
            var bytes = id.ToByteArray();
            writeFn(bytes, bytes.Length);
        }

        /// <summary>
        /// Write message envelope and raw bytes.
        /// </summary>
        /// <param name="envelope">Envelope to be written.</param>
        /// <param name="message">Message raw bytes to be written.</param>
        /// <param name="writer">Buffer writer to which to write.</param>
        /// <param name="writeFn">Function to write raw bytes to transport.</param>
        public static void WriteMessage(Envelope envelope, byte[] message, BufferWriter writer, Action<byte[], int> writeFn)
        {
            writer.Reset();
            writer.Write(envelope);
            writer.Write(message.Length);
            writer.WriteEx(message, 0, message.Length);
            writeFn(writer.Buffer, writer.Position);
        }

        /// <summary>
        /// Read session ID (GUID) from stream.
        /// </summary>
        /// <param name="stream">Stream from which to read.</param>
        /// <returns>Session ID.</returns>
        public static Guid ReadSessionId(Stream stream)
        {
            var bytes = new byte[16];
            Read(bytes, bytes.Length, stream);

            return new Guid(bytes);
        }

        /// <summary>
        /// Read message envelope and raw bytes from stream.
        /// </summary>
        /// <param name="reader">Buffer reader used to read.</param>
        /// <param name="stream">Stream from which to read.</param>
        /// <returns>Message envelope and raw bytes.</returns>
        public static Tuple<Envelope, byte[]> ReadMessage(BufferReader reader, Stream stream)
        {
            int headerLen;
            unsafe
            {
                headerLen = sizeof(Envelope) + sizeof(int);
            }

            reader.Reset(headerLen);
            Read(reader.Buffer, headerLen, stream);
            var envelope = reader.ReadEnvelope();
            var length = reader.ReadInt32();
            var buffer = new byte[length];
            Read(buffer, length, stream);
            return Tuple.Create(envelope, buffer);
        }

        /// <summary>
        /// Read raw bytes from stream.
        /// </summary>
        /// <param name="buffer">Buffer used to read.</param>
        /// <param name="size">Size of buffer used to read.</param>
        /// <param name="stream">Stream from which to read.</param>
        public static void Read(byte[] buffer, int size, Stream stream)
        {
            var p = 0;
            do
            {
                var count = stream.Read(buffer, p, size);
                p += count;
                size -= count;
            }
            while (size > 0);
        }
    }
}
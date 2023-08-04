// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using Microsoft.Psi.Common;

    /// <summary>
    /// Writes message blocks to an infinite file
    /// The Write methods are thread safe, allowing shared use of one message writer from multiple threads.
    /// </summary>
    internal sealed class MessageWriter : IDisposable
    {
        private const int DefaultExtentCapacity64 = 256 * 1024 * 1024;
        private const int DefaultExtentCapacity32 = 32 * 1024 * 1024;
        private const int DefaultRetentionQueueLength64 = 6;
        private const int DefaultRetentionQueueLength32 = 0;
        private InfiniteFileWriter fileWriter;

        public MessageWriter(string name, string path, int extentSize = 0)
        {
            if (extentSize == 0)
            {
                extentSize = Environment.Is64BitProcess ? DefaultExtentCapacity64 : DefaultExtentCapacity32;
            }

            if (path != null)
            {
                this.fileWriter = new InfiniteFileWriter(path, name, extentSize);
            }
            else
            {
                int retentionQueueLength = Environment.Is64BitProcess ? DefaultRetentionQueueLength64 : DefaultRetentionQueueLength32;
                this.fileWriter = new InfiniteFileWriter(name, extentSize, retentionQueueLength);
            }
        }

        public string FileName => this.fileWriter.FileName;

        public string Path => this.fileWriter.Path;

        public int CurrentExtentId => this.fileWriter.CurrentExtentId;

        public int CurrentMessageStart => this.fileWriter.CurrentBlockStart;

        public int Write(Envelope envelope, byte[] source)
        {
            return this.Write(envelope, source, 0, source.Length);
        }

        public int Write(BufferReader buffer, Envelope envelope)
        {
            return this.Write(envelope, buffer.Buffer, buffer.Position, buffer.RemainingLength);
        }

        public int Write(Envelope envelope, byte[] source, int start, int count)
        {
            // for now, lock. To get rid of it we need to split an ExtentWriter out of the InfiniteFileWriter
            lock (this.fileWriter)
            {
                unsafe
                {
                    int totalBytes = sizeof(Envelope) + count;
                    this.fileWriter.ReserveBlock(totalBytes);
                    this.fileWriter.WriteToBlock((byte*)&envelope, sizeof(Envelope));
                    this.fileWriter.WriteToBlock(source, start, count);
                    this.fileWriter.CommitBlock();
                    return totalBytes;
                }
            }
        }

        public int Write(BufferWriter buffer, Envelope envelope)
        {
            return this.Write(envelope, buffer.Buffer, 0, buffer.Position);
        }

        public void Dispose()
        {
            this.fileWriter.Dispose();
            this.fileWriter = null;
        }
    }
}

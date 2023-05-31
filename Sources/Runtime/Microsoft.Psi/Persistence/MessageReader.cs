// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Reads message blocks from an infinite file.
    /// This class is not thread safe. It is the caller's responsibility to synchronize the calls to MoveNext and Read.
    /// Concurrent read can still be achieved, by instantiating multiple message readers against the same file.
    /// </summary>
    internal sealed class MessageReader : IDisposable
    {
        private InfiniteFileReader fileReader;
        private Envelope currentEnvelope;

        public MessageReader(string fileName, string path)
        {
            if (path == null)
            {
                this.fileReader = new InfiniteFileReader(fileName);
            }
            else
            {
                this.fileReader = new InfiniteFileReader(path, fileName);
            }
        }

        public Mutex DataReady => this.fileReader.WritePulse;

        public string FileName => this.fileReader.FileName;

        public string Path => this.fileReader.Path;

        public Envelope Current => this.currentEnvelope;

        public int CurrentExtentId => this.fileReader.CurrentExtentId;

        public int CurrentMessageStart => this.fileReader.CurrentBlockStart;

        public void Seek(int extentId, int position)
        {
            this.fileReader.Seek(extentId, position);
        }

        public bool MoveNext(HashSet<int> ids)
        {
            bool hasData = this.MoveNext();
            while (hasData && !ids.Contains(this.currentEnvelope.SourceId))
            {
                hasData = this.MoveNext();
            }

            return hasData;
        }

        public bool MoveNext()
        {
            Envelope e;
            if (!this.fileReader.MoveNext())
            {
                return false;
            }

            unsafe
            {
                this.fileReader.Read((byte*)&e, sizeof(Envelope));
                this.currentEnvelope = e;
            }

            return true;
        }

        public int Read(ref byte[] buffer)
        {
            return this.fileReader.ReadBlock(ref buffer);
        }

        public unsafe int Read(byte* buffer, int size)
        {
            return this.fileReader.Read(buffer, size);
        }

        public void Dispose()
        {
            this.fileReader?.Dispose();
            this.fileReader = null;
        }
    }
}

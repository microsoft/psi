// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Threading;

    internal unsafe sealed class InfiniteFileReader : IDisposable
    {
        private const int WriteEventTimeout = 1000; // ms
        private byte* startPointer;
        private int currentPosition;
        private int currentBlockStart;
        private MemoryMappedFile mappedFile;
        private MemoryMappedViewAccessor view;
        private string path;
        private string fileName;
        private int fileId;
        private Mutex writePulse;
        private int remainingBlockSize;

        public InfiniteFileReader(string path, string fileName, int fileId = 0)
        {
            this.path = path;
            this.fileName = fileName;
            this.fileId = fileId;
            Mutex pulse;
            Mutex.TryOpenExisting(InfiniteFileWriter.PulseEventName(path, fileName), out pulse);
            this.writePulse = pulse ?? new Mutex(false);
        }

        public InfiniteFileReader(string name, int fileId = 0)
            : this(null, name, fileId)
        {
        }

        public Mutex WritePulse => this.writePulse;

        public string FileName => this.fileName;

        public string Path => this.path;

        public int CurrentExtentId => this.fileId;

        public int CurrentBlockStart => this.currentBlockStart;

        /// <summary>
        /// Indicates whether the specified file is already loaded by a reader or writer.
        /// </summary>
        /// <param name="name">Infinite file name.</param>
        /// <param name="path">Infinite file path.</param>
        /// <returns>Returns true if the store is already loaded.</returns>
        public static bool IsActive(string name, string path)
        {
            if (!EventWaitHandle.TryOpenExisting(InfiniteFileWriter.PulseEventName(path, name), out EventWaitHandle eventHandle))
            {
                return false;
            }

            eventHandle.Dispose();
            return true;
        }

        /// <summary>
        /// Indicates whether more data might be added to this file
        /// (i.e. the file still has an active writer).
        /// </summary>
        /// <returns>Returns true if there is an active writer to this file.</returns>
        public bool IsMoreDataExpected()
        {
            return InfiniteFileWriter.IsActive(this.fileName, this.path);
        }

        public void Dispose()
        {
            this.writePulse.Dispose();
            this.CloseCurrent();
        }

        // Seeks to the next block (assumes the position points to a block entry)
        public void Seek(int extentId, int position)
        {
            if (this.fileId != extentId || this.startPointer == null)
            {
                this.CloseCurrent();
                this.fileId = extentId;
                this.LoadNextExtent();
            }

            this.currentPosition = position;
            this.currentBlockStart = position;
            this.remainingBlockSize = 0;
        }

        /// <summary>
        /// Returns true if we are in the middle of a block or
        /// if we are positioned at the start of the block and the block size prefix is greater than zero.
        /// If false, use <see cref="IsMoreDataExpected"/> to determine if there could ever be more data
        /// (i.e. if a writer is still active).
        /// </summary>
        /// <returns>True if more data is present, false if no more data is available</returns>
        public bool HasMoreData() => this.mappedFile == null || this.remainingBlockSize != 0 || *(int*)(this.startPointer + this.currentPosition) != 0;

        /// <summary>
        /// Prepares to read the next message if one is present
        /// </summary>
        /// <returns>True if a message exists, false if no message is present</returns>
        public bool MoveNext()
        {
            if (this.startPointer == null)
            {
                this.LoadNextExtent();
            }

            this.currentPosition += this.remainingBlockSize;
            this.currentBlockStart = this.currentPosition;
            this.remainingBlockSize = *(int*)(this.startPointer + this.currentPosition);
            if (this.remainingBlockSize == 0)
            {
                // a zero block size means there is no more data to read (for now)
                return false;
            }

#if DEBUG
            // read twice to detect unaligned writes, which would mean we introduced a bug in the writer (unaligned writes mean we could read incomplete data).
            int check = *(int*)(this.startPointer + this.currentPosition);
            if (check != this.remainingBlockSize)
            {
                throw new InvalidDataException();
            }
#endif
            this.currentPosition += sizeof(int);

            // eof?
            if (this.remainingBlockSize < 0)
            {
                this.startPointer = null;
                return this.MoveNext();
            }

            return true; // more data available
        }

        public int ReadBlock(ref byte[] target)
        {
            if (target == null || this.remainingBlockSize > target.Length)
            {
                target = new byte[this.remainingBlockSize];
            }

            fixed (byte* b = target)
            {
                return this.Read(b, this.remainingBlockSize);
            }
        }

        public int Read(byte[] target, int bytesToRead)
        {
            if (this.remainingBlockSize < bytesToRead)
            {
                bytesToRead = this.remainingBlockSize;
            }

            fixed (byte* b = target)
            {
                return this.Read(b, bytesToRead);
            }
        }

        public int Read(byte* target, int bytes)
        {
            if (bytes > this.remainingBlockSize)
            {
                throw new ArgumentException("Attempted to read past the end of the block.");
            }

            this.UncheckedRead(target, bytes);
            return bytes;
        }

        private void UncheckedRead(byte* target, int bytes)
        {
            Buffer.MemoryCopy(this.startPointer + this.currentPosition, target, bytes, bytes);
            this.currentPosition += bytes;
            this.remainingBlockSize -= bytes;
        }

        private void LoadNextExtent()
        {
            // get the name of the new file from the old file
            if (this.mappedFile != null)
            {
                this.fileId = -this.remainingBlockSize; // we've read the EOF when reading the remaining size
                this.CloseCurrent();
            }

            string name = string.Format(InfiniteFileWriter.FileNameFormat, this.fileName, this.fileId);

            if (this.path != null)
            {
                // create a new MMF from persisted file, if the file can be found
                string fullName = System.IO.Path.Combine(this.path, name);
                if (File.Exists(fullName))
                {
                    using (var file = File.Open(fullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        this.mappedFile = MemoryMappedFile.CreateFromFile(file, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.Inheritable, false);
                    }
                }
            }

            if (this.mappedFile == null)
            {
                // attach to an in-memory MMF
                this.mappedFile = MemoryMappedFile.OpenExisting(name);
            }

            this.view = this.mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            this.view.SafeMemoryMappedViewHandle.AcquirePointer(ref this.startPointer);
            this.remainingBlockSize = 0;
            this.currentPosition = 0;
        }

        private void CloseCurrent()
        {
            // if the Writer creates and releases volatile extents too fast, a slow reader could lose the next extent
            // we could hold on to the next extent (if we can sufficiently parallelize the reads, so the reader skips forward as fast as possible without waiting for deserialization).
            // we should create an ExtentReader (and ExtentWriter) class to partition responsibilities. The EXtentWriter could help with locking too.
            if (this.mappedFile != null)
            {
                this.view.SafeMemoryMappedViewHandle.ReleasePointer();
                this.view.Dispose();
                this.view = null;
                this.mappedFile.Dispose();
                this.mappedFile = null;
                this.remainingBlockSize = 0;
            }
        }
    }
}

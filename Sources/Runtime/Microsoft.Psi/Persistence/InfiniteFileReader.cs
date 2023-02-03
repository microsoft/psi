// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Threading;
    using System.Threading.Tasks;

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

        public void Dispose()
        {
            this.writePulse.Dispose();
            this.CloseCurrent();

            // may have already been disposed in CloseCurrent
            this.view?.Dispose();
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
        /// If false, use <see cref="PsiStoreMonitor.IsStoreLive(string, string)"/> to determine if there could ever be more data
        /// (i.e. if a writer is still active).
        /// </summary>
        /// <returns>True if more data is present, false if no more data is available.</returns>
        public bool HasMoreData() => this.mappedFile == null || this.remainingBlockSize != 0 || *(int*)(this.startPointer + this.currentPosition) != 0;

        /// <summary>
        /// Prepares to read the next message if one is present.
        /// </summary>
        /// <returns>True if a message exists, false if no message is present.</returns>
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
                // A zero block size means there is no more data to read for now. This
                // may change if more data is subsequently written to this extent, if
                // it is open for simultaneous reading/writing.
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

            // a negative remaining block size indicates we have reached the end of the extent
            if (this.remainingBlockSize < 0)
            {
                // clear the start pointer and move to the next extent
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
            // If there is a current extent open, it means we have reached the EOF and remainingBlockSize
            // will be a negative number whose absolute value represents the next file extent id.
            if (this.mappedFile != null)
            {
                // Get the fileId of the next extent to load and close the current extent.
                this.fileId = -this.remainingBlockSize;
                this.CloseCurrent();
            }

            string extentName = string.Format(InfiniteFileWriter.FileNameFormat, this.fileName, this.fileId);

            if (this.path != null)
            {
                // create a new MMF from persisted file, if the file can be found
                string fullName = System.IO.Path.Combine(this.path, extentName);
                if (File.Exists(fullName))
                {
                    int maxAttempts = 5;
                    int attempts = 0;

                    // Retry opening the file up to a maximum number of attempts - this is to handle the possible race
                    // condition where the file is being resized on disposal (see InfiniteFileWriter.CloseCurrent),
                    // which will result in an IOException being thrown if we attempt to open the file simultaneously.
                    while (this.mappedFile == null && attempts++ < maxAttempts)
                    {
                        try
                        {
                            var file = File.Open(fullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                            this.mappedFile = MemoryMappedFile.CreateFromFile(file, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.Inheritable, false);
                        }
                        catch (IOException)
                        {
                            if (attempts == maxAttempts)
                            {
                                // rethrow the exception if we have exhausted the maximum number of attempts
                                throw;
                            }
                        }
                    }
                }
            }

            if (this.mappedFile == null)
            {
                // attach to an in-memory MMF
                try
                {
                    this.mappedFile = MemoryMappedFile.OpenExisting(extentName);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to open extent: {extentName}.", ex);
                }
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

                // Calling `view.Dispose()` flushes the underlying MemoryMappedView, in turn making
                // blocking system calls and with retry logic and `Thread.Sleep()`s.
                // To avoid taking this hit on our thread here (which blocks writing to the infinite file for
                // human-noticeable time when crossing extents), we queue this work to the thread pool.
                // See: https://referencesource.microsoft.com/#System.Core/System/IO/MemoryMappedFiles/MemoryMappedView.cs,176
                var temp = this.view;
                Task.Run(() => temp.Dispose());
                this.view = null;
                this.mappedFile.Dispose();
                this.mappedFile = null;
                this.remainingBlockSize = 0;
            }
        }
    }
}

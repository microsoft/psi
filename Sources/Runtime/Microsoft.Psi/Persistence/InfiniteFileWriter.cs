// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Psi.Common;

    internal unsafe sealed class InfiniteFileWriter : IDisposable
    {
        internal const string FileNameFormat = "{0}_{1:000000}.psi";
        private const string PulseEventFormat = @"Global\PulseEvent_{0}_{1}";
        private readonly object syncRoot = new object();
        private string extentName;
        private int extentSize;
        private byte* startPointer;
        private byte* freePointer;
        private byte* currentPointer;
        private byte* currentBlock;
        private int currentBlockSize;
        private int remainingAllocationSize;
        private MemoryMappedFile mappedFile;
        private MemoryMappedViewAccessor view;
        private int fileId;
        private int freeSpace;
        private bool disposed = false;
        private EventWaitHandle localWritePulse;
        private Mutex globalWritePulse;
        private Queue<MemoryMappedFile> priorExtents;
        private int priorExtentQueueLength;
        private object viewDisposeLock = new object();

        public InfiniteFileWriter(string fileName, int extentSize, int retentionQueueLength)
            : this(null, fileName, extentSize)
        {
            this.priorExtentQueueLength = retentionQueueLength;
            this.priorExtents = new Queue<MemoryMappedFile>(retentionQueueLength);
        }

        public InfiniteFileWriter(string path, string fileName, int extentSize)
        {
            this.Path = path;
            this.FileName = fileName;
            this.extentSize = extentSize + sizeof(int); // eof marker
            this.localWritePulse = new EventWaitHandle(false, EventResetMode.ManualReset);
            new Thread(new ThreadStart(() =>
            {
                try
                {
                    this.globalWritePulse = new Mutex(true, PulseEventName(path, fileName));
                }
                catch (UnauthorizedAccessException)
                {
                    // Some platforms don't allow global mutexes.  In this case
                    // we can still continue on with a slight perf degradation.
                }

                try
                {
                    while (!this.disposed)
                    {
                        this.localWritePulse?.WaitOne();
                        this.globalWritePulse?.ReleaseMutex();
                        this.globalWritePulse?.WaitOne();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // ignore if localWritePulse was disposed
                }
                catch (AbandonedMutexException)
                {
                    // ignore if globalWritePulse was disposed
                }
            })) { IsBackground = true }.Start();

            this.CreateNewExtent();
        }

        public string FileName { get; }

        public string Path { get; }

        public bool IsVolatile => this.Path == null;

        public int CurrentExtentId => this.fileId - 1;

        public int CurrentBlockStart => (int)(this.freePointer - this.startPointer);

        public void Dispose()
        {
            this.CloseCurrent(true);
            if (this.priorExtentQueueLength > 0)
            {
                foreach (var extent in this.priorExtents)
                {
                    extent.Dispose();
                }

                this.priorExtents = null;
            }

            this.disposed = true;
            this.localWritePulse.Set();
            this.localWritePulse.Dispose();
            this.localWritePulse = null;
            this.globalWritePulse?.Dispose();
            this.globalWritePulse = null;

            // may have already been disposed in CloseCurrent
            this.view?.Dispose();
        }

        public void Write(BufferWriter bufferWriter)
        {
            this.ReserveBlock(bufferWriter.Position);
            this.WriteToBlock(bufferWriter.Buffer, 0, bufferWriter.Position);
            this.CommitBlock();
        }

        public void WriteToBlock(byte[] source)
        {
            unsafe
            {
                fixed (byte* b = source)
                {
                    this.WriteToBlock(b, source.Length);
                }
            }
        }

        public void WriteToBlock(byte[] source, int start, int count)
        {
            if (count == 0)
            {
                return;
            }

            if (start + count > source.Length)
            {
                throw new InvalidOperationException("Attempted to read beyond the end of the source buffer.");
            }

            fixed (byte* b = &source[start])
            {
                this.WriteToBlock(b, count);
            }
        }

        public void WriteToBlock(byte* source, int bytes)
        {
            Buffer.MemoryCopy(source, this.currentPointer, this.remainingAllocationSize, bytes); // this performs the bounds check
            this.currentPointer += bytes;
            this.remainingAllocationSize -= bytes;
        }

        public void ReserveBlock(int bytes)
        {
            // pad the block to guarantee atomicity of write/read of block markers
            int padding = 4 - (bytes % 4);
            if (padding != 4)
            {
                bytes += padding;
            }

            int totalBytes = bytes + sizeof(int);
            if (this.freeSpace < totalBytes)
            {
                // we don't break the data across extents, to ensure extents are independently readable
                if (this.extentSize < totalBytes)
                {
                    this.extentSize = totalBytes;
                }

                this.CreateNewExtent();
            }

            // remember the start of the block
            this.currentBlock = this.freePointer;
            this.currentBlockSize = bytes;

            // remember the start of the free space
            this.currentPointer = this.freePointer + sizeof(int);
            this.remainingAllocationSize = bytes;

            this.freePointer += totalBytes;
            this.freeSpace -= totalBytes;

            // write the tail marker but don't move the pointer. Simply let the next write override it, that's how we know there is more data when reading.
            *((uint*)this.freePointer) = 0;
        }

        public void CommitBlock()
        {
            // write the header. This MUST be 32-bit aligned (this is achieved by padding the reserved block size to multiples of 4)
            *((int*)this.currentBlock) = this.currentBlockSize;
            this.localWritePulse.Set();
            this.localWritePulse.Reset();
        }

        /// <summary>
        /// Clears all buffers for this view and causes any buffered data to be written to the underlying file,
        /// by calling <see cref="MemoryMappedViewAccessor.Flush"/>.
        /// Warning: calling this function can make the persistence system less efficient,
        /// diminishing the overall throughput.
        /// </summary>
        public void Flush() => this.view.Flush();

        internal static string PulseEventName(string path, string fileName)
        {
            return MakeHandleName(PulseEventFormat, path, fileName);
        }

        private static string MakeHandleName(string format, string path, string fileName)
        {
            var name = string.Format(format, path?.ToLower().GetDeterministicHashCode(), fileName.ToLower());
            if (name.Length > 260)
            {
                // exceeded the name length limit
                return string.Format(format, path?.ToLower().GetDeterministicHashCode(), fileName.ToLower().GetDeterministicHashCode());
            }

            return name;
        }

        private void CreateNewExtent()
        {
            int newFileId = this.fileId;
            this.fileId++;
            this.extentName = string.Format(FileNameFormat, this.FileName, newFileId);

            // create a new file first, just in case anybody is reading
            MemoryMappedFile newMMF;
            if (!this.IsVolatile)
            {
                this.extentName = System.IO.Path.Combine(this.Path, this.extentName);
                var file = File.Open(this.extentName, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                try
                {
                    newMMF = MemoryMappedFile.CreateFromFile(file, null, this.extentSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.Inheritable, false);
                }
                catch (IOException)
                {
                    file.Dispose();
                    throw;
                }
            }
            else
            {
                newMMF = MemoryMappedFile.CreateNew(this.extentName, this.extentSize);
            }

            // store the id of the new file in the old file and close it
            if (this.mappedFile != null)
            {
                // the id of the next file is encoded as a negative value to mark the end of the current file
                *(int*)this.freePointer = -newFileId;
                this.CloseCurrent(false);
            }

            // re-initialize
            this.mappedFile = newMMF;
            this.view = this.mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);
            this.view.SafeMemoryMappedViewHandle.AcquirePointer(ref this.startPointer);
            this.freeSpace = this.extentSize - sizeof(int);
            this.freePointer = this.startPointer;
            *((uint*)this.freePointer) = 0;
        }

        // closes the current extent, and trims the file if requested (should only be requested for the very last extent, when Disposing).
        private void CloseCurrent(bool disposing)
        {
            if (this.mappedFile != null)
            {
                this.view.SafeMemoryMappedViewHandle.ReleasePointer();

                // Calling `view.Dispose()` flushes the underlying MemoryMappedView, in turn making
                // blocking system calls and with retry logic and `Thread.Sleep()`s.
                // To avoid taking this hit on our thread here (which blocks writing to the infinite file for
                // human-noticeable time when crossing extents), we queue this work to the thread pool.
                // See: https://referencesource.microsoft.com/#System.Core/System/IO/MemoryMappedFiles/MemoryMappedView.cs,176
                var temp = this.view;
                var viewDisposeTask = Task.Run(() =>
                {
                    // serialize disposal to avoid disk thrashing
                    lock (this.viewDisposeLock)
                    {
                        temp.Dispose();
                    }
                });

                this.view = null;

                if (this.priorExtentQueueLength > 0 && !disposing)
                {
                    // if the queue reached its limit, remove one item first
                    if (this.priorExtents.Count == this.priorExtentQueueLength)
                    {
                        var pe = this.priorExtents.Dequeue();
                        pe.Dispose();
                    }

                    this.priorExtents.Enqueue(this.mappedFile);
                }
                else
                {
                    this.mappedFile.Dispose();
                }

                this.mappedFile = null;

                if (disposing && !this.IsVolatile)
                {
                    // need to wait for the view to be completely disposed before resizing the file,
                    // otherwise there will be an access conflict and the file will not be resized
                    viewDisposeTask.Wait();

                    try
                    {
                        using (var file = File.Open(this.extentName, FileMode.Open, FileAccess.Write, FileShare.None))
                        {
                            // resize the file to a multiple of 4096 (page size)
                            int actualSize = this.extentSize - this.freeSpace;
                            file.SetLength(((actualSize >> 12) + 1) << 12);
                        }
                    }
                    catch (IOException)
                    {
                        // ignore
                    }
                }
            }

            this.freeSpace = 0;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Reader that streams audio from a WAVE file.
    /// </summary>
    [StreamReader("WAVE File", ".wav")]
    public sealed class WaveFileStreamReader : IStreamReader
    {
        /// <summary>
        /// Name of audio stream.
        /// </summary>
        public const string AudioStreamName = "Audio";

        /// <summary>
        /// Default size of each data buffer in milliseconds.
        /// </summary>
        public const int DefaultAudioBufferSizeMs = 20;

        private const int AudioSourceId = 0;

        private readonly WaveAudioStreamMetadata audioStreamMetadata;
        private readonly BinaryReader waveFileReader;
        private readonly WaveFormat waveFormat;
        private readonly DateTime startTime;
        private readonly long dataStart;
        private readonly long dataLength;

        private readonly List<Delegate> audioTargets = new List<Delegate>();
        private readonly List<Delegate> audioIndexTargets = new List<Delegate>();

        private int sequenceId = 0;
        private byte[] buffer;
        private TimeInterval seekInterval = TimeInterval.Infinite;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileStreamReader"/> class.
        /// </summary>
        /// <param name="name">Name of the WAVE file.</param>
        /// <param name="path">Path of the WAVE file.</param>
        public WaveFileStreamReader(string name, string path)
            : this(name, path, DateTime.UtcNow, DefaultAudioBufferSizeMs)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileStreamReader"/> class.
        /// </summary>
        /// <param name="name">Name of the WAVE file.</param>
        /// <param name="path">Path of the WAVE file.</param>
        /// <param name="startTime">Starting time for streams of data..</param>
        /// <param name="audioBufferSizeMs">The size of each data buffer in milliseconds.</param>
        internal WaveFileStreamReader(string name, string path, DateTime startTime, int audioBufferSizeMs = DefaultAudioBufferSizeMs)
        {
            this.Name = name;
            this.Path = path;
            this.startTime = startTime;
            var file = System.IO.Path.Combine(path, name);
            this.Size = file.Length;
            this.waveFileReader = new BinaryReader(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read));
            this.waveFormat = WaveFileHelper.ReadWaveFileHeader(this.waveFileReader);
            this.dataLength = WaveFileHelper.ReadWaveDataLength(this.waveFileReader);
            this.dataStart = this.waveFileReader.BaseStream.Position;
            var bufferSize = (int)(this.waveFormat.AvgBytesPerSec * audioBufferSizeMs / 1000);
            this.buffer = new byte[bufferSize];

            // Compute originating times based on audio chunk start time + duration
            var endTime = this.startTime.AddSeconds((double)this.dataLength / (double)this.waveFormat.AvgBytesPerSec);
            this.MessageOriginatingTimeInterval = this.MessageCreationTimeInterval = this.StreamTimeInterval = new TimeInterval(this.startTime, endTime);

            var messageCount = (long)Math.Ceiling((double)this.dataLength / bufferSize);
            this.audioStreamMetadata = new WaveAudioStreamMetadata(AudioStreamName, typeof(AudioBuffer).AssemblyQualifiedName, name, path, this.startTime, endTime, messageCount, (double)this.dataLength / messageCount, audioBufferSizeMs);
        }

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public string Path { get; private set; }

        /// <inheritdoc />
        public IEnumerable<IStreamMetadata> AvailableStreams
        {
            get
            {
                yield return this.audioStreamMetadata;
            }
        }

        /// <inheritdoc />
        public TimeInterval MessageCreationTimeInterval { get; private set; }

        /// <inheritdoc />
        public TimeInterval MessageOriginatingTimeInterval { get; private set; }

        /// <inheritdoc />
        public TimeInterval StreamTimeInterval { get; private set; }

        /// <inheritdoc/>
        public long? Size { get; }

        /// <inheritdoc/>
        public int? StreamCount => 1;

        /// <inheritdoc />
        public bool ContainsStream(string name)
        {
            return name == AudioStreamName;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.waveFileReader.Dispose();
        }

        /// <inheritdoc />
        public IStreamMetadata GetStreamMetadata(string name)
        {
            ValidateStreamName(name);
            return this.audioStreamMetadata;
        }

        /// <inheritdoc />
        public T GetSupplementalMetadata<T>(string streamName)
        {
            ValidateStreamName(streamName);

            if (typeof(T) != typeof(WaveFormat))
            {
                throw new NotSupportedException("The Audio stream supports only supplemental metadata of type WaveFormat.");
            }

            return (T)(object)this.waveFormat;
        }

        /// <inheritdoc />
        public bool IsLive()
        {
            return false;
        }

        /// <inheritdoc />
        public bool MoveNext(out Envelope envelope)
        {
            if (
                !this.Next(out var audio, out envelope) ||
                !this.seekInterval.PointIsWithin(envelope.OriginatingTime))
            {
                return false;
            }

            this.InvokeTargets(audio, envelope);
            return true;
        }

        /// <inheritdoc />
        public IStreamReader OpenNew()
        {
            return new WaveFileStreamReader(this.Name, this.Path, this.startTime);
        }

        /// <inheritdoc />
        public IStreamMetadata OpenStream<T>(string name, Action<T, Envelope> target, Func<T> allocator = null, Action<T> deallocator = null, Action<SerializationException> errorHandler = null)
        {
            ValidateStreamName(name);

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (allocator != null)
            {
                throw new NotSupportedException($"Allocators are not supported by {nameof(WaveFileStreamReader)} and must be null.");
            }

            // targets are later called when data is read by MoveNext or ReadAll (see InvokeTargets).
            this.audioTargets.Add(target);
            return this.audioStreamMetadata;
        }

        /// <inheritdoc />
        public IStreamMetadata OpenStreamIndex<T>(string name, Action<Func<IStreamReader, T>, Envelope> target, Func<T> allocator = null)
        {
            ValidateStreamName(name);

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (allocator != null)
            {
                throw new NotSupportedException($"Allocators are not supported by {nameof(WaveFileStreamReader)} and must be null.");
            }

            // targets are later called when data is read by MoveNext or ReadAll (see InvokeTargets).
            this.audioIndexTargets.Add(target);
            return this.audioStreamMetadata;
        }

        /// <inheritdoc />
        public void ReadAll(ReplayDescriptor descriptor, CancellationToken cancelationToken = default)
        {
            this.Seek(descriptor.Interval);
            while (!cancelationToken.IsCancellationRequested && this.Next(out var audio, out var envelope))
            {
                if (descriptor.Interval.PointIsWithin(envelope.OriginatingTime))
                {
                    this.InvokeTargets(audio, envelope);
                }
            }
        }

        /// <inheritdoc />
        public void Seek(TimeInterval interval, bool useOriginatingTime = false)
        {
            this.seekInterval = interval;
            this.waveFileReader.BaseStream.Position = this.dataStart;
            this.sequenceId = 0;

            var previousPosition = this.waveFileReader.BaseStream.Position;
            while (this.Next(out var _, out var envelope))
            {
                if (interval.PointIsWithin(envelope.OriginatingTime))
                {
                    this.waveFileReader.BaseStream.Position = previousPosition; // rewind
                    return;
                }

                previousPosition = this.waveFileReader.BaseStream.Position;
            }
        }

        /// <summary>
        /// Validate that name corresponds to a supported stream.
        /// </summary>
        /// <param name="name">Stream name.</param>
        private static void ValidateStreamName(string name)
        {
            if (name != AudioStreamName)
            {
                // the only supported stream is the single audio stream.
                throw new NotSupportedException($"Only '{AudioStreamName}' stream is supported.");
            }
        }

        /// <summary>
        /// Read an audio buffer of data.
        /// </summary>
        /// <param name="position">Byte position.</param>
        /// <param name="sequenceId">Message sequence ID.</param>
        /// <returns>Audio buffer.</returns>
        private AudioBuffer Read(long position, int sequenceId)
        {
            this.waveFileReader.BaseStream.Position = position;
            this.sequenceId = sequenceId;
            if (!this.Next(out var audio, out var _))
            {
                throw new InvalidOperationException("Invalid position (out of bounds).");
            }

            return audio;
        }

        /// <summary>
        /// Invoke target callbacks with currently read message information.
        /// </summary>
        /// <param name="audio">Current audio buffer.</param>
        /// <param name="envelope">Current message envelope.</param>
        /// <remarks>This method is called as the data is read when MoveNext() or ReadAll() are called.</remarks>
        private void InvokeTargets(AudioBuffer audio, Envelope envelope)
        {
            foreach (Delegate action in this.audioTargets)
            {
                action.DynamicInvoke(audio, envelope);
            }

            foreach (Delegate action in this.audioIndexTargets)
            {
                // Index targets are given the message Envelope and a Func by which to retrieve the message data.
                // This Func may be held as a kind of "index" later called to retrieve the data. It may be called,
                // given the current IStreamReader or a new `reader` instance against the same store.
                // The Func is a closure over the `position` and `sequenceId` information needed for retrieval
                // but these implementation details remain opaque to users of the reader.
                var position = this.waveFileReader.BaseStream.Position;
                var sequenceId = this.sequenceId;
                action.DynamicInvoke(new Func<IStreamReader, AudioBuffer>(reader => ((WaveFileStreamReader)reader).Read(position, sequenceId)), envelope);
            }
        }

        /// <summary>
        /// Read the next audio buffer of data from the WAVE file.
        /// </summary>
        /// <param name="audio">Audio buffer to be populated.</param>
        /// <param name="envelope">Message envelope to be populated.</param>
        /// <returns>A bool indicating whether the end of available data has been reached.</returns>
        private bool Next(out AudioBuffer audio, out Envelope envelope)
        {
            var bytesRemaining = this.dataLength - (this.waveFileReader.BaseStream.Position - this.dataStart);
            int nextBytesToRead = (int)Math.Min(this.buffer.Length, bytesRemaining);

            // Re-allocate buffer if necessary
            if ((this.buffer == null) || (this.buffer.Length != nextBytesToRead))
            {
                this.buffer = new byte[nextBytesToRead];
            }

            // Read next audio chunk
            int bytesRead = this.waveFileReader.Read(this.buffer, 0, (int)nextBytesToRead);
            if (bytesRead == 0)
            {
                // Break on end of file
                audio = default;
                envelope = default;
                return false;
            }

            // Truncate buffer if necessary
            if (bytesRead < nextBytesToRead)
            {
                byte[] truncated = new byte[bytesRead];
                Array.Copy(this.buffer, 0, truncated, 0, bytesRead);
                this.buffer = truncated;
            }

            var totalBytesRead = this.waveFileReader.BaseStream.Position - this.dataStart;
            DateTime time = this.startTime.AddSeconds((double)totalBytesRead / (double)this.waveFormat.AvgBytesPerSec);

            audio = new AudioBuffer(this.buffer, this.waveFormat);
            envelope = new Envelope(time, time, AudioSourceId, this.sequenceId++);
            return true;
        }

        /// <summary>
        /// WAVE audio stream metadata.
        /// </summary>
        public class WaveAudioStreamMetadata : StreamMetadataBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="WaveAudioStreamMetadata"/> class.
            /// </summary>
            /// <param name="name">Stream name.</param>
            /// <param name="typeName">Stream type name.</param>
            /// <param name="partitionName">Partition/file name.</param>
            /// <param name="partitionPath">Partition/file path.</param>
            /// <param name="first">First message time.</param>
            /// <param name="last">Last message time.</param>
            /// <param name="messageCount">Total message count.</param>
            /// <param name="averageMessageSize">Average message size (bytes).</param>
            /// <param name="averageLatencyMs">Average message latency (milliseconds).</param>
            internal WaveAudioStreamMetadata(string name, string typeName, string partitionName, string partitionPath, DateTime first, DateTime last, long messageCount, double averageMessageSize, double averageLatencyMs)
                : base(name, AudioSourceId, typeName, partitionName, partitionPath, first, last, messageCount, averageMessageSize, averageLatencyMs)
            {
            }
        }
    }
}

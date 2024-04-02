// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that implements a spatial sound renderer.
    /// </summary>
    public class SpatialSound : StereoKitComponent, IConsumerProducer<AudioBuffer, AudioBuffer>, ISourceComponent
    {
        private const int SamplesPerSecond = 48000; // audio sample rate must be 48kHz
        private readonly ConcurrentQueue<AudioBuffer> audioQueue = new ();
        private readonly float bufferSizeInSeconds;
        private readonly int bufferSizeInSamples;
        private Sound sound;
        private SoundInst soundInst;
        private Point3D worldPosition;
        private float volume;
        private bool playing = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialSound"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="initialPosition">Initial position of spatial sound.</param>
        /// <param name="initialVolume">Intial audio volume (0-1, default 1).</param>
        /// <param name="bufferSizeInSeconds">The amount of audio in seconds that can be buffered.</param>
        /// <param name="name">An optional name for the component.</param>
        public SpatialSound(Pipeline pipeline, Point3D initialPosition, double initialVolume = 1, double bufferSizeInSeconds = 2f, string name = nameof(SpatialSound))
            : base(pipeline, name)
        {
            this.worldPosition = initialPosition;
            this.volume = (float)initialVolume;
            this.bufferSizeInSeconds = (float)bufferSizeInSeconds;
            this.bufferSizeInSamples = (int)(this.bufferSizeInSeconds * SamplesPerSecond);
            this.PositionInput = pipeline.CreateReceiver<Point3D>(this, p => this.worldPosition = p, nameof(this.PositionInput));
            this.VolumeInput = pipeline.CreateReceiver<double>(this, v => this.volume = (float)v, nameof(this.VolumeInput));
            this.In = pipeline.CreateReceiver<AudioBuffer>(this, this.UpdateAudio, nameof(this.In));
            this.Out = pipeline.CreateEmitter<AudioBuffer>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the receiver of audio.
        /// </summary>
        public Receiver<AudioBuffer> In { get; private set; }

        /// <summary>
        /// Gets receiver for spatial pose.
        /// </summary>
        public Receiver<Point3D> PositionInput { get; private set; }

        /// <summary>
        /// Gets receiver for audio volume.
        /// </summary>
        public Receiver<double> VolumeInput { get; private set; }

        /// <summary>
        /// Gets emitter for the played audio.
        /// </summary>
        public Emitter<AudioBuffer> Out { get; private set; }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime) => notifyCompletionTime(DateTime.MaxValue);

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.playing = false;
            while (this.audioQueue.TryDequeue(out var audio))
            {
                this.In.Recycle(audio);
            }

            notifyCompleted();
        }

        /// <inheritdoc />
        public override bool Initialize()
        {
            this.sound = Sound.CreateStream(this.bufferSizeInSeconds);
            this.soundInst = this.sound.Play(this.ComputeSoundPosition(), this.volume);
            this.playing = true;
            return true;
        }

        /// <inheritdoc/>
        public override void Step()
        {
            if (this.playing)
            {
                this.soundInst.Volume = this.volume;

                if (StereoKitTransforms.WorldToStereoKit is not null)
                {
                    this.soundInst.Position = this.ComputeSoundPosition();
                }

                // Play any queued audio as long as there is enough space in the sound buffer.
                while (this.audioQueue.TryPeek(out var audio) &&
                    (audio.Length / 4) <= (this.bufferSizeInSamples - this.sound.UnreadSamples))
                {
                    // Play the audio first, then dequeue it. This ensures that we never attempt to
                    // call PlayAudio in the receiver while we are servicing the audio queue here.
                    this.PlayAudio(audio);
                    this.audioQueue.TryDequeue(out _);

                    // Recycle the audio buffer back to the receiver for reuse.
                    this.In.Recycle(audio);
                }
            }
        }

        private Vec3 ComputeSoundPosition()
        {
            if (StereoKitTransforms.WorldToStereoKit is null)
            {
                return Vec3.Zero;
            }
            else
            {
                return this.worldPosition.TransformBy(StereoKitTransforms.WorldToStereoKit).ToVec3();
            }
        }

        private void UpdateAudio(AudioBuffer audio)
        {
            var format = audio.Format;
            if (format.Channels != 1 ||
                format.SamplesPerSec != SamplesPerSecond ||
                (format.FormatTag != WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT &&
                 format.FormatTag != WaveFormatTag.WAVE_FORMAT_EXTENSIBLE) ||
                format.BitsPerSample != 32)
            {
                throw new ArgumentException("Expected 1-channel, 48kHz, float32 audio format.");
            }

            if (audio.Duration.TotalSeconds > this.bufferSizeInSeconds)
            {
                throw new InvalidOperationException($"Audio length exceeds internal buffer capacity. Try increasing the {nameof(this.bufferSizeInSeconds)}.");
            }

            var sampleCount = audio.Length / 4;

            // If the audio queue is empty and there is enough space in the sound buffer, play the audio immediately.
            if (this.playing && this.audioQueue.IsEmpty && (this.bufferSizeInSamples - this.sound.UnreadSamples) >= sampleCount)
            {
                this.PlayAudio(audio);
            }
            else
            {
                // Enqueue the audio for later playback.
                this.audioQueue.Enqueue(audio.DeepClone(this.In.Recycler));
            }
        }

        private void PlayAudio(AudioBuffer audio)
        {
            if (this.playing)
            {
                using var stream = new MemoryStream(audio.Data, 0, audio.Length);
                using var reader = new BinaryReader(stream);
                var count = audio.Length / 4;
                var samples = new float[count];
                for (var i = 0; i < count; i++)
                {
                    samples[i] = reader.ReadSingle();
                }

                this.sound.WriteSamples(samples);

                // The estimated audio playback time is the current time plus the duration of any unread audio samples in the buffer.
                var playbackOriginatingTime = this.Out.Pipeline.GetCurrentTime() + TimeSpan.FromSeconds((double)this.sound.UnreadSamples / SamplesPerSecond);

                this.Out.Post(audio, this.GetCompliantOriginatingTime(playbackOriginatingTime, this.Out));
            }
        }

        private DateTime GetCompliantOriginatingTime(DateTime originatingTime, IEmitter emitter)
            => originatingTime <= emitter.LastEnvelope.OriginatingTime ? emitter.LastEnvelope.OriginatingTime.AddTicks(1) : originatingTime;
    }
}

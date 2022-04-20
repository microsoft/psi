// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.IO;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Audio;
    using StereoKit;

    /// <summary>
    /// Component that implements a spatial sound renderer.
    /// </summary>
    public class SpatialSound : StereoKitComponent, IConsumer<AudioBuffer>
    {
        private Sound sound;
        private SoundInst soundInst;
        private Vec3 position;
        private float volume;
        private bool playing = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialSound"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="initialPosition">Initial position of spatial sound.</param>
        /// <param name="initialVolume">Intial audio volume (0-1, default 1).</param>
        /// <param name="name">An optional name for the component.</param>
        public SpatialSound(Pipeline pipeline, Point3D initialPosition, double initialVolume = 1, string name = nameof(SpatialSound))
            : base(pipeline, name)
        {
            this.position = new CoordinateSystem(initialPosition, UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis).ToStereoKitMatrix().Translation;
            this.volume = (float)initialVolume;
            this.In = pipeline.CreateReceiver<AudioBuffer>(this, this.UpdateAudio, nameof(this.In));
            this.PositionInput = pipeline.CreateReceiver<Point3D>(this, this.UpdatePosition, nameof(this.PositionInput));
            this.VolumeInput = pipeline.CreateReceiver<double>(this, this.UpdateVolume, nameof(this.VolumeInput));
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

        /// <inheritdoc />
        public override bool Initialize()
        {
            this.sound = Sound.CreateStream(2f);
            return true;
        }

        private void UpdateAudio(AudioBuffer audio)
        {
            var format = audio.Format;
            if (format.Channels != 1 ||
                format.SamplesPerSec != 48000 ||
                (format.FormatTag != WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT &&
                 format.FormatTag != WaveFormatTag.WAVE_FORMAT_EXTENSIBLE) ||
                format.BitsPerSample != 32)
            {
                throw new ArgumentException("Expected 1-channel, 48kHz, float32 audio format.");
            }

            using var stream = new MemoryStream(audio.Data, 0, audio.Length);
            using var reader = new BinaryReader(stream);
            var count = audio.Length / 4;
            var samples = new float[count];
            for (var i = 0; i < count; i++)
            {
                samples[i] = reader.ReadSingle();
            }

            this.sound.WriteSamples(samples);
            if (!this.playing)
            {
                this.soundInst = this.sound.Play(this.position, this.volume);
                this.playing = true;
            }
        }

        private void UpdatePosition(Point3D position)
        {
            this.position = position.TransformBy(StereoKitTransforms.WorldToStereoKit).ToVec3();
            if (this.playing)
            {
                this.soundInst.Position = this.position;
            }
        }

        private void UpdateVolume(double volume)
        {
            this.volume = (float)volume;
            if (this.playing)
            {
                this.soundInst.Volume = this.volume;
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.IO;
    using Microsoft.Psi;

    /// <summary>
    /// Component that streams audio from a WAVE file.
    /// </summary>
    public sealed class WaveFileAudioSource : IProducer<AudioBuffer>
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileAudioSource"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="path">The path name of the WAVE file.</param>
        /// <param name="audioStartTime">Indicates a time to use for the start of the audio source. If null, the current time will be used.</param>
        /// <param name="audioBufferSizeMs">The size of each data buffer to post, determined by the amount of audio data it can hold.</param>
        /// <param name="name">An optional name for this component.</param>
        public WaveFileAudioSource(Pipeline pipeline, string path, DateTime? audioStartTime = null, int audioBufferSizeMs = 20, string name = nameof(WaveFileAudioSource))
        {
            this.name = name;

            var filename = Path.GetFileName(path);
            var directoryName = Path.GetDirectoryName(path);
            var importer = WaveFileStore.Open(pipeline, filename, directoryName, audioStartTime ?? DateTime.UtcNow, audioBufferSizeMs);
            var audio = importer.OpenStream<AudioBuffer>(WaveFileStreamReader.AudioStreamName);
            this.Out = audio.Out;
        }

        /// <inheritdoc />
        public Emitter<AudioBuffer> Out { get; private set; }

        /// <inheritdoc/>
        public override string ToString() => this.name;
    }
}

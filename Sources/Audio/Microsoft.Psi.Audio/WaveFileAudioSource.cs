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
        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileAudioSource"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="filename">The path name of the WAVE file.</param>
        /// <param name="audioStartTime">Indicates a time to use for the start of the audio source. If null, the current time will be used.</param>
        /// <param name="audioBufferSizeMs">The size of each data buffer to post, determined by the amount of audio data it can hold.</param>
        public WaveFileAudioSource(Pipeline pipeline, string filename, DateTime? audioStartTime = null, int audioBufferSizeMs = 20)
        {
            var name = Path.GetFileName(filename);
            var path = Path.GetDirectoryName(filename);
            var importer = WaveFileStore.Open(pipeline, name, path, audioStartTime ?? DateTime.UtcNow, audioBufferSizeMs);
            var audio = importer.OpenStream<AudioBuffer>(WaveFileStreamReader.AudioStreamName);
            this.Out = audio.Out;
        }

        /// <inheritdoc />
        public Emitter<AudioBuffer> Out { get; private set; }
    }
}

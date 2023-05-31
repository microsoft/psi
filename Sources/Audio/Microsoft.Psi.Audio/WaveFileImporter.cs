// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Component that reads messages from a WAVE file and publishes them on a stream.
    /// </summary>
    public sealed class WaveFileImporter : Importer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileImporter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">The name of the application that generated the persisted files, or the root name of the files.</param>
        /// <param name="path">The directory in which the main persisted file resides or will reside, or null to open a volatile data store.</param>
        /// <param name="startTime">Starting time for streams of data..</param>
        /// <param name="audioBufferSizeMs">The size of each data buffer in milliseconds.</param>
        public WaveFileImporter(Pipeline pipeline, string name, string path, DateTime startTime, int audioBufferSizeMs = WaveFileStreamReader.DefaultAudioBufferSizeMs)
            : base(pipeline, new WaveFileStreamReader(name, path, startTime, audioBufferSizeMs), false)
        {
        }
    }
}

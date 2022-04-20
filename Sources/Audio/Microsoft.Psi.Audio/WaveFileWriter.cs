// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that writes an audio stream into a WAVE file.
    /// </summary>
    public sealed class WaveFileWriter : SimpleConsumer<AudioBuffer>, IDisposable
    {
        private readonly string outputFilename;
        private WaveDataWriterClass writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFileWriter"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="filename">The path name of the Wave file.</param>
        /// <param name="name">An optional name for this component.</param>
        public WaveFileWriter(Pipeline pipeline, string filename, string name = nameof(WaveFileWriter))
            : base(pipeline, name)
        {
            this.outputFilename = filename;
        }

        /// <summary>
        /// Disposes the component.
        /// </summary>
        public void Dispose()
        {
            if (this.writer != null)
            {
                this.writer.Dispose();
                this.writer = null;
            }
        }

        /// <summary>
        /// The receiver for the audio messages.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        public override void Receive(Message<AudioBuffer> message)
        {
            if (this.writer == null)
            {
                var format = message.Data.Format.DeepClone();
                this.writer = new WaveDataWriterClass(new FileStream(this.outputFilename, FileMode.Create), format);
            }

            this.writer.Write(message.Data.Data);
        }
    }
}
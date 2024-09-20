// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Batch task that exports audio streams to a wav file.
    /// </summary>
    [BatchProcessingTask(
        "Export Audio to Wav File",
        Description = "This task exports an audio stream to a wav file.")]
    public class ExportAudioToWavFileTask : BatchProcessingTask<ExportAudioToWavFileTaskConfiguration>
    {
        /// <inheritdoc/>
        public override void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, ExportAudioToWavFileTaskConfiguration configuration)
        {
            var audio = sessionImporter.OpenStream<AudioBuffer>(configuration.AudioStreamName);
            var partition = sessionImporter.PartitionImporters.Values.First();
            var wavFileWriter = new WaveFileWriter(pipeline, Path.Combine(partition.StorePath, configuration.WavOutputFilename));

            var streamlinedAudio = audio.Streamline(configuration.AudioStreamlineMethod, configuration.MaxOffsetBeforeUnpleatedRealignmentMs);
            if (exporter != null)
            {
                streamlinedAudio.Write(configuration.OutputAudioStreamName, exporter);
            }

            streamlinedAudio.PipeTo(wavFileWriter);
        }
    }

    /// <summary>
    /// Represents the configuration for the <see cref="ExportAudioToWavFileTask"/>.
    /// </summary>
#pragma warning disable SA1402 // File may only contain a single type
    public class ExportAudioToWavFileTaskConfiguration : BatchProcessingTaskConfiguration
#pragma warning restore SA1402 // File may only contain a single type
    {
        private string audioStreamName = "Audio";
        private string outputAudioStreamName = "Audio";
        private string wavOutputFilename = "Audio.wav";
        private AudioStreamlineMethod audioStreamlineMethod = AudioStreamlineMethod.Unpleat;
        private double maxOffsetBeforeUnpleatedRealignmentMs = 20;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportAudioToWavFileTaskConfiguration"/> class.
        /// </summary>
        public ExportAudioToWavFileTaskConfiguration()
            : base()
        {
            this.OutputStoreName = string.Empty;
            this.OutputPartitionName = string.Empty;
            this.DeliveryPolicySpec = DeliveryPolicySpec.Throttle;
            this.ReplayAllRealTime = false;
        }

        /// <summary>
        /// Gets or sets the name of the audio stream.
        /// </summary>
        [DataMember]
        [DisplayName("Audio Stream Name")]
        [Description("The name of the audio stream.")]
        public string AudioStreamName
        {
            get => this.audioStreamName;
            set { this.Set(nameof(this.AudioStreamName), ref this.audioStreamName, value); }
        }

        /// <summary>
        /// Gets or sets the name of the output audio stream.
        /// </summary>
        [DataMember]
        [DisplayName("Output Audio Stream Name")]
        [Description("The name of the output audio stream.")]
        public string OutputAudioStreamName
        {
            get => this.outputAudioStreamName;
            set { this.Set(nameof(this.OutputAudioStreamName), ref this.outputAudioStreamName, value); }
        }

        /// <summary>
        /// Gets or sets the name of a wave output file.
        /// </summary>
        [DataMember]
        [DisplayName("Wave Output Filename")]
        [Description("The filename is relative to the partition folder.")]
        public string WavOutputFilename
        {
            get => this.wavOutputFilename;
            set { this.Set(nameof(this.WavOutputFilename), ref this.wavOutputFilename, value); }
        }

        /// <summary>
        /// Gets or sets the method used to streamline the audio stream.
        /// </summary>
        [DataMember]
        [DisplayName("Audio Streamline Method")]
        [Description("The method used to streamline the audio stream.")]
        public AudioStreamlineMethod AudioStreamlineMethod
        {
            get => this.audioStreamlineMethod;
            set { this.Set(nameof(this.AudioStreamlineMethod), ref this.audioStreamlineMethod, value); }
        }

        /// <summary>
        /// Gets or sets the maximum offset before realignment in milliseconds.
        /// </summary>
        [DataMember]
        [DisplayName("Max Offset Before Realignment (ms)")]
        [Description("The maximum time offset between the unpleated stream and originating times before a realignment is enforced (in milliseconds).")]
        public double MaxOffsetBeforeUnpleatedRealignmentMs
        {
            get => this.maxOffsetBeforeUnpleatedRealignmentMs;
            set { this.Set(nameof(this.MaxOffsetBeforeUnpleatedRealignmentMs), ref this.maxOffsetBeforeUnpleatedRealignmentMs, value); }
        }
    }
}
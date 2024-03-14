// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Enumeration flag for configuration constructors.
    /// </summary>
    public enum ConfigurationType
    {
        /// <summary>
        /// Construct the default configuration.
        /// </summary>
        Default,

        /// <summary>
        /// Construct template configuration.
        /// </summary>
        Template,
    }

    /// <summary>
    /// Represents a configuration for the compute server pipeline.
    /// </summary>
    public class ComputeServerPipelineConfiguration : ObservableObject
    {
        private string name = null;
        private SpeechRecognitionPipelineConfiguration speechRecognitionPipelineConfiguration = new ();
        private string outputPath = null;
        private bool reEncodePreviewStream = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeServerPipelineConfiguration"/> class.
        /// </summary>
        public ComputeServerPipelineConfiguration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeServerPipelineConfiguration"/> class.
        /// </summary>
        /// <param name="configurationType">The type of configuration to construct.</param>
        public ComputeServerPipelineConfiguration(ConfigurationType configurationType = ConfigurationType.Default)
        {
            if (configurationType == ConfigurationType.Template)
            {
                this.Name = "please fill in the configuration name";
                this.OutputPath = "please fill in the path to the output logs";
                this.ReEncodePreviewStream = false;
            }
        }

        /// <summary>
        /// Gets or sets the name of the configuration.
        /// </summary>
        [DataMember]
        public string Name
        {
            get => this.name;
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets or sets the speech recognition pipeline configuration.
        /// </summary>
        [DataMember]
        public SpeechRecognitionPipelineConfiguration SpeechRecognitionPipelineConfiguration
        {
            get => this.speechRecognitionPipelineConfiguration;
            set { this.Set(nameof(this.SpeechRecognitionPipelineConfiguration), ref this.speechRecognitionPipelineConfiguration, value); }
        }

        /// <summary>
        /// Gets or sets the output path.
        /// </summary>
        [DataMember]
        public string OutputPath
        {
            get => this.outputPath;
            set { this.Set(nameof(this.OutputPath), ref this.outputPath, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to reencode the preview stream.
        /// </summary>
        [DataMember]
        public bool ReEncodePreviewStream
        {
            get => this.reEncodePreviewStream;
            set { this.Set(nameof(this.ReEncodePreviewStream), ref this.reEncodePreviewStream, value); }
        }
    }
}

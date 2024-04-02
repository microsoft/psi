// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents the configuration for the speech recognition pipeline.
    /// </summary>
    public class SpeechRecognitionPipelineConfiguration : ObservableObject
    {
        private bool useContinuationDetector = false;
        private SpeechRecognitionContinuationDetectorConfiguration continuationDetectorConfiguration = new ();

        /// <summary>
        /// Gets or sets a value indicating whether to use the continuation detector.
        /// </summary>
        [DataMember]
        [DisplayName("Use Continuation Detector")]
        [Description("Indicates whether to use a continuation detector.")]
        public bool UseContinuationDetector
        {
            get => this.useContinuationDetector;
            set { this.Set(nameof(this.UseContinuationDetector), ref this.useContinuationDetector, value); }
        }

        /// <summary>
        /// Gets or sets the name of the configuration.
        /// </summary>
        [DataMember]
        [DisplayName("Speech Continuation Detector")]
        [Description("The configuration for the speech recognition continuation detector.")]
        public SpeechRecognitionContinuationDetectorConfiguration ContinuationDetectorConfiguration
        {
            get => this.continuationDetectorConfiguration;
            set { this.Set(nameof(this.ContinuationDetectorConfiguration), ref this.continuationDetectorConfiguration, value); }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    /// <summary>
    /// Represents the configuration for the <see cref="SimpleVoiceActivityDetector"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="SimpleVoiceActivityDetector"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class SimpleVoiceActivityDetectorConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleVoiceActivityDetectorConfiguration"/> class.
        /// </summary>
        public SimpleVoiceActivityDetectorConfiguration()
        {
            this.FrameDuration = 0.02;
            this.FrameRate = 100;
            this.LogEnergyThreshold = 7;
            this.VoiceActivityDetectionWindow = 0.1;
            this.SilenceDetectionWindow = 0.5;
        }

        /// <summary>
        /// Gets or sets the audio frame duration in seconds.
        /// </summary>
        public double FrameDuration { get; set; }

        /// <summary>
        /// Gets or sets the frame rate in frames per second.
        /// </summary>
        public double FrameRate { get; set; }

        /// <summary>
        /// Gets or sets the log energy detection threshold for voice activity.
        /// </summary>
        public int LogEnergyThreshold { get; set; }

        /// <summary>
        /// Gets or sets the voice activity detection window in seconds.
        /// </summary>
        public double VoiceActivityDetectionWindow { get; set; }

        /// <summary>
        /// Gets or sets the silence detection window in seconds.
        /// </summary>
        public double SilenceDetectionWindow { get; set; }
    }
}

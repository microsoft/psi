// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    /// <summary>
    /// Represents the configuration for the <see cref="AudioCapture"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="AudioCapture"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class AudioCaptureConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioCaptureConfiguration"/> class.
        /// </summary>
        public AudioCaptureConfiguration()
        {
            this.DeviceName = string.Empty;
            this.TargetLatencyInMs = 20;
            this.AudioLevel = -1;
            this.Gain = 1.0f;
            this.OptimizeForSpeech = false;
            this.DropOutOfOrderPackets = false;
            this.OutputFormat = null;
        }

        /// <summary>
        /// Gets or sets the name of the audio source device.
        /// </summary>
        /// <remarks>
        /// Use this to specify the name of the audio recording device from which to capture audio.
        /// To obtain a list of available recording devices on the system, use the
        /// <see cref="AudioCapture.GetAvailableDevices"/> static method. If not specified, the
        /// default recording device will be selected.
        /// </remarks>
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the target audio latency.
        /// </summary>
        /// <remarks>
        /// Captured audio will be output as a stream of type <see cref="AudioBuffer"/> in the
        /// <see cref="AudioCapture"/> component. This parameter controls the amount of audio to capture
        /// for each <see cref="AudioBuffer"/> message, which in turn determines the latency of the
        /// audio (i.e. the amount of lag between when the audio was produced and when a captured
        /// <see cref="AudioBuffer"/> is output on the stream). The larger this value, the more audio
        /// data is carried in each <see cref="AudioBuffer"/> and the longer the audio latency. For
        /// live audio capture, we normally want this value to be small as possible, with the lower
        /// bound being constrained by the audio capture pipeline. By default, this value is set to
        /// 20 milliseconds. Is is safe to leave this unchanged.
        /// </remarks>
        public int TargetLatencyInMs { get; set; }

        /// <summary>
        /// Gets or sets the audio input level.
        /// </summary>
        /// <remarks>
        /// This is the initial level to set the audio recording device to. Valid values range
        /// between 0.0 and 1.0 inclusive. If not specified, the current level of the selected
        /// recording device will be left unchanged.
        /// </remarks>
        public double AudioLevel { get; set; }

        /// <summary>
        /// Gets or sets the additional gain to be applied to the captured audio.
        /// </summary>
        /// <remarks>
        /// This specifies an additional gain which may be applied computationally to the captured
        /// audio signal. Values greater than 1.0 boost the audio signal, while values in the range
        /// of 0.0 to 1.0 attenuate it. The default value is 1.0 (no additional gain).
        /// </remarks>
        public float Gain { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the captured audio should be pre-processed for
        /// speech recognition applications. Available in Windows 10 Creators Update or later.
        /// </summary>
        /// <remarks>
        /// In later versions of Windows 10, the audio capture pipeline optionally enables additional
        /// signal processing to apply enhancements such as echo and noise suppression to optimize
        /// the audio for speech recognition applications. By default, this option is set to false.
        /// This feature may not be available for all capture devices.
        /// </remarks>
        public bool OptimizeForSpeech { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the component should
        /// discard captured audio packets with an out-of-order timestamp.
        /// </summary>
        /// <remarks>
        /// This is for internal use only and may be removed in future versions.
        /// </remarks>
        public bool DropOutOfOrderPackets { get; set; }

        /// <summary>
        /// Gets or sets the desired format for the captured audio.
        /// </summary>
        /// <remarks>
        /// By default, audio will be captured in the default format of the audio recording device.
        /// Use this to specify a different format for the <see cref="AudioBuffer"/> Out stream of
        /// the <see cref="AudioCapture"/> component.
        /// </remarks>
        public WaveFormat OutputFormat { get; set; }
    }
}

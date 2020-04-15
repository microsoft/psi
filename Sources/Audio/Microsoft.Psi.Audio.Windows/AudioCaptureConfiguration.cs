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
            this.AudioEngineBufferInMs = 500;
            this.AudioLevel = -1;
            this.Gain = 1.0f;
            this.OptimizeForSpeech = false;
            this.UseEventDrivenCapture = true;
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
        /// Gets or sets the target audio latency (pull capture mode only).
        /// </summary>
        /// <remarks>
        /// In pull capture mode, this parameter determines the interval at which the audio engine is
        /// polled for new data. This in turn affects the latency of the captured audio (i.e. the amount
        /// of lag between when the audio was produced and when a captured <see cref="AudioBuffer"/> is
        /// output on the stream). The larger this value, the more audio data is captured at each interval,
        /// and the larger the audio latency. For live audio capture, we normally want this value to be as
        /// small as possible. By default, this value is set to 20 milliseconds. This value is ignored if
        /// <see cref="UseEventDrivenCapture"/> is set to true. In event-driven capture mode, the latency
        /// is determined by the rate at which the audio engine signals that it has new data available.
        /// </remarks>
        public int TargetLatencyInMs { get; set; }

        /// <summary>
        /// Gets or sets the audio engine buffer.
        /// </summary>
        /// <remarks>
        /// This parameter controls the amount of audio that the audio capture engine is able to
        /// buffer between reads. This determines the maximum delay that may be incurred between
        /// reading two consecutive audio buffers before an overrun occurs, which may lead to
        /// glitches due to loss of audio, and allows additional audio packets to be queued up
        /// in the engine should the application occasionally not be able to consume the captured
        /// audio packets fast enough. Setting this to a larger value reduces the likelihood of
        /// encountering glitches in the captured audio stream.
        /// </remarks>
        public int AudioEngineBufferInMs { get; set; }

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
        /// Gets or sets a value indicating whether to use event-driven or pull capture mode. When using
        /// event-driven capture, audio is captured as soon as the audio engine signals that there is
        /// data available, instead of intervals determined by the <see cref="TargetLatencyInMs"/>
        /// property. When this value is set to false, the audio engine is polled at an interval
        /// equal to the value specified by <see cref="TargetLatencyInMs"/>. Additional data may be buffered
        /// by the audio engine (up to an amount equivalent to <see cref="AudioEngineBufferInMs"/>) should
        /// the application be unable to consume the audio data quickly enough.
        /// </summary>
        public bool UseEventDrivenCapture { get; set; }

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

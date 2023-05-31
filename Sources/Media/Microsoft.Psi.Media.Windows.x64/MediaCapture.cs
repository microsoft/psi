// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media_Interop;

    /// <summary>
    /// Component that captures and streams video and audio from a camera.
    /// </summary>
    public class MediaCapture : IProducer<Shared<Image>>, ISourceComponent, IDisposable, IMediaCapture
    {
        private readonly Pipeline pipeline;
        private readonly string name;

        /// <summary>
        /// The video camera configuration.
        /// </summary>
        private readonly MediaCaptureConfiguration configuration;

        private readonly IProducer<Audio.AudioBuffer> audio;

        /// <summary>
        /// The video capture device.
        /// </summary>
        private MediaCaptureDevice camera;

        /// <summary>
        /// Defines attributes of properties exposed by MediaCaptureDevice.
        /// </summary>
        private MediaCaptureInfo deviceInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">Name of file containing media capture device configuration.</param>
        /// <param name="name">An optional name for the component.</param>
        public MediaCapture(Pipeline pipeline, string configurationFilename, string name = nameof(MediaCapture))
        : this(pipeline)
        {
            this.name = name;
            var configurationHelper = new ConfigurationHelper<MediaCaptureConfiguration>(configurationFilename);
            this.configuration = configurationHelper.Configuration;
            if (this.configuration.CaptureAudio)
            {
                this.audio = new Audio.AudioCapture(pipeline, Psi.Audio.WaveFormat.Create16kHz1Channel16BitPcm());
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">Describes how to configure the media capture device.</param>
        /// <param name="name">An optional name for the component.</param>
        public MediaCapture(Pipeline pipeline, MediaCaptureConfiguration configuration = null, string name = nameof(MediaCapture))
            : this(pipeline, name)
        {
            this.configuration = configuration ?? new MediaCaptureConfiguration();
            if (this.configuration.CaptureAudio)
            {
                this.audio = new Audio.AudioCapture(pipeline, Psi.Audio.WaveFormat.Create16kHz1Channel16BitPcm());
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaCapture"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="width">Width of output image in pixels.</param>
        /// <param name="height">Height of output image in pixels.</param>
        /// <param name="framerate">Frame rate.</param>
        /// <param name="captureAudio">Should we create an audio capture device.</param>
        /// <param name="deviceId">Device ID.</param>
        /// <param name="useInSharedMode">Indicates whether camera is shared amongst multiple applications.</param>
        /// <param name="name">An optional name for the component.</param>
        public MediaCapture(
            Pipeline pipeline,
            int width,
            int height,
            double framerate = 30,
            bool captureAudio = false,
            string deviceId = null,
            bool useInSharedMode = false,
            string name = nameof(MediaCapture))
            : this(pipeline, name)
        {
            this.configuration = new MediaCaptureConfiguration()
            {
                UseInSharedMode = useInSharedMode,
                DeviceId = deviceId,
                Width = width,
                Height = height,
                Framerate = framerate,
                CaptureAudio = captureAudio,
            };
            if (this.configuration.CaptureAudio)
            {
                this.audio = new Audio.AudioCapture(pipeline, Psi.Audio.WaveFormat.Create16kHz1Channel16BitPcm());
            }
        }

        private MediaCapture(Pipeline pipeline, string name)
        {
            this.name = name;
            this.pipeline = pipeline;
            this.Out = pipeline.CreateEmitter<Shared<Image>>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the emitter for the audio stream.
        /// </summary>
        public Emitter<Audio.AudioBuffer> Audio
        {
            get { return this.audio?.Out; }
            private set { }
        }

        /// <summary>
        /// Gets the emitter for the video stream.
        /// </summary>
        public Emitter<Shared<Image>> Video => this.Out;

        /// <summary>
        /// Gets the output stream of images.
        /// </summary>
        public Emitter<Shared<Image>> Out { get; private set; }

        /// <summary>
        /// Returns information about each property exposed by the media capture device.
        /// </summary>
        /// <returns>MediaCaptureInfo object defining ranges and availability of each property.</returns>
        public MediaCaptureInfo GetDeviceInfo()
        {
            return this.deviceInfo;
        }

        /// <summary>
        /// Returns the current configuration for the media capture device.
        /// </summary>
        /// <returns>A new MediaCaptureConfiguration object with the device's current settings.</returns>
        public MediaCaptureConfiguration GetDeviceConfiguration()
        {
            MediaCaptureConfiguration config = new MediaCaptureConfiguration
            {
                BacklightCompensation = this.GetValueBool(VideoProperty.BacklightCompensation, this.deviceInfo.BacklightCompensationInfo.Supported),
                Brightness = this.GetValueInt(VideoProperty.Brightness, this.deviceInfo.BrightnessInfo.Supported),
                ColorEnable = this.GetValueBool(VideoProperty.ColorEnable, this.deviceInfo.ColorEnableInfo.Supported),
                Contrast = this.GetValueInt(VideoProperty.Contrast, this.deviceInfo.ContrastInfo.Supported),
                Gain = this.GetValueInt(VideoProperty.Gain, this.deviceInfo.GainInfo.Supported),
                Gamma = this.GetValueInt(VideoProperty.Gamma, this.deviceInfo.GammaInfo.Supported),
                Hue = this.GetValueInt(VideoProperty.Hue, this.deviceInfo.HueInfo.Supported),
                Saturation = this.GetValueInt(VideoProperty.Saturation, this.deviceInfo.SaturationInfo.Supported),
                Sharpness = this.GetValueInt(VideoProperty.Sharpness, this.deviceInfo.SharpnessInfo.Supported),
                WhiteBalance = this.GetValueInt(VideoProperty.WhiteBalance, this.deviceInfo.WhiteBalanceInfo.Supported),
                Focus = this.GetValueInt(ManagedCameraControlProperty.Focus, this.deviceInfo.FocusInfo.Supported),
            };
            return config;
        }

        /// <summary>
        /// Assigns the specified configuration to the media capture device.
        /// </summary>
        /// <param name="config">Configuration to set on media capture device.</param>
        public void SetDeviceConfiguration(MediaCaptureConfiguration config)
        {
            this.SetDeviceProperty(VideoProperty.BacklightCompensation, this.deviceInfo.BacklightCompensationInfo, config.BacklightCompensation);
            this.SetDeviceProperty(VideoProperty.Brightness, this.deviceInfo.BrightnessInfo, config.Brightness);
            this.SetDeviceProperty(VideoProperty.ColorEnable, this.deviceInfo.ColorEnableInfo, config.ColorEnable);
            this.SetDeviceProperty(VideoProperty.Contrast, this.deviceInfo.ContrastInfo, config.Contrast);
            this.SetDeviceProperty(VideoProperty.Gain, this.deviceInfo.GainInfo, config.Gain);
            this.SetDeviceProperty(VideoProperty.Gamma, this.deviceInfo.GammaInfo, config.Gamma);
            this.SetDeviceProperty(VideoProperty.Hue, this.deviceInfo.HueInfo, config.Hue);
            this.SetDeviceProperty(VideoProperty.Saturation, this.deviceInfo.SaturationInfo, config.Saturation);
            this.SetDeviceProperty(VideoProperty.Sharpness, this.deviceInfo.SharpnessInfo, config.Sharpness);
            this.SetDeviceProperty(VideoProperty.WhiteBalance, this.deviceInfo.WhiteBalanceInfo, config.WhiteBalance);
            this.SetDeviceProperty(ManagedCameraControlProperty.Focus, this.deviceInfo.FocusInfo, config.Focus);
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            // check for null since it's possible that Start was never called
            if (this.camera != null)
            {
                this.camera.Shutdown();
                this.camera.Dispose();
                this.camera = null;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            MediaCaptureDevice.Initialize();
            CaptureFormat found = null;
            foreach (var device in MediaCaptureDevice.AllDevices)
            {
                if (!device.Attach(this.configuration.UseInSharedMode))
                {
                    continue;
                }

               // Trace.WriteLine($"MediaCapture - Searching for width={this.configuration.Width} height={this.configuration.Height} deviceId={this.configuration.DeviceId}");
               // Trace.WriteLine($"MediaCapture - Found: Name: '{device.FriendlyName}' SymLink: {device.SymbolicLink}");
               // Trace.WriteLine($"MediaCapture -   Current   - Width: {device.CurrentFormat.nWidth} Height: {device.CurrentFormat.nHeight} Type: {device.CurrentFormat.subType.Name}/{device.CurrentFormat.subType.Guid} Framerate: {device.CurrentFormat.nFrameRateNumerator}/{device.CurrentFormat.nFrameRateDenominator}");
                if (string.IsNullOrEmpty(this.configuration.DeviceId) || device.FriendlyName == this.configuration.DeviceId || device.SymbolicLink == this.configuration.DeviceId)
                {
                    foreach (var format in device.Formats)
                    {
                      // Trace.WriteLine($"MediaCapture -   Supported - Width: {format.nWidth} Height: {format.nHeight} Type: {format.subType.Name}/{format.subType.Guid} Framerate: {format.nFrameRateNumerator}/{format.nFrameRateDenominator}");
                        if (this.configuration.Width == format.nWidth && this.configuration.Height == format.nHeight)
                        {
                            // found suitable width/height
                            if (this.configuration.Framerate == format.nFrameRateNumerator / format.nFrameRateDenominator)
                            {
                                // found suitable framerate
                                if (found == null || this.configuration.Framerate == found.nFrameRateNumerator / found.nFrameRateDenominator)
                                {
                                    // found first suitable or closer framerate match
                                    this.camera = device;
                                    found = format;
                                }
                            }
                        }
                    }
                }

                if (found != null)
                {
                    // Trace.WriteLine($"MediaCapture - Using - Width: {found.nWidth} Height: {found.nHeight} Type: {found.subType.Name}/{found.subType.Guid} Framerate: {found.nFrameRateNumerator}/{found.nFrameRateDenominator}");
                    break;
                }
            }

            if (found != null)
            {
                this.camera.CurrentFormat = found;
                this.deviceInfo = new MediaCaptureInfo(this.camera);
                var width = this.camera.CurrentFormat.nWidth;
                var height = this.camera.CurrentFormat.nHeight;

                // Get default settings for other properties.
                var currentConfig = this.GetDeviceConfiguration();
                this.configuration.BacklightCompensation = currentConfig.BacklightCompensation;
                this.configuration.Brightness = currentConfig.Brightness;
                this.configuration.ColorEnable = currentConfig.ColorEnable;
                this.configuration.Contrast = currentConfig.Contrast;
                this.configuration.Gain = currentConfig.Gain;
                this.configuration.Gamma = currentConfig.Gamma;
                this.configuration.Hue = currentConfig.Hue;
                this.configuration.Saturation = currentConfig.Saturation;
                this.configuration.Sharpness = currentConfig.Sharpness;
                this.configuration.WhiteBalance = currentConfig.WhiteBalance;
                this.configuration.Focus = currentConfig.Focus;

                this.SetDeviceConfiguration(this.configuration);

                this.camera.CaptureSample((data, length, timestamp) =>
                {
                    using var sharedImage = ImagePool.GetOrCreate(this.configuration.Width, this.configuration.Height, PixelFormat.BGR_24bpp);
                    sharedImage.Resource.CopyFrom(data);

                    var originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks(timestamp);

                    // Ensure that originating times are strictly increasing
                    if (originatingTime <= this.Out.LastEnvelope.OriginatingTime)
                    {
                        originatingTime = this.Out.LastEnvelope.OriginatingTime.AddTicks(1);
                    }

                    this.Out.Post(sharedImage, originatingTime);
                });
            }
            else
            {
                // Requested camera capture format was not found. Construct an exception message with a list of supported formats.
                var exceptionMessageBuilder = new StringBuilder();

                if (string.IsNullOrEmpty(this.configuration.DeviceId))
                {
                    exceptionMessageBuilder.Append($"No cameras were found that support the requested capture format of {this.configuration.Width}x{this.configuration.Height} @ {this.configuration.Framerate} fps. ");
                }
                else
                {
                    exceptionMessageBuilder.Append($"The specified camera {this.configuration.DeviceId} does not support the requested capture format of {this.configuration.Width}x{this.configuration.Height} @ {this.configuration.Framerate} fps. ");
                }

                exceptionMessageBuilder.AppendLine("Use one of the following supported camera capture formats instead:");
                this.AppendSupportedCaptureFormats(exceptionMessageBuilder);

                throw new ArgumentException(exceptionMessageBuilder.ToString());
            }
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.Dispose();
            MediaCaptureDevice.Uninitialize();
            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void SetDeviceProperty(VideoProperty prop, MediaCaptureInfo.PropertyInfo propInfo, MediaCaptureConfiguration.PropertyValue<int> value)
        {
            if (propInfo.Supported)
            {
                VideoPropertyFlags flag = (propInfo.AutoControlled && value.Auto) ? VideoPropertyFlags.Auto : VideoPropertyFlags.Manual;
                this.camera.SetProperty(prop, value.Value, flag);
            }
        }

        private void SetDeviceProperty(ManagedCameraControlProperty prop, MediaCaptureInfo.PropertyInfo propInfo, MediaCaptureConfiguration.PropertyValue<int> value)
        {
            if (propInfo.Supported)
            {
                ManagedCameraControlPropertyFlags flag = (propInfo.AutoControlled && value.Auto) ? ManagedCameraControlPropertyFlags.Auto : ManagedCameraControlPropertyFlags.Manual;
                this.camera.SetProperty(prop, value.Value, flag);
            }
        }

        private void SetDeviceProperty(VideoProperty prop, MediaCaptureInfo.PropertyInfo propInfo, MediaCaptureConfiguration.PropertyValue<bool> value)
        {
            if (propInfo.Supported)
            {
                VideoPropertyFlags flag = (propInfo.AutoControlled && value.Auto) ? VideoPropertyFlags.Auto : VideoPropertyFlags.Manual;
                this.camera.SetProperty(prop, value.Value ? 1 : 0, flag);
            }
        }

        private MediaCaptureConfiguration.PropertyValue<int> GetValueInt(VideoProperty prop, bool supported)
        {
            int flags = 0;
            int value = 0;
            MediaCaptureConfiguration.PropertyValue<int> propValue = new MediaCaptureConfiguration.PropertyValue<int>();
            if (supported &&
                this.camera.GetProperty(prop, ref value, ref flags))
            {
                propValue.Value = value;
                propValue.Auto = flags == (int)VideoPropertyFlags.Auto;
            }

            return propValue;
        }

        private MediaCaptureConfiguration.PropertyValue<int> GetValueInt(ManagedCameraControlProperty prop, bool supported)
        {
            int flags = 0;
            int value = 0;
            MediaCaptureConfiguration.PropertyValue<int> propValue = new MediaCaptureConfiguration.PropertyValue<int>();
            if (supported &&
                this.camera.GetProperty(prop, ref value, ref flags))
            {
                propValue.Value = value;
                propValue.Auto = flags == (int)VideoPropertyFlags.Auto;
            }

            return propValue;
        }

        private MediaCaptureConfiguration.PropertyValue<bool> GetValueBool(VideoProperty prop, bool supported)
        {
            int flags = 0;
            int value = 0;
            MediaCaptureConfiguration.PropertyValue<bool> propValue = new MediaCaptureConfiguration.PropertyValue<bool>();
            if (supported &&
                this.camera.GetProperty(prop, ref value, ref flags))
            {
                propValue.Value = value == 1;
                propValue.Auto = flags == (int)VideoPropertyFlags.Auto;
            }

            return propValue;
        }

        /// <summary>
        /// Appends the list of supported capture formats for all devices. Used to build a more informative
        /// exception message when the requested capture format is not found.
        /// </summary>
        /// <param name="stringBuilder">The <see cref="StringBuilder"/> object to which to append the list.</param>
        /// <remarks>Assumes <see cref="MediaCaptureDevice.Initialize"/> has already been called.</remarks>
        private void AppendSupportedCaptureFormats(StringBuilder stringBuilder)
        {
            foreach (var device in MediaCaptureDevice.AllDevices)
            {
                if (device.Attach(this.configuration.UseInSharedMode))
                {
                    foreach (var format in device.Formats)
                    {
                        stringBuilder.AppendLine($"{device.FriendlyName}: {format.nWidth}x{format.nHeight} @ {format.nFrameRateNumerator / format.nFrameRateDenominator} fps");
                    }
                }
            }
        }
    }
}

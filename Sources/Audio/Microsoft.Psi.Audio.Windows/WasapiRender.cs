// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Microsoft.Psi.Audio.ComInterop;

    /// <summary>
    /// Implements the services required to play audio to audio renderer devices.
    /// </summary>
    internal class WasapiRender : IDisposable
    {
        private static Guid guidEventContext = new Guid(0x65717dc8, 0xe74c, 0x4087, 0x90, 0x1, 0xdb, 0xc5, 0xdd, 0x5c, 0x9e, 0x19);

        private IMMDevice audioDevice;
        private IAudioEndpointVolume volume;
        private AudioEndpointVolumeCallback volumeCallback;
        private WasapiRenderClient wasapiRenderClient;
        private AudioDataRequestedCallback callbackDelegate;
        private CircularBufferStream audioBufferStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiRender"/> class.
        /// </summary>
        public WasapiRender()
        {
        }

        /// <summary>
        /// An event that is raised whenever there is a change to the audio volume.
        /// </summary>
        public event EventHandler<AudioVolumeEventArgs> AudioVolumeNotification;

        /// <summary>
        /// Gets the expected audio format. This property will only be valid after StartRendering has been called
        /// and will return the expected format of the audio being rendered on the selected audio renderer device.
        /// </summary>
        public WaveFormat MixFormat => this.wasapiRenderClient?.MixFormat;

        /// <summary>
        /// Gets or sets the audio output level.
        /// </summary>
        public float AudioLevel
        {
            get
            {
                return this.volume?.GetMasterVolumeLevelScalar() ?? 0;
            }

            set
            {
                this.volume?.SetMasterVolumeLevelScalar(value, ref guidEventContext);
            }
        }

        /// <summary>
        /// Gets the friendly name of the audio renderer device.
        /// </summary>
        public string Name
        {
            get
            {
                return DeviceUtil.GetDeviceFriendlyName(this.audioDevice);
            }
        }

        /// <summary>
        /// Gets a list of available audio render devices.
        /// </summary>
        /// <returns>
        /// An array of available render device names.
        /// </returns>
        public static string[] GetAvailableRenderDevices()
        {
            // Get the collection of available render devices
            IMMDeviceCollection deviceCollection = DeviceUtil.GetAvailableDevices(EDataFlow.Render);

            string[] devices = null;
            int deviceCount = deviceCollection.GetCount();

            devices = new string[deviceCount];

            // Iterate over the collection to get the device names
            for (int i = 0; i < deviceCount; i++)
            {
                IMMDevice device = deviceCollection.Item(i);

                // Get the friendly name of the device
                devices[i] = DeviceUtil.GetDeviceFriendlyName(device);

                // Done with the device so release it
                Marshal.ReleaseComObject(device);
            }

            // Release the collection when done
            Marshal.ReleaseComObject(deviceCollection);

            return devices;
        }

        /// <summary>
        /// Disposes an instance of the <see cref="WasapiRender"/> class.
        /// </summary>
        public void Dispose()
        {
            this.StopRendering();

            if (this.volume != null)
            {
                if (this.volumeCallback != null)
                {
                    // Unregister the callback before releasing.
                    this.volume.UnregisterControlChangeNotify(this.volumeCallback);
                    this.volumeCallback = null;
                }

                Marshal.ReleaseComObject(this.volume);
                this.volume = null;
            }

            if (this.audioDevice != null)
            {
                Marshal.ReleaseComObject(this.audioDevice);
                this.audioDevice = null;
            }
        }

        /// <summary>
        /// Initializes the audio renderer device.
        /// </summary>
        /// <param name="deviceDescription">
        /// The friendly name description of the device to render to. This is usually
        /// something like "Speakers (High Definition Audio)". To just use the
        /// default device, pass in NULL or an empty string.
        /// </param>
        public void Initialize(string deviceDescription)
        {
            // Activate native audio COM objects on a thread-pool thread to ensure that they are in an MTA
            Task.Run(() =>
            {
                if (string.IsNullOrEmpty(deviceDescription))
                {
                    // use the default console device
                    this.audioDevice = DeviceUtil.GetDefaultDevice(EDataFlow.Render, ERole.Console);
                }
                else
                {
                    this.audioDevice = DeviceUtil.GetDeviceByName(EDataFlow.Render, deviceDescription);
                }

                if (this.audioDevice != null)
                {
                    // Try to get the volume control
                    object obj = this.audioDevice.Activate(new Guid(Guids.IAudioEndpointVolumeIIDString), ClsCtx.ALL, IntPtr.Zero);
                    this.volume = (IAudioEndpointVolume)obj;

                    // Now create an IAudioEndpointVolumeCallback object that wraps the callback and register it with the endpoint.
                    this.volumeCallback = new AudioEndpointVolumeCallback(this.AudioVolumeCallback);
                    this.volume.RegisterControlChangeNotify(this.volumeCallback);
                }
            }).Wait();
        }

        /// <summary>
        /// Starts rendering audio data.
        /// </summary>
        /// <param name="maxBufferSeconds">
        /// The maximum duration of audio that can be buffered for playback.
        /// </param>
        /// <param name="targetLatencyInMs">
        /// The target maximum number of milliseconds of acceptable lag between
        /// playback of samples and live sound being produced.
        /// </param>
        /// <param name="gain">
        /// The gain to be applied prior to rendering the audio.
        /// </param>
        /// <param name="inFormat">
        /// The input audio format.
        /// </param>
        public void StartRendering(double maxBufferSeconds, int targetLatencyInMs, float gain, WaveFormat inFormat)
        {
            if (this.wasapiRenderClient != null)
            {
                this.StopRendering();
            }

            // Create an audio buffer to buffer audio awaiting playback.
            this.audioBufferStream = new CircularBufferStream((long)Math.Ceiling(maxBufferSeconds * inFormat.AvgBytesPerSec), false);

            this.wasapiRenderClient = new WasapiRenderClient(this.audioDevice);

            // Create a callback delegate and marshal it to a function pointer. Keep a
            // reference to the delegate as a class field to prevent it from being GC'd.
            this.callbackDelegate = new AudioDataRequestedCallback(this.AudioDataRequestedCallback);

            // initialize the renderer with the desired parameters
            this.wasapiRenderClient.Initialize(targetLatencyInMs, gain, inFormat, this.callbackDelegate);

            // tell WASAPI to start rendering
            this.wasapiRenderClient.Start();
        }

        /// <summary>
        /// Appends audio buffers to the render queue. Audio will be rendered as soon as possible
        /// if <see cref="StartRendering"/> has previously been called.
        /// </summary>
        /// <param name="audioBuffer">The audio buffer to be rendered.</param>
        /// <param name="overwritePending">
        /// If true, then the internal buffer of audio pending rendering may be overwritten. If false,
        /// the call will block until there is sufficient space in the buffer to accommodate the audio
        /// data. Default is false.
        /// </param>
        public void AppendAudio(byte[] audioBuffer, bool overwritePending = false)
        {
            if (this.audioBufferStream == null)
            {
                // component has been stopped
                return;
            }

            if (overwritePending)
            {
                this.audioBufferStream.Write(audioBuffer, 0, audioBuffer.Length);
            }
            else
            {
                int bytesRemaining = audioBuffer.Length;
                while (bytesRemaining > 0)
                {
                    bytesRemaining -= this.audioBufferStream.WriteNoOverrun(audioBuffer, audioBuffer.Length - bytesRemaining, bytesRemaining);
                }
            }
        }

        /// <summary>
        /// Stops rendering audio data.
        /// </summary>
        public void StopRendering()
        {
            if (this.wasapiRenderClient != null)
            {
                this.wasapiRenderClient.Dispose();
                this.wasapiRenderClient = null;
            }

            if (this.audioBufferStream != null)
            {
                this.audioBufferStream.Dispose();
                this.audioBufferStream = null;
            }
        }

        /// <summary>
        /// Callback function that is passed to WASAPI to call whenever it is
        /// ready to receive new audio samples for rendering.
        /// </summary>
        /// <param name="dataPtr">
        /// Pointer to the native buffer that will receive the new audio data.
        /// </param>
        /// <param name="length">
        /// The maximum number of bytes of audio data that may be copied into pbData.
        /// </param>
        /// <param name="timestamp">
        /// The timestamp in 100-ns ticks of the first sample in data.
        /// </param>
        /// <returns>
        /// Returns the actual number of bytes copied into dataPtr.
        /// </returns>
        private int AudioDataRequestedCallback(IntPtr dataPtr, int length, out long timestamp)
        {
            // Timestamp is unnecessary when rendering so just set it to zero
            timestamp = 0;

            unsafe
            {
                // Read buffered audio directly into dataPtr
                return this.audioBufferStream.Read(dataPtr, length, length);
            }
        }

        /// <summary>
        /// Callback function that is passed to the audio endpoint to call whenever
        /// there is a new audio volume notification.
        /// </summary>
        /// <param name="data">
        /// The audio volume notification data.
        /// </param>
        private void AudioVolumeCallback(AudioVolumeNotificationData data)
        {
            // Only raise event notification if we didn't initiate the volume change
            if (data.EventContext != guidEventContext)
            {
                // Raise the event, passing the audio volume notification data in the event args
                this.AudioVolumeNotification?.Invoke(this, new AudioVolumeEventArgs(data.Muted, data.MasterVolume, data.ChannelVolume));
            }
        }
    }
}

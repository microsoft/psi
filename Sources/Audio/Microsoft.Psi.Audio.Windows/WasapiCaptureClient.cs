// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.Psi.Audio.ComInterop;

    /// <summary>
    /// The callback delegate that is invoked whenever new audio data is captured.
    /// </summary>
    /// <param name="data">A pointer to the audio buffer.</param>
    /// <param name="length">The number of bytes of data available in the audio buffer.</param>
    /// <param name="timestamp">The timestamp of the audio buffer.</param>
    internal delegate void AudioDataAvailableCallback(IntPtr data, int length, long timestamp);

    /// <summary>
    /// The WASAPI capture class.
    /// </summary>
    internal class WasapiCaptureClient : IDisposable
    {
        private const int AudioClientBufferEmpty = 0x08890001;
        private static Guid audioClientIID = new Guid(Guids.IAudioClientIIDString);

        // Core Audio Capture member variables.
        private readonly IMMDevice endpoint;
        private readonly bool isEventDriven;
        private IAudioClient audioClient;
        private IAudioCaptureClient captureClient;
        private IMFTransform resampler;
        private AutoResetEvent audioAvailableEvent;

        private AudioDataAvailableCallback dataAvailableCallback;
        private Thread captureThread;
        private ManualResetEvent shutdownEvent;
        private int engineLatencyInMs;
        private int engineBufferInMs;
        private WaveFormat mixFormat;
        private int mixFrameSize;
        private float gain;

        // Capture buffer member variables
        private int inputBufferSize;
        private IMFMediaBuffer inputBuffer;
        private IMFSample inputSample;
        private int outputBufferSize;
        private IMFMediaBuffer outputBuffer;
        private IMFSample outputSample;
        private int bytesCaptured;

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiCaptureClient"/> class.
        /// </summary>
        /// <param name="endpoint">The audio endpoint device.</param>
        /// <param name="isEventDriven">If true, uses WASAPI event-driven audio capture.</param>
        public WasapiCaptureClient(IMMDevice endpoint, bool isEventDriven)
        {
            this.endpoint = endpoint;
            this.isEventDriven = isEventDriven;
        }

        /// <summary>
        /// Gets the mix format of the captured audio.
        /// </summary>
        public WaveFormat MixFormat => this.mixFormat;

        /// <summary>
        /// Disposes the <see cref="WasapiCaptureClient"/> object.
        /// </summary>
        public void Dispose()
        {
            if (this.captureThread != null)
            {
                this.shutdownEvent.Set();
                this.captureThread.Join();
                this.captureThread = null;
            }

            if (this.shutdownEvent != null)
            {
                this.shutdownEvent.Close();
                this.shutdownEvent = null;
            }

            if (this.audioAvailableEvent != null)
            {
                this.audioAvailableEvent.Close();
                this.audioAvailableEvent = null;
            }

            if (this.audioClient != null)
            {
                Marshal.ReleaseComObject(this.audioClient);
                this.audioClient = null;
            }

            if (this.captureClient != null)
            {
                Marshal.ReleaseComObject(this.captureClient);
                this.captureClient = null;
            }

            if (this.resampler != null)
            {
                Marshal.ReleaseComObject(this.resampler);
                this.resampler = null;
            }

            if (this.inputBuffer != null)
            {
                Marshal.ReleaseComObject(this.inputBuffer);
                this.inputBuffer = null;
            }

            if (this.inputSample != null)
            {
                Marshal.ReleaseComObject(this.inputSample);
                this.inputSample = null;
            }

            if (this.outputBuffer != null)
            {
                Marshal.ReleaseComObject(this.outputBuffer);
                this.outputBuffer = null;
            }

            if (this.outputSample != null)
            {
                Marshal.ReleaseComObject(this.outputSample);
                this.outputSample = null;
            }
        }

        /// <summary>
        /// Initialize the capturer.
        /// </summary>
        /// <param name="engineLatency">
        /// Number of milliseconds of acceptable lag between live sound being produced and recording operation.
        /// </param>
        /// <param name="engineBuffer">
        /// Number of milliseconds of audio that may be buffered between reads.
        /// </param>
        /// <param name="gain">
        /// The gain to be applied to the audio after capture.
        /// </param>
        /// <param name="outFormat">
        /// The format of the audio to be captured. If this is NULL, the default audio format of the
        /// capture device will be used.
        /// </param>
        /// <param name="callback">
        /// Callback function delegate which will handle the captured data.
        /// </param>
        /// <param name="speech">
        /// If true, sets the audio category to speech to optimize audio pipeline for speech recognition.
        /// </param>
        public void Initialize(int engineLatency, int engineBuffer, float gain, WaveFormat outFormat, AudioDataAvailableCallback callback, bool speech)
        {
            // Create our shutdown event - we want a manual reset event that starts in the not-signaled state.
            this.shutdownEvent = new ManualResetEvent(false);

            // Now activate an IAudioClient object on our preferred endpoint and retrieve the mix format for that endpoint.
            object obj = this.endpoint.Activate(ref audioClientIID, ClsCtx.INPROC_SERVER, IntPtr.Zero);
            this.audioClient = (IAudioClient)obj;

            // The following block enables advanced mic array APO pipeline on Windows 10 RS2 builds >= 15004.
            // This must be called before the call to GetMixFormat() in LoadFormat().
            if (speech)
            {
                IAudioClient2 audioClient2 = (IAudioClient2)this.audioClient;
                if (audioClient2 != null)
                {
                    AudioClientProperties properties = new AudioClientProperties
                    {
                        Size = Marshal.SizeOf<AudioClientProperties>(),
                        Category = AudioStreamCategory.Speech,
                    };

                    int hr = audioClient2.SetClientProperties(ref properties);
                    if (hr != 0)
                    {
                        Console.WriteLine("Failed to set audio stream category to AudioCategory_Speech: {0}", hr);
                    }
                }
                else
                {
                    Console.WriteLine("Unable to get IAudioClient2 interface");
                }
            }

            // Load the MixFormat. This may differ depending on the shared mode used.
            this.LoadFormat();

            // Remember our configured latency and buffer size
            this.engineLatencyInMs = engineLatency;
            this.engineBufferInMs = engineBuffer;

            // Set the gain
            this.gain = gain;

            // Determine whether or not we need a resampler
            this.resampler = null;

            if (outFormat != null)
            {
                // Check if the desired format is supported
                IntPtr closestMatchPtr;
                IntPtr outFormatPtr = WaveFormat.MarshalToPtr(outFormat);
                int hr = this.audioClient.IsFormatSupported(AudioClientShareMode.Shared, outFormatPtr, out closestMatchPtr);

                // Free outFormatPtr to prevent leaking memory
                Marshal.FreeHGlobal(outFormatPtr);

                if (hr == 0)
                {
                    // Replace _MixFormat with outFormat. Since it is supported, we will initialize
                    // the audio capture client with that format and capture without resampling.
                    this.mixFormat = outFormat;
                    this.mixFrameSize = (this.mixFormat.BitsPerSample / 8) * this.mixFormat.Channels;

                    this.InitializeAudioEngine();
                }
                else
                {
                    // In all other cases, we need to resample to OutFormat
                    if ((hr == 1) && (closestMatchPtr != IntPtr.Zero))
                    {
                        // Use closest match suggested by IsFormatSupported() and resample
                        this.mixFormat = WaveFormat.MarshalFromPtr(closestMatchPtr);
                        this.mixFrameSize = (this.mixFormat.BitsPerSample / 8) * this.mixFormat.Channels;

                        // Free closestMatchPtr to prevent leaking memory
                        Marshal.FreeCoTaskMem(closestMatchPtr);
                    }

                    // initialize the audio engine first as the engine latency may be modified after initialization
                    this.InitializeAudioEngine();

                    // initialize the resampler buffers
                    this.inputBufferSize = (int)(this.engineBufferInMs * this.mixFormat.AvgBytesPerSec / 1000);
                    this.outputBufferSize = (int)(this.engineBufferInMs * outFormat.AvgBytesPerSec / 1000);

                    DeviceUtil.CreateResamplerBuffer(this.inputBufferSize, out this.inputSample, out this.inputBuffer);
                    DeviceUtil.CreateResamplerBuffer(this.outputBufferSize, out this.outputSample, out this.outputBuffer);

                    // Create resampler object
                    this.resampler = DeviceUtil.CreateResampler(this.mixFormat, outFormat);
                }
            }
            else
            {
                // initialize the audio engine with the default mix format
                this.InitializeAudioEngine();
            }

            // Set the callback function
            this.dataAvailableCallback = callback;
        }

        /// <summary>
        ///  Start capturing audio data.
        /// </summary>
        public void Start()
        {
            this.bytesCaptured = 0;

            // Now create the thread which is going to drive the capture.
            this.captureThread = new Thread(this.DoCaptureThread);

            // We're ready to go, start capturing!
            this.captureThread.Start();
            this.audioClient.Start();
        }

        /// <summary>
        /// Stop the capturer.
        /// </summary>
        public void Stop()
        {
            // Tell the capture thread to shut down, wait for the thread to complete then clean up all the stuff we
            // allocated in Start().
            if (this.shutdownEvent != null)
            {
                this.shutdownEvent.Set();
            }

            this.audioClient.Stop();

            if (this.captureThread != null)
            {
                this.captureThread.Join();
                this.captureThread = null;
            }
        }

        /// <summary>
        /// Capture thread - captures audio from WASAPI, resampling it if necessary.
        /// </summary>
        private void DoCaptureThread()
        {
            bool stillPlaying = true;
            int mmcssHandle = 0;
            int mmcssTaskIndex = 0;

            mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics("Audio", ref mmcssTaskIndex);

            WaitHandle[] waitArray = this.isEventDriven ?
                new WaitHandle[] { this.shutdownEvent, this.audioAvailableEvent } :
                new WaitHandle[] { this.shutdownEvent };

            int waitTimeout = this.isEventDriven ? Timeout.Infinite : this.engineLatencyInMs;

            while (stillPlaying)
            {
                // We want to wait for half the desired latency in milliseconds.
                // That way we'll wake up half way through the processing period to pull the
                // next set of samples from the engine.
                int waitResult = WaitHandle.WaitAny(waitArray, waitTimeout);

                switch (waitResult)
                {
                    case 0:
                        // If shutdownEvent has been set, we're done and should exit the main capture loop.
                        stillPlaying = false;
                        break;

                    default:
                        {
                            // We need to retrieve the next buffer of samples from the audio capturer.
                            IntPtr dataPointer;
                            int framesAvailable;
                            int flags;
                            long bufferPosition;
                            long qpcPosition;
                            bool isEmpty = false;
                            long lastQpcPosition = 0;

                            // Keep fetching audio in a tight loop as long as audio device still has data.
                            while (!isEmpty && !this.shutdownEvent.WaitOne(0))
                            {
                                int hr = this.captureClient.GetBuffer(out dataPointer, out framesAvailable, out flags, out bufferPosition, out qpcPosition);
                                if (hr >= 0)
                                {
                                    if ((hr == AudioClientBufferEmpty) || (framesAvailable == 0))
                                    {
                                        isEmpty = true;
                                    }
                                    else
                                    {
                                        int bytesAvailable = framesAvailable * this.mixFrameSize;

                                        unsafe
                                        {
                                            // The flags on capture tell us information about the data.
                                            // We only really care about the silent flag since we want to put frames of silence into the buffer
                                            // when we receive silence.  We rely on the fact that a logical bit 0 is silence for both float and int formats.
                                            if ((flags & (int)AudioClientBufferFlags.Silent) != 0)
                                            {
                                                // Fill 0s from the capture buffer to the output buffer.
                                                float* ptr = (float*)dataPointer.ToPointer();
                                                for (int i = 0; i < bytesAvailable / sizeof(float); i++)
                                                {
                                                    *(ptr + i) = 0f;
                                                }
                                            }
                                            else if (this.gain != 1.0f)
                                            {
                                                // Apply gain on the raw buffer if needed, before the resampler.
                                                // When we capture in shared mode the capture mix format is always 32-bit IEEE
                                                // floating point, so we can safely assume float samples in the buffer.
                                                float* ptr = (float*)dataPointer.ToPointer();
                                                for (int i = 0; i < bytesAvailable / sizeof(float); i++)
                                                {
                                                    *(ptr + i) *= this.gain;
                                                }
                                            }
                                        }

                                        // Check if we need to resample
                                        if (this.resampler != null)
                                        {
                                            // Process input to resampler
                                            this.ProcessResamplerInput(dataPointer, bytesAvailable, flags, qpcPosition);

                                            // Process output from resampler
                                            int bytesWritten = this.ProcessResamplerOutput();

                                            // Audio capture was successful, so bump the capture buffer pointer.
                                            this.bytesCaptured += bytesWritten;
                                        }
                                        else
                                        {
                                            // Invoke the callback directly to handle the captured samples
                                            if (this.dataAvailableCallback != null)
                                            {
                                                if (qpcPosition > lastQpcPosition)
                                                {
                                                    this.dataAvailableCallback(dataPointer, bytesAvailable, qpcPosition);
                                                    lastQpcPosition = qpcPosition;

                                                    this.bytesCaptured += bytesAvailable;
                                                }
                                                else
                                                {
                                                    Console.WriteLine("QPC is less than last {0}", qpcPosition - lastQpcPosition);
                                                }
                                            }
                                        }
                                    }

                                    this.captureClient.ReleaseBuffer(framesAvailable);
                                }
                            }
                        }

                        break;
                }
            }

            if (mmcssHandle != 0)
            {
                NativeMethods.AvRevertMmThreadCharacteristics(mmcssHandle);
            }
        }

        /// <summary>
        /// Take audio data captured from WASAPI and feed it as input to audio resampler.
        /// </summary>
        /// <param name="bufferPtr">
        /// [in] Buffer holding audio data from WASAPI.
        /// </param>
        /// <param name="bufferSize">
        /// [in] Number of bytes available in pBuffer.
        /// </param>
        /// <param name="flags">
        /// [in] Flags returned from WASAPI capture.
        /// </param>
        /// <param name="qpcPosition">
        /// [in] The value of the performance counter in 100-nanosecond ticks at the time
        /// the first audio frame in pBuffer was recorded.
        /// </param>
        private void ProcessResamplerInput(IntPtr bufferPtr, int bufferSize, int flags, long qpcPosition)
        {
            IntPtr ptrLocked;
            int maxLength;

            this.inputBuffer.Lock(out ptrLocked, out maxLength, out _);
            int dataToCopy = Math.Min(bufferSize, maxLength);

            // Copy data from the audio engine buffer to the output buffer.
            unsafe
            {
                Buffer.MemoryCopy(bufferPtr.ToPointer(), ptrLocked.ToPointer(), maxLength, dataToCopy);
            }

            // Set the sample timestamp and duration (use LL suffix to prevent INT32 overflow!)
            this.inputSample.SetSampleTime(qpcPosition);
            this.inputSample.SetSampleDuration(10000000L * dataToCopy / this.mixFormat.AvgBytesPerSec);

            this.inputBuffer.SetCurrentLength(dataToCopy);
            this.resampler.ProcessInput(0, this.inputSample, 0);

            this.inputBuffer.Unlock();
        }

        /// <summary>
        /// Get data output from audio resampler and raises a callback.
        /// </summary>
        /// <returns>Number of bytes captured.</returns>
        private int ProcessResamplerOutput()
        {
            MFTOutputDataBuffer outBuffer;
            int outStatus;
            int lockedLength = 0;

            outBuffer.StreamID = 0;
            outBuffer.Sample = this.outputSample;
            outBuffer.Status = 0;
            outBuffer.Events = null;

            var hr = this.resampler.ProcessOutput(0, 1, ref outBuffer, out outStatus);
            if (hr == 0)
            {
                IntPtr ptrLocked;
                this.outputBuffer.Lock(out ptrLocked, out _, out _);

                lockedLength = this.outputBuffer.GetCurrentLength();

                long sampleTime;
                hr = this.outputSample.GetSampleTime(out sampleTime);
                if (hr < 0)
                {
                    // Use zero to indicate that timestamp was not available
                    sampleTime = 0;
                }

                // Raise the callback to handle the captured samples
                this.dataAvailableCallback?.Invoke(ptrLocked, lockedLength, sampleTime);

                this.outputBuffer.Unlock();
            }

            return lockedLength;
        }

        /// <summary>
        /// Initialize WASAPI in timer driven mode, and retrieve a capture client for the transport.
        /// </summary>
        private void InitializeAudioEngine()
        {
            var streamFlags = AudioClientStreamFlags.NoPersist;

            if (this.isEventDriven)
            {
                streamFlags |= AudioClientStreamFlags.EventCallback;
                this.audioAvailableEvent = new AutoResetEvent(false);
            }
            else
            {
                // ensure buffer is at least twice the latency (only in pull mode)
                if (this.engineBufferInMs < 2 * this.engineLatencyInMs)
                {
                    this.engineBufferInMs = 2 * this.engineLatencyInMs;
                }
            }

            IntPtr mixFormatPtr = WaveFormat.MarshalToPtr(this.mixFormat);
            this.audioClient.Initialize(AudioClientShareMode.Shared, streamFlags, this.engineBufferInMs * 10000, 0, mixFormatPtr, Guid.Empty);
            Marshal.FreeHGlobal(mixFormatPtr);

            if (this.isEventDriven)
            {
                this.audioClient.SetEventHandle(this.audioAvailableEvent.SafeWaitHandle.DangerousGetHandle());
            }

            // get the actual audio engine buffer size
            int bufferFrames = this.audioClient.GetBufferSize();
            this.engineBufferInMs = (int)(bufferFrames * 1000L / this.mixFormat.SamplesPerSec);

            object obj = this.audioClient.GetService(new Guid(Guids.IAudioCaptureClientIIDString));
            this.captureClient = (IAudioCaptureClient)obj;
        }

        /// <summary>
        /// Retrieve the format we'll use to capture samples.
        /// We use the Mix format since we're capturing in shared mode.
        /// </summary>
        private void LoadFormat()
        {
            IntPtr mixFormatPtr = this.audioClient.GetMixFormat();
            this.mixFormat = WaveFormat.MarshalFromPtr(mixFormatPtr);
            Marshal.FreeCoTaskMem(mixFormatPtr);
            this.mixFrameSize = (this.mixFormat.BitsPerSample / 8) * this.mixFormat.Channels;
        }
    }
}
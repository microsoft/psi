// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Microsoft.Psi.Audio.ComInterop;

    /// <summary>
    /// The callback delegate that is invoked whenever more audio data is requested by the renderer.
    /// </summary>
    /// <param name="data">A pointer to the audio buffer.</param>
    /// <param name="length">The length of the audio buffer.</param>
    /// <param name="timestamp">The timestamp of the audio.</param>
    /// <returns>The number of bytes that were copied into the audio buffer.</returns>
    internal delegate int AudioDataRequestedCallback(IntPtr data, int length, out long timestamp);

    /// <summary>
    /// The WASAPI renderer class.
    /// </summary>
    internal class WasapiRenderClient : IDisposable
    {
        private static Guid audioClientIID = new Guid(Guids.IAudioClientIIDString);

        // Core Audio Renderer member variables.
        private IMMDevice endpoint;
        private IAudioClient audioClient;
        private IAudioRenderClient renderClient;
        private IMFTransform resampler;

        private AudioDataRequestedCallback dataRequestedCallback;
        private Thread renderThread;
        private ManualResetEvent shutdownEvent;
        private int engineLatencyInMs;
        private WaveFormat mixFormat;
        private int mixFrameSize;
        private float gain;

        // Render buffer member variables
        private int bufferFrameCount;
        private int inputBufferSize;
        private IMFMediaBuffer inputBuffer;
        private IMFSample inputSample;
        private int outputBufferSize;
        private IMFMediaBuffer outputBuffer;
        private IMFSample outputSample;
        private int bytesRendered;

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiRenderClient"/> class.
        /// </summary>
        /// <param name="endpoint">The audio endpoint device.</param>
        public WasapiRenderClient(IMMDevice endpoint)
        {
            this.endpoint = endpoint;
        }

        /// <summary>
        /// Gets the mix format of the audio renderer.
        /// </summary>
        public WaveFormat MixFormat => this.mixFormat;

        /// <summary>
        /// Gets number of bytes of audio data rendered so far.
        /// </summary>
        public int BytesRendered => this.bytesRendered;

        /// <summary>
        /// Disposes the <see cref="WasapiRenderClient"/> object.
        /// </summary>
        public void Dispose()
        {
            if (this.renderThread != null)
            {
                this.shutdownEvent.Set();
                this.renderThread.Join();
                this.renderThread = null;
            }

            if (this.shutdownEvent != null)
            {
                this.shutdownEvent.Close();
                this.shutdownEvent = null;
            }

            if (this.audioClient != null)
            {
                Marshal.ReleaseComObject(this.audioClient);
                this.audioClient = null;
            }

            if (this.renderClient != null)
            {
                Marshal.ReleaseComObject(this.renderClient);
                this.renderClient = null;
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
        /// Initialize the renderer.
        /// </summary>
        /// <param name="engineLatency">
        /// Number of milliseconds of acceptable lag between playback of samples and live sound being produced.
        /// </param>
        /// <param name="gain">
        /// The gain to be applied to the audio before rendering.
        /// </param>
        /// <param name="inFormat">
        /// The format of the input audio samples to be rendered. If this is NULL, the current default audio
        /// format of the renderer device will be assumed.
        /// </param>
        /// <param name="callback">
        /// Callback function delegate which will supply the data to be rendered.
        /// </param>
        public void Initialize(int engineLatency, float gain, WaveFormat inFormat, AudioDataRequestedCallback callback)
        {
            // Create our shutdown event - we want a manual reset event that starts in the not-signaled state.
            this.shutdownEvent = new ManualResetEvent(false);

            // Now activate an IAudioClient object on our preferred endpoint and retrieve the mix format for that endpoint.
            object obj = this.endpoint.Activate(ref audioClientIID, ClsCtx.INPROC_SERVER, IntPtr.Zero);
            this.audioClient = (IAudioClient)obj;

            // Load the MixFormat. This may differ depending on the shared mode used.
            this.LoadFormat();

            // Remember our configured latency
            this.engineLatencyInMs = engineLatency;

            // Set the gain
            this.gain = gain;

            // Check if the desired format is supported
            IntPtr closestMatchPtr;
            IntPtr inFormatPtr = WaveFormat.MarshalToPtr(inFormat);
            int hr = this.audioClient.IsFormatSupported(AudioClientShareMode.Shared, inFormatPtr, out closestMatchPtr);

            // Free outFormatPtr to prevent leaking memory
            Marshal.FreeHGlobal(inFormatPtr);

            if (hr == 0)
            {
                // Replace _MixFormat with inFormat. Since it is supported, we will initialize
                // the audio render client with that format and render without resampling.
                this.mixFormat = inFormat;
                this.mixFrameSize = (this.mixFormat.BitsPerSample / 8) * this.mixFormat.Channels;
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
            }

            this.inputBufferSize = (int)(this.engineLatencyInMs * inFormat.AvgBytesPerSec / 1000);
            this.outputBufferSize = (int)(this.engineLatencyInMs * this.mixFormat.AvgBytesPerSec / 1000);

            DeviceUtil.CreateResamplerBuffer(this.inputBufferSize, out this.inputSample, out this.inputBuffer);
            DeviceUtil.CreateResamplerBuffer(this.outputBufferSize, out this.outputSample, out this.outputBuffer);

            // Create resampler object
            this.resampler = DeviceUtil.CreateResampler(inFormat, this.mixFormat);

            this.InitializeAudioEngine();

            // Set the callback function
            this.dataRequestedCallback = callback;
        }

        /// <summary>
        ///  Start rendering audio data.
        /// </summary>
        public void Start()
        {
            this.bytesRendered = 0;

            // Now create the thread which is going to drive the rendering.
            this.renderThread = new Thread(this.DoRenderThread);

            // We're ready to go, start rendering!
            this.renderThread.Start();

            this.audioClient.Start();
        }

        /// <summary>
        /// Stop the renderer.
        /// </summary>
        public void Stop()
        {
            // Tell the render thread to shut down, wait for the thread to complete then clean up all the stuff we
            // allocated in Start().
            if (this.shutdownEvent != null)
            {
                this.shutdownEvent.Set();
            }

            this.audioClient.Stop();

            if (this.renderThread != null)
            {
                this.renderThread.Join();
                this.renderThread = null;
            }
        }

        /// <summary>
        /// Render thread - reads audio, processes it with a resampler and renders it to WASAPI.
        /// </summary>
        private void DoRenderThread()
        {
            bool stillPlaying = true;
            int mmcssHandle = 0;
            int mmcssTaskIndex = 0;

            mmcssHandle = NativeMethods.AvSetMmThreadCharacteristics("Audio", ref mmcssTaskIndex);

            while (stillPlaying)
            {
                // We want to wait for half the desired latency in milliseconds.
                // That way we'll wake up half way through the processing period to send the
                // next set of samples to the engine.
                bool waitResult = this.shutdownEvent.WaitOne(this.engineLatencyInMs / 2);

                if (waitResult)
                {
                    // If shutdownEvent has been set, we're done and should exit the main render loop.
                    stillPlaying = false;
                }
                else
                {
                    // We need to send the next buffer of samples to the audio renderer.
                    bool isEmpty = false;

                    // Keep fetching audio in a tight loop as long as there is audio available.
                    while (!isEmpty && !this.shutdownEvent.WaitOne(0))
                    {
                        // Process input to resampler
                        int bytesAvailable = this.ProcessResamplerInput();
                        if (bytesAvailable > 0)
                        {
                            // Process output from resampler
                            this.bytesRendered += this.ProcessResamplerOutput();
                        }
                        else
                        {
                            isEmpty = true;
                            stillPlaying = !(bytesAvailable < 0);
                        }
                    }
                }
            }

            if (mmcssHandle != 0)
            {
                NativeMethods.AvRevertMmThreadCharacteristics(mmcssHandle);
            }
        }

        /// <summary>
        /// Read audio data and feed it as input to audio resampler.
        /// </summary>
        /// <returns>The number of bytes read.</returns>
        private int ProcessResamplerInput()
        {
            IntPtr ptrLocked;
            int maxLength;

            this.inputBuffer.Lock(out ptrLocked, out maxLength, out _);
            int bytesRead = 0;
            long sampleTimestamp = 0;

            // Invoke the callback to fill the input buffer with more samples.
            if (this.dataRequestedCallback != null)
            {
                bytesRead = this.dataRequestedCallback(ptrLocked, maxLength, out sampleTimestamp);
            }

            if (bytesRead > 0)
            {
                // Process and resample the audio data
                this.inputBuffer.SetCurrentLength(bytesRead);
                this.resampler.ProcessInput(0, this.inputSample, 0);
            }

            this.inputBuffer.Unlock();

            return bytesRead;
        }

        /// <summary>
        /// Get data output from audio resampler and render it to WASAPI.
        /// </summary>
        /// <returns>The number of bytes rendered.</returns>
        private int ProcessResamplerOutput()
        {
            MFTOutputDataBuffer outBuffer;
            int outStatus;
            int totalBytesWritten = 0;

            outBuffer.StreamID = 0;
            outBuffer.Sample = this.outputSample;
            outBuffer.Status = 0;
            outBuffer.Events = null;

            // Call resampler to generate resampled output audio data.
            var hr = this.resampler.ProcessOutput(0, 1, ref outBuffer, out outStatus);
            if (hr == 0)
            {
                // Grab (lock) the resampler output buffer.
                IntPtr ptrLocked;
                this.outputBuffer.Lock(out ptrLocked, out _, out _);

                // How many bytes of audio data do we have?
                int lockedLength = this.outputBuffer.GetCurrentLength();

                // Convert this to frames since the render client deals in frames, not bytes.
                int framesAvailable = lockedLength / this.mixFrameSize;
                int framesRemaining = framesAvailable;

                // For as long as we have frames to write and we have not been told to shutdown.
                while ((framesRemaining > 0) && !this.shutdownEvent.WaitOne(0))
                {
                    // How many frames in the render buffer are still waiting to be processed?
                    int numFramesPadding = this.audioClient.GetCurrentPadding();

                    // Render the smaller of all remaining output frames and the actual space in the render buffer.
                    int numRenderFrames = Math.Min(this.bufferFrameCount - numFramesPadding, framesRemaining);

                    // numRenderFrames can be zero if the render buffer is still full, so we need
                    // this check to avoid unnecessary calls to IAudioRenderClient::GetBuffer, etc.
                    if (numRenderFrames > 0)
                    {
                        IntPtr dataPointer = this.renderClient.GetBuffer(numRenderFrames);
                        int numRenderBytes = numRenderFrames * this.mixFrameSize;

                        // Copy data from the resampler output buffer to the audio engine buffer.
                        unsafe
                        {
                            // Apply gain on the raw buffer if needed, before rendering.
                            if (this.gain != 1.0f)
                            {
                                // Assumes float samples in the buffer!
                                float* src = (float*)ptrLocked.ToPointer();
                                float* dest = (float*)dataPointer.ToPointer();
                                for (int i = 0; i < numRenderBytes / sizeof(float); i++)
                                {
                                    *(dest + i) = *(src + i) * this.gain;
                                }
                            }
                            else
                            {
                                Buffer.MemoryCopy(ptrLocked.ToPointer(), dataPointer.ToPointer(), numRenderBytes, numRenderBytes);
                            }
                        }

                        this.renderClient.ReleaseBuffer(numRenderFrames, 0);

                        // Increment pLocked and decrement frames remaining
                        ptrLocked += numRenderBytes;
                        totalBytesWritten += numRenderBytes;
                        framesRemaining -= numRenderFrames;
                    }
                    else
                    {
                        // Render buffer is full, so wait for half the latency to give it a chance to free up some space
                        this.shutdownEvent.WaitOne(this.engineLatencyInMs / 2);
                    }
                }

                this.outputBuffer.Unlock();
            }

            return totalBytesWritten;
        }

        /// <summary>
        /// Initialize WASAPI in timer driven mode, and retrieve a render client for the transport.
        /// </summary>
        private void InitializeAudioEngine()
        {
            IntPtr mixFormatPtr = WaveFormat.MarshalToPtr(this.mixFormat);
            this.audioClient.Initialize(AudioClientShareMode.Shared, AudioClientStreamFlags.NoPersist, this.engineLatencyInMs * 10000, 0, mixFormatPtr, Guid.Empty);
            Marshal.FreeHGlobal(mixFormatPtr);

            this.bufferFrameCount = this.audioClient.GetBufferSize();

            object obj = this.audioClient.GetService(new Guid(Guids.IAudioRenderClientIIDString));
            this.renderClient = (IAudioRenderClient)obj;
        }

        /// <summary>
        /// Retrieve the format we'll use to render samples.
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
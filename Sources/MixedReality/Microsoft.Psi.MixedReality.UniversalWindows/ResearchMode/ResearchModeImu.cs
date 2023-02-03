// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.ResearchMode
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using HoloLens2ResearchMode;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Implements an abstract base class for a HoloLens 2 research mode IMU component.
    /// </summary>
    public abstract class ResearchModeImu : IProducer<(Vector3D Sample, DateTime OriginatingTime)[]>, ISourceComponent
    {
        private readonly string name;
        private readonly ResearchModeSensorDevice sensorDevice;
        private readonly ResearchModeImuSensor imuSensor;
        private readonly Task<ResearchModeSensorConsent> requestImuAccessTask;

        private Thread captureThread;
        private bool shutdown;
        private DateTime lastSampleOriginatingTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResearchModeImu"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="sensorType">The research mode sensor type.</param>
        /// <param name="name">An optional name for the component.</param>
        public ResearchModeImu(Pipeline pipeline, ResearchModeSensorType sensorType, string name = nameof(ResearchModeImu))
        {
            this.name = name;
            this.Pipeline = pipeline;
            this.sensorDevice = new ResearchModeSensorDevice();
            this.requestImuAccessTask = this.sensorDevice.RequestIMUAccessAsync().AsTask();
            this.imuSensor = (ResearchModeImuSensor)this.sensorDevice.GetSensor(sensorType);
            this.Out = pipeline.CreateEmitter<(Vector3D Sample, DateTime OriginatingTime)[]>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the IMU stream.
        /// </summary>
        public Emitter<(Vector3D Sample, DateTime OriginatingTime)[]> Out { get; }

        /// <summary>
        /// Gets the pipeline to which this component belongs.
        /// </summary>
        protected Pipeline Pipeline { get; }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            var consent = this.requestImuAccessTask.Result;
            this.CheckConsentAndThrow(consent);

            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);

            this.captureThread = new Thread(this.CaptureThread);
            this.captureThread.Start();
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.shutdown = true;
            this.captureThread.Join(5000);

            notifyCompleted();
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        /// <summary>
        /// Processes a sensor frame received from the sensor.
        /// </summary>
        /// <param name="sensorFrame">The sensor frame.</param>
        protected abstract void ProcessSensorFrame(IResearchModeSensorFrame sensorFrame);

        /// <summary>
        /// Post sensor frame samples.
        /// </summary>
        /// <typeparam name="T">Type of sensor sample.</typeparam>
        /// <param name="sensorFrame">Sensor frame containing samples.</param>
        /// <param name="samples">Sensor samples.</param>
        /// <param name="toValueFn">Function mapping sample to float tuple value.</param>
        /// <param name="toNanos">Function getting sample nanoseconds.</param>
        protected void PostSamples<T>(IResearchModeSensorFrame sensorFrame, T[] samples, Func<T, (float X, float Y, float Z)> toValueFn, Func<T, ulong> toNanos)
        {
            if (samples.Length == 0)
            {
                return;
            }

            // Frames of samples arrive with a time stamp in 100ns "host ticks", which we convert to pipeline time.
            // Individual samples within the frames have "VinylHupTicks", extracted by the toNanos function, which
            // are in nanoseconds and are rooted at an arbitrary epoch (HololLens boot). To compute the originating
            // time of individual samples, we find the difference between a particular sample and the last sample
            // in the frame and apply this as a relative (negative) offset to the frame originating time, which also
            // corresponds with the last sample but is rooted in a proper epoch.
            var frameOriginatingTime = this.Pipeline.GetCurrentTimeFromElapsedTicks((long)sensorFrame.GetTimeStamp().HostTicks);
            var lastRecentSampleNanos = toNanos(samples.Last()); // nanoseconds of last sample (arbitrary epoch)
            var frameSamples = samples
                .Select(sample =>
                {
                    var sensorTicksOffsetFromFrame = (long)(toNanos(sample) - lastRecentSampleNanos) / 100; // negative offset in tick from last sample
                    var sampleOriginatingTime = frameOriginatingTime.AddTicks(sensorTicksOffsetFromFrame); // sample originating time relative to frame
                    var val = toValueFn(sample);
                    return (new Vector3D(-val.Z, -val.X, val.Y) /* \psi basis */, sampleOriginatingTime);
                })
                .Where(sample => sample.sampleOriginatingTime > this.lastSampleOriginatingTime)
                .ToArray();

            if (frameSamples.Length > 0)
            {
                this.lastSampleOriginatingTime = frameSamples.Last().sampleOriginatingTime;
                this.Out.Post(frameSamples, frameOriginatingTime);
            }
        }

        private void CaptureThread()
        {
            // ResearchMode requires that OpenStream() and GetNextBuffer() are called from the same thread
            this.imuSensor.OpenStream();

            try
            {
                while (!this.shutdown)
                {
                    var sensorFrame = this.imuSensor.GetNextBuffer();
                    if (!this.shutdown)
                    {
                        this.ProcessSensorFrame(sensorFrame);
                    }
                }
            }
            finally
            {
                this.imuSensor.CloseStream();
            }
        }

        private void CheckConsentAndThrow(ResearchModeSensorConsent consent)
        {
            switch (consent)
            {
                case ResearchModeSensorConsent.Allowed:
                    return;
                case ResearchModeSensorConsent.DeniedBySystem:
                    throw new UnauthorizedAccessException("Access to the IMU was denied by the system");
                case ResearchModeSensorConsent.DeniedByUser:
                    throw new UnauthorizedAccessException("Access to the IMU was denied by the user");
                case ResearchModeSensorConsent.NotDeclaredByApp:
                    throw new UnauthorizedAccessException("IMU capability was not declared in the app manifest (DeviceCapability backgroundSpatialPerception)");
                case ResearchModeSensorConsent.UserPromptRequired:
                    throw new UnauthorizedAccessException("Permission to access to the IMU must be requested first");
            }
        }
    }
}

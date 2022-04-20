// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.AzureKinect
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.Kinect.BodyTracking;
    using Microsoft.Azure.Kinect.Sensor;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using DepthImage = Microsoft.Psi.Imaging.DepthImage;
    using Image = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Component that performs body tracking from the depth/IR images captured by the Azure Kinect sensor.
    /// </summary>
    /// <remarks>It is important that Depth and IR images do *not* go through lossy encoding/decoding (e.g., JPEG)
    /// before arriving at the tracker. Unencoded or non-lossy (e.g. PNG) encoding are okay.</remarks>
    public sealed class AzureKinectBodyTracker : ConsumerProducer<(Shared<DepthImage> Depth, Shared<Image> IR), List<AzureKinectBody>>, IDisposable
    {
        private static readonly object TrackerCreationLock = new ();

        private readonly AzureKinectBodyTrackerConfiguration configuration;
        private readonly List<AzureKinectBody> currentBodies = new ();
        private readonly Capture capture = new ();

        private Tracker tracker = null;
        private byte[] depthBytes = null;
        private byte[] infraredBytes = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureKinectBodyTracker"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">An optional configuration to use for the body tracker.</param>
        /// <param name="name">An optional name for the component.</param>
        public AzureKinectBodyTracker(Pipeline pipeline, AzureKinectBodyTrackerConfiguration configuration = null, string name = nameof(AzureKinectBodyTracker))
            : base(pipeline, name)
        {
            this.configuration = configuration ?? new AzureKinectBodyTrackerConfiguration();
            this.AzureKinectSensorCalibration = pipeline.CreateReceiver<Calibration>(this, this.ReceiveCalibration, nameof(this.AzureKinectSensorCalibration));
        }

        /// <summary>
        /// Gets the receiver for sensor calibration needed to initialize the tracker.
        /// </summary>
        public Receiver<Calibration> AzureKinectSensorCalibration { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.tracker != null)
            {
                this.tracker.Dispose();
                this.tracker = null;
            }

            this.capture?.Dispose();
        }

        /// <inheritdoc />
        protected override void Receive((Shared<DepthImage> Depth, Shared<Image> IR) depthAndIRImages, Envelope envelope)
        {
            if (this.tracker != null)
            {
                // Allocate depth and IR image buffers.
                if (this.capture.Depth == null || this.capture.IR == null || this.depthBytes == null || this.infraredBytes == null)
                {
                    this.capture.Depth = new Azure.Kinect.Sensor.Image(ImageFormat.Depth16, depthAndIRImages.Depth.Resource.Width, depthAndIRImages.Depth.Resource.Height);
                    this.capture.IR = new Azure.Kinect.Sensor.Image(ImageFormat.IR16, depthAndIRImages.IR.Resource.Width, depthAndIRImages.IR.Resource.Height);
                    this.depthBytes = new byte[depthAndIRImages.Depth.Resource.Size];
                    this.infraredBytes = new byte[depthAndIRImages.IR.Resource.Size];
                }

                // Copy the depth image data.
                depthAndIRImages.Depth.Resource.CopyTo(this.depthBytes);
                var depthMemory = new Memory<byte>(this.depthBytes);
                depthMemory.CopyTo(this.capture.Depth.Memory);

                // Copy the IR image data.
                depthAndIRImages.IR.Resource.CopyTo(this.infraredBytes);
                var infraredMemory = new Memory<byte>(this.infraredBytes);
                infraredMemory.CopyTo(this.capture.IR.Memory);

                // Call the body tracker.
                this.tracker.EnqueueCapture(this.capture);
                using (var bodyFrame = this.tracker.PopResult(false))
                {
                    // Parse the output into a list of KinectBody's to post
                    if (bodyFrame != null)
                    {
                        uint bodyIndex = 0;
                        while (bodyIndex < bodyFrame.NumberOfBodies)
                        {
                            if (bodyIndex >= this.currentBodies.Count)
                            {
                                this.currentBodies.Add(new AzureKinectBody());
                            }

                            this.currentBodies[(int)bodyIndex].CopyFrom(bodyFrame.GetBody(bodyIndex));
                            bodyIndex++;
                        }

                        this.currentBodies.RemoveRange((int)bodyIndex, this.currentBodies.Count - (int)bodyIndex);
                    }
                    else
                    {
                        this.currentBodies.Clear();
                    }
                }

                this.Out.Post(this.currentBodies, envelope.OriginatingTime);
            }
        }

        private void InitializeTracker(Calibration calibration)
        {
            // Static Lock to prevent external error when creating multiple trackers simultanously.
            lock (TrackerCreationLock)
            {
                this.tracker = Tracker.Create(calibration, new TrackerConfiguration()
                {
                    SensorOrientation = this.configuration.SensorOrientation,
                    ProcessingMode = this.configuration.CpuOnlyMode ? TrackerProcessingMode.Cpu : TrackerProcessingMode.Gpu,
                    ModelPath = this.configuration.UseLiteModel ? "dnn_model_2_0_lite_op11.onnx" : "dnn_model_2_0_op11.onnx",
                });
            }

            this.tracker.SetTemporalSmooting(this.configuration.TemporalSmoothing);
        }

        private void ReceiveCalibration(Calibration calibration, Envelope envelope)
        {
            if (this.tracker == null)
            {
                this.InitializeTracker(calibration);
            }
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect.Face
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Face;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Kinect;

    /// <summary>
    /// Used for receiving information from a Kinect sensor
    /// </summary>
    public class KinectFaceDetector : IKinectFaceDetector, IStartable, IDisposable
    {
        private Kinect.KinectSensor kinectSensor = null;
        private KinectFaceDetectorConfiguration configuration = null;
        private Pipeline pipeline;

        private FaceFrameReader[] faceFrameReaders = null;
        private FaceFrameSource[] faceFrameSources = null;

        private int trackedBodies = 0;
        private List<KinectFace> kinectFaces = null;
        private bool disposed = false;
        private KinectBodyReceiver bodyReceiver = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="KinectFaceDetector"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this sensor is a part of</param>
        /// <param name="kinectSensor">Psi Kinect device from which we get our associated bodies</param>
        /// <param name="configuration">Configuration to use</param>
        public KinectFaceDetector(Pipeline pipeline, Kinect.KinectSensor kinectSensor, KinectFaceDetectorConfiguration configuration)
        {
            this.pipeline = pipeline;
            this.configuration = configuration;
            this.kinectSensor = kinectSensor;
            this.Faces = pipeline.CreateEmitter<List<KinectFace>>(this, nameof(this.Faces));
            this.bodyReceiver = new KinectBodyReceiver(pipeline, this);
            kinectSensor.Bodies.PipeTo(this.bodyReceiver);
        }

        /// <summary>
        /// Gets the list of faces from the Kinect
        /// </summary>
        public Emitter<List<KinectFace>> Faces { get; private set; }

        /// <summary>
        /// Called with a list of bodies we got from our Kinect sensor
        /// </summary>
        /// <param name="kinectBodies">List of KinectBody</param>
        /// <param name="e">Envelope containing originating time of the kinect bodies</param>
        public void UpdateFaceTracking(List<KinectBody> kinectBodies, Envelope e)
        {
            if (kinectBodies.Count == 0)
            {
                this.Faces.Post(new List<KinectFace>(), e.OriginatingTime);
                return;
            }

            // Check if the body count changed. If so, we need to re-attach
            // our face frame sources and readers
            if (kinectBodies.Count != this.trackedBodies)
            {
                // Re-attach our face sources and readers
                if (this.faceFrameSources != null)
                {
                    for (int i = 0; i < this.faceFrameSources.Length; i++)
                    {
                        this.faceFrameReaders[i]?.Dispose();
                        this.faceFrameSources[i]?.Dispose();
                    }

                    this.faceFrameSources = null;
                    this.faceFrameReaders = null;
                }

                this.faceFrameSources = new FaceFrameSource[kinectBodies.Count];
                this.faceFrameReaders = new FaceFrameReader[kinectBodies.Count];
                for (int i = 0; i < kinectBodies.Count; i++)
                {
                    this.faceFrameSources[i] = new FaceFrameSource(this.kinectSensor.KinectDevice, 0, this.configuration.FaceFrameFeatures);
                    this.faceFrameReaders[i] = this.faceFrameSources[i].OpenReader();
                    this.faceFrameReaders[i].FrameArrived += this.FaceFrameReader_FrameArrived;
                }

                this.trackedBodies = kinectBodies.Count;
            }

            // construct the output
            for (int i = 0; i < kinectBodies.Count; i++)
            {
                if (this.faceFrameSources != null && this.faceFrameSources[i] != null)
                {
                    this.faceFrameSources[i].TrackingId = kinectBodies[i].TrackingId;
                }
            }
        }

        /// <summary>
        /// IStartable called by the pipeline when KinectSensor is activated in the pipeline
        /// </summary>
        /// <param name="onCompleted">Unused</param>
        /// <param name="descriptor">Parameter Unused</param>
        void IStartable.Start(Action onCompleted, ReplayDescriptor descriptor)
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(nameof(KinectFaceDetector));
            }
        }

        /// <summary>
        /// Called by the pipeline to stop the sensor
        /// </summary>
        void IStartable.Stop()
        {
        }

        /// <summary>
        /// Called to release the sensor
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                if (this.faceFrameSources != null)
                {
                    for (int i = 0; i < this.faceFrameSources.Length; i++)
                    {
                        this.faceFrameReaders[i]?.Dispose();
                        this.faceFrameSources[i]?.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Defines callback for handling when a face frame arrives from the kinect sensor
        /// </summary>
        /// <param name="sender">Kinect device sending this event</param>
        /// <param name="e">Event data</param>
        internal void FaceFrameReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null && faceFrame.FaceFrameResult != null)
                {
                    if (this.kinectFaces == null || this.kinectFaces.Count != this.trackedBodies)
                    {
                        this.kinectFaces = new List<KinectFace>(this.trackedBodies);
                        for (int i = 0; i < this.trackedBodies; i++)
                        {
                            this.kinectFaces.Add(new KinectFace());
                        }
                    }

                    // determine which body this data goes with
                    int index = 0;
                    for (; index < this.trackedBodies; index++)
                    {
                        if (faceFrame.TrackingId == this.faceFrameSources[index].TrackingId)
                        {
                            this.kinectFaces[index].FaceBoundingBoxInColorSpace = faceFrame.FaceFrameResult.FaceBoundingBoxInColorSpace.DeepClone();
                            this.kinectFaces[index].FaceBoundingBoxInInfraredSpace = faceFrame.FaceFrameResult.FaceBoundingBoxInInfraredSpace.DeepClone();
                            this.kinectFaces[index].FaceFrameFeatures = faceFrame.FaceFrameResult.FaceFrameFeatures.DeepClone();
                            this.kinectFaces[index].FacePointsInColorSpace = this.CloneDictionary(faceFrame.FaceFrameResult.FacePointsInColorSpace);
                            this.kinectFaces[index].FacePointsInInfraredSpace = this.CloneDictionary(faceFrame.FaceFrameResult.FacePointsInInfraredSpace);
                            this.kinectFaces[index].FaceProperties = this.CloneDictionary(faceFrame.FaceFrameResult.FaceProperties);
                            this.kinectFaces[index].TrackingId = faceFrame.TrackingId;
                            var time = this.pipeline.GetCurrentTimeFromElapsedTicks(e.FrameReference.RelativeTime.Ticks);
                            this.Faces.Post(this.kinectFaces, time);
                            break;
                        }
                    }
                }
            }
        }

        private Dictionary<TKey, TVal> CloneDictionary<TKey, TVal>(IReadOnlyDictionary<TKey, TVal> dictionaryIn)
        {
            Dictionary<TKey, TVal> dictionary = new Dictionary<TKey, TVal>();
            foreach (var key in dictionaryIn.Keys)
            {
                dictionary[key] = dictionaryIn[key];
            }

            return dictionary;
        }

        /// <summary>
        /// Define an internal component used to receive KinectBody's from
        /// our Kinect sensor device associated with this face detector
        /// </summary>
        internal class KinectBodyReceiver : IConsumer<List<KinectBody>>
        {
            private KinectFaceDetector faceDetector = null;

            /// <summary>
            /// Initializes a new instance of the <see cref="KinectBodyReceiver"/> class.
            /// Defines an internal receiver for receiving the KinectBody from our associated Kinect sensor
            /// </summary>
            /// <param name="pipeline">Pipeline sensor is running in</param>
            /// <param name="faceDetector">Our parent face detector</param>
            public KinectBodyReceiver(Pipeline pipeline, KinectFaceDetector faceDetector)
            {
                this.faceDetector = faceDetector;
                this.In = pipeline.CreateReceiver<List<KinectBody>>(this, this.ReceiveInput, nameof(this.In));
            }

            /// <summary>
            /// Gets or sets receives the KinectBody
            /// </summary>
            public Receiver<List<KinectBody>> In { get; set; }

            /// <summary>
            /// Callback for processing a list of KinectBodys once it is received from the Kinect sensor
            /// </summary>
            /// <param name="kinectBodies">List of Kinect bodies</param>
            /// <param name="e">Envelope</param>
            public void ReceiveInput(List<KinectBody> kinectBodies, Envelope e)
            {
                this.faceDetector.UpdateFaceTracking(kinectBodies, e);
            }
        }
    }
}

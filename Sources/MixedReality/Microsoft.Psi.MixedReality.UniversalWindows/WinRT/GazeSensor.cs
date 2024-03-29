﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.WinRT
{
    using System;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;
    using Windows.Perception;
    using Windows.Perception.People;
    using Windows.UI.Input;
    using Windows.UI.Input.Spatial;

    /// <summary>
    /// Source component that emits head and/or eye gaze using WinRT.
    /// </summary>
    /// <remarks>The origin of the eyes and head poses are between the user's eyes.
    /// If emitting both eye and head gaze, the head poses are computed and emitted
    /// in alignment with the timestamp of each eye gaze sample.
    /// See here for more information about the WinRT APIs:
    /// https://learn.microsoft.com/en-us/windows/mixed-reality/develop/native/gaze-in-directx.</remarks>
    public class GazeSensor : Generator, IProducer<(Eyes, CoordinateSystem)>
    {
        private readonly Pipeline pipeline;
        private readonly GazeSensorConfiguration configuration;
        private Emitter<Eyes> eyesEmitter;
        private Emitter<CoordinateSystem> headEmitter;

        /// <summary>
        /// Initializes a new instance of the <see cref="GazeSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for this component.</param>
        /// <param name="name">An optional name for the component.</param>
        public GazeSensor(Pipeline pipeline, GazeSensorConfiguration configuration = null, string name = nameof(GazeSensor))
            : base(pipeline, true, name)
        {
            this.pipeline = pipeline;
            this.Out = this.pipeline.CreateEmitter<(Eyes, CoordinateSystem)>(this, nameof(this.Out));
            this.configuration = configuration ?? new GazeSensorConfiguration();

            // If configured to emit eye gaze, make sure that eye gaze is supported and accessible on the device.
            if (this.configuration.OutputEyeGaze)
            {
                this.pipeline.PipelineRun += (_, _) =>
                {
                    if (!EyesPose.IsSupported())
                    {
                        throw new Exception("Eye gaze is not supported by the current headset.");
                    }

                    var accessStatus = EyesPose.RequestAccessAsync().GetAwaiter().GetResult();
                    if (accessStatus != GazeInputAccessStatus.Allowed)
                    {
                        throw new Exception($"Gaze Input access denied: {accessStatus}");
                    }
                };
            }
        }

        /// <summary>
        /// Gets the emitter for the eyes and head gaze.
        /// </summary>
        public Emitter<(Eyes, CoordinateSystem)> Out { get; }

        /// <summary>
        /// Gets the emitter for the eye gaze.
        /// </summary>
        /// <remarks>The origin of the pose ray is between the user's eyes.</remarks>
        public Emitter<Eyes> Eyes => this.eyesEmitter ??= this.Out.Select(t => t.Item1, DeliveryPolicy.SynchronousOrThrottle).Out;

        /// <summary>
        /// Gets the emitter for the head pose.
        /// </summary>
        public Emitter<CoordinateSystem> Head => this.headEmitter ??= this.Out.Select(t => t.Item2, DeliveryPolicy.SynchronousOrThrottle).Out;

        /// <inheritdoc/>
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            // Query for the gaze
            var perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(currentTime);
            var spatialPointerPose = SpatialPointerPose.TryGetAtTimestamp(MixedReality.WorldSpatialCoordinateSystem, perceptionTimestamp);
            var headPose = spatialPointerPose?.Head;
            var eyesPose = spatialPointerPose?.Eyes;

            // If outputting eye gaze
            if (this.configuration.OutputEyeGaze)
            {
                // Compute the originating time. If eyePose is null, we have no timestamp from the eye tracking device.
                // In this case we use the originating time based on the perception timestamp. O/w get the actual timestamp
                // for this eye gaze result, as reported by the underlying eye tracker device
                var originatingTime = eyesPose is null ?
                    this.pipeline.GetCurrentTimeFromElapsedTicks(perceptionTimestamp.SystemRelativeTargetTime.Ticks) :
                    this.pipeline.GetCurrentTimeFromElapsedTicks(eyesPose.UpdateTimestamp.SystemRelativeTargetTime.Ticks);

                if (originatingTime > this.Out.LastEnvelope.OriginatingTime)
                {
                    // Construct the eyes object
                    var eyes = eyesPose is null ? null : new Eyes(eyesPose.Gaze?.ToRay3D(), eyesPose.IsCalibrationValid);

                    // If also outputting head gaze
                    var head = default(CoordinateSystem);
                    if (this.configuration.OutputHeadGaze)
                    {
                        // If the eyes pose is null, use the current headpose. O/w use the actual time of the eye gaze result
                        // to re-query for the head pose at the exact same time.
                        head = eyesPose is null ?
                            headPose?.ToCoordinateSystem() :
                            SpatialPointerPose.TryGetAtTimestamp(MixedReality.WorldSpatialCoordinateSystem, eyesPose.UpdateTimestamp)?.Head?.ToCoordinateSystem();
                    }

                    // Post results
                    this.Out.Post((eyes, head), originatingTime);
                }
            }
            else if (this.configuration.OutputHeadGaze)
            {
                // Get the current timestamp
                var originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks(perceptionTimestamp.SystemRelativeTargetTime.Ticks);

                // Post results
                if (originatingTime > this.Out.LastEnvelope.OriginatingTime)
                {
                    this.Out.Post((default, headPose?.ToCoordinateSystem()), originatingTime);
                }
            }

            return currentTime + this.configuration.Interval;
        }
    }
}
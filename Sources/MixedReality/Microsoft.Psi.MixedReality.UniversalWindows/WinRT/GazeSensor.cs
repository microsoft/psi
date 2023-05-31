// Copyright (c) Microsoft Corporation. All rights reserved.
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
    public class GazeSensor : Generator
    {
        private readonly Pipeline pipeline;
        private readonly GazeSensorConfiguration configuration;

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
            this.Eyes = this.pipeline.CreateEmitter<Eyes>(this, nameof(this.Eyes));
            this.Head = this.pipeline.CreateEmitter<CoordinateSystem>(this, nameof(this.Head));
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
        /// Gets the emitter for the eye gaze.
        /// </summary>
        /// <remarks>The origin of the pose ray is between the user's eyes.</remarks>
        public Emitter<Eyes> Eyes { get; }

        /// <summary>
        /// Gets the emitter for the head gaze.
        /// </summary>
        /// <remarks>The origin of the pose is between the user's eyes.</remarks>
        public Emitter<CoordinateSystem> Head { get; }

        /// <inheritdoc/>
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            // Get the current timestamp
            var perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(currentTime);
            var originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks(perceptionTimestamp.SystemRelativeTargetTime.Ticks);

            // Query for the gaze
            var spatialPointerPose = SpatialPointerPose.TryGetAtTimestamp(MixedReality.WorldSpatialCoordinateSystem, perceptionTimestamp);
            var headPose = spatialPointerPose?.Head;
            var eyesPose = spatialPointerPose?.Eyes;

            if (this.configuration.OutputEyeGaze && eyesPose is null)
            {
                // If configured to output eye gaze, but we received a null result, and thus have no timestamp from the
                // eye tracking device, simply return without posting anything for the eyes or head.
                return currentTime + this.configuration.Interval;
            }

            // Eyes
            if (this.configuration.OutputEyeGaze)
            {
                // Get the actual timestamp for this eye gaze result, as reported by the underlying eye tracker device
                originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks(eyesPose.UpdateTimestamp.SystemRelativeTargetTime.Ticks);

                if (originatingTime > this.Eyes.LastEnvelope.OriginatingTime)
                {
                    this.Eyes.Post(new Eyes(eyesPose.Gaze?.ToRay3D(), eyesPose.IsCalibrationValid), originatingTime);
                }

                if (this.configuration.OutputHeadGaze)
                {
                    // Use the actual time of the eye gaze result to re-query for the head pose at the exact same time.
                    headPose = SpatialPointerPose.TryGetAtTimestamp(MixedReality.WorldSpatialCoordinateSystem, eyesPose.UpdateTimestamp)?.Head;
                }
            }

            // Head
            if (this.configuration.OutputHeadGaze && originatingTime > this.Head.LastEnvelope.OriginatingTime)
            {
                this.Head.Post(headPose?.ToCoordinateSystem(), originatingTime);
            }

            return currentTime + this.configuration.Interval;
        }
    }
}

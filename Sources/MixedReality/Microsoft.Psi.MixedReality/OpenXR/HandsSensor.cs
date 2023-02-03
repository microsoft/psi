// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.OpenXR
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.MixedReality.StereoKit;

    /// <summary>
    /// Source component that produces streams containing information about the tracked hands directly from OpenXR.
    /// </summary>
    public class HandsSensor : Generator, IProducer<(Hand Left, Hand Right)>, IDisposable
    {
        private static readonly long AhatFrameWidthTicks = TimeSpan.FromSeconds(1.0 / 45.0).Ticks;

        // OpenXR functions
        private static readonly XR_xrCreateHandTrackerEXT OpenXrCreateHandTrackerEXT;
        private static readonly XR_xrDestroyHandTrackerEXT OpenXrDestroyHandTrackerEXT;
        private static readonly XR_xrLocateHandJointsEXT OpenXrLocateHandJointsEXT;

        private readonly Pipeline pipeline;
        private readonly TimeSpan interval;

        // Variables for managing joint location data
        private readonly int jointLocationSizeBytes;
        private XrHandJointLocationsEXT leftHandJointLocationsXR;
        private XrHandJointLocationsEXT rightHandJointLocationsXR;

        // Handles for the underlying hand trackers
        private ulong leftHandTrackerHandle;
        private ulong rightHandTrackerHandle;

        private Emitter<Hand> leftHandEmitter = null;
        private Emitter<Hand> rightHandEmitter = null;

        static HandsSensor()
        {
            if (!SK.IsInitialized)
            {
                throw new InvalidOperationException($"Cannot initialize any {nameof(HandsSensor)} component before calling SK.Initialize.");
            }

            if (Backend.XRType != BackendXRType.OpenXR)
            {
                throw new InvalidOperationException($"Cannot use {nameof(HandsSensor)} component if the backend XR type is not OpenXR.");
            }

            // Load the necessary OpenXR functions via StereoKit.
            OpenXrCreateHandTrackerEXT = Backend.OpenXR.GetFunction<XR_xrCreateHandTrackerEXT>("xrCreateHandTrackerEXT");
            OpenXrDestroyHandTrackerEXT = Backend.OpenXR.GetFunction<XR_xrDestroyHandTrackerEXT>("xrDestroyHandTrackerEXT");
            OpenXrLocateHandJointsEXT = Backend.OpenXR.GetFunction<XR_xrLocateHandJointsEXT>("xrLocateHandJointsEXT");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandsSensor"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="interval">Interval for emitting hands (default is 45Hz).</param>
        /// <param name="name">An optional name for the component.</param>
        public HandsSensor(Pipeline pipeline, TimeSpan interval = default, string name = nameof(HandsSensor))
            : base(pipeline, true, name)
        {
            this.pipeline = pipeline;
            this.Out = this.pipeline.CreateEmitter<(Hand Left, Hand Right)>(this, nameof(this.Out));
            this.interval = interval == default ? TimeSpan.FromTicks(AhatFrameWidthTicks) : interval;

            this.jointLocationSizeBytes = Marshal.SizeOf<XrHandJointLocationEXT>();
            this.leftHandJointLocationsXR = new XrHandJointLocationsEXT
            {
                Type = XrStructureType.XR_TYPE_HAND_JOINT_LOCATIONS_EXT,
                JointCount = (uint)HandJointIndex.MaxIndex,
                JointLocations = Marshal.AllocCoTaskMem(this.jointLocationSizeBytes * (int)HandJointIndex.MaxIndex),
            };

            this.rightHandJointLocationsXR = new XrHandJointLocationsEXT
            {
                Type = XrStructureType.XR_TYPE_HAND_JOINT_LOCATIONS_EXT,
                JointCount = (uint)HandJointIndex.MaxIndex,
                JointLocations = Marshal.AllocCoTaskMem(this.jointLocationSizeBytes * (int)HandJointIndex.MaxIndex),
            };

            this.pipeline.PipelineRun += (_, _) =>
            {
                // Open underlying OpenXR hand trackers and create handles.
                var leftHandCreateInfo = new XrHandTrackerCreateInfoEXT
                {
                    Type = XrStructureType.XR_TYPE_HAND_TRACKER_CREATE_INFO_EXT,
                    Hand = XrHandEXT.XR_HAND_LEFT_EXT,
                    HandJointSet = XrHandJointSetEXT.XR_HAND_JOINT_SET_DEFAULT_EXT,
                };

                var rightHandCreateInfo = new XrHandTrackerCreateInfoEXT
                {
                    Type = XrStructureType.XR_TYPE_HAND_TRACKER_CREATE_INFO_EXT,
                    Hand = XrHandEXT.XR_HAND_RIGHT_EXT,
                    HandJointSet = XrHandJointSetEXT.XR_HAND_JOINT_SET_DEFAULT_EXT,
                };

                var leftResult = OpenXrCreateHandTrackerEXT(Backend.OpenXR.Session, ref leftHandCreateInfo, out this.leftHandTrackerHandle);
                var rightResult = OpenXrCreateHandTrackerEXT(Backend.OpenXR.Session, ref rightHandCreateInfo, out this.rightHandTrackerHandle);

                if (leftResult != XrResult.XR_SUCCESS || rightResult != XrResult.XR_SUCCESS)
                {
                    throw new Exception("Error creating hand trackers.\n" +
                                       $"OpenXR result code for left hand tracker: {leftResult}\n" +
                                       $"OpenXR result code for right hand tracker: {rightResult}");
                }
            };
        }

        private delegate XrResult XR_xrCreateHandTrackerEXT(ulong session, ref XrHandTrackerCreateInfoEXT createInfo, out ulong handTracker);

        private delegate XrResult XR_xrDestroyHandTrackerEXT(ulong handTracker);

        private delegate XrResult XR_xrLocateHandJointsEXT(ulong handTracker, ref XrHandJointsLocateInfoEXT locateInfo, ref XrHandJointLocationsEXT locations);

        /// <inheritdoc/>
        public Emitter<(Hand Left, Hand Right)> Out { get; }

        /// <summary>
        /// Gets the stream of left hand information.
        /// </summary>
        public Emitter<Hand> Left => this.leftHandEmitter ??= this.Out.Select(
            hands => hands.Left,
            DeliveryPolicy.SynchronousOrThrottle,
            "SelectLeftHand").Out;

        /// <summary>
        /// Gets the stream of right hand information.
        /// </summary>
        public Emitter<Hand> Right => this.rightHandEmitter ??= this.Out.Select(
            hands => hands.Right,
            DeliveryPolicy.SynchronousOrThrottle,
            "SelectRightHand").Out;

        /// <inheritdoc/>
        public void Dispose()
        {
            // Free up memory and destroy the hand tracker handles.
            Marshal.FreeCoTaskMem(this.leftHandJointLocationsXR.JointLocations);
            Marshal.FreeCoTaskMem(this.rightHandJointLocationsXR.JointLocations);
            OpenXrDestroyHandTrackerEXT(this.leftHandTrackerHandle);
            OpenXrDestroyHandTrackerEXT(this.rightHandTrackerHandle);
        }

        /// <inheritdoc/>
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            // HoloLens 2 computes hand tracking from the AHAT (articulated hand tracking)
            // depth sensor stream (45 FPS). To ensure that the underlying hand tracker has
            // a computed result ready, query at a time of 1 Ahat frame in the past.
            var queryTimeTicks = TimeHelper.GetCurrentTimeHnsTicks() - AhatFrameWidthTicks;

            // The proper originating time to associate with the result appears to be
            // 1 Ahat frame length subtracted from the actual query time.
            var originatingTime = this.pipeline.GetCurrentTimeFromElapsedTicks(queryTimeTicks - AhatFrameWidthTicks);

            var locateInfo = new XrHandJointsLocateInfoEXT
            {
                Type = XrStructureType.XR_TYPE_HAND_JOINTS_LOCATE_INFO_EXT,
                BaseSpace = Backend.OpenXR.Space,
                Time = TimeHelper.ConvertHnsTicksToXrTime(queryTimeTicks),
            };

            // Query the left hand tracker.
            var leftHand = default(Hand);
            if (OpenXrLocateHandJointsEXT(this.leftHandTrackerHandle, ref locateInfo, ref this.leftHandJointLocationsXR) == XrResult.XR_SUCCESS)
            {
                leftHand = this.CreateHand(this.leftHandJointLocationsXR);
            }

            // Query the right hand tracker.
            var rightHand = default(Hand);
            if (OpenXrLocateHandJointsEXT(this.rightHandTrackerHandle, ref locateInfo, ref this.rightHandJointLocationsXR) == XrResult.XR_SUCCESS)
            {
                rightHand = this.CreateHand(this.rightHandJointLocationsXR);
            }

            this.Out.Post((leftHand, rightHand), originatingTime);
            return currentTime + this.interval;
        }

        private Hand CreateHand(XrHandJointLocationsEXT handJointLocationsXR)
        {
            // Since StereoKitToWorld could be updated by the Stepper method (which is not mutually exclusive with
            // this method), we capture its value once at the start and use it to compute all the joint transforms.
            var stereoKitToWorld = StereoKitTransforms.StereoKitToWorld;
            if (stereoKitToWorld is null)
            {
                return null;
            }

            bool isActive = handJointLocationsXR.IsActive;
            var handJoints = new CoordinateSystem[(int)HandJointIndex.MaxIndex];
            var validJoints = new bool[(int)HandJointIndex.MaxIndex];
            var trackedJoints = new bool[(int)HandJointIndex.MaxIndex];
            for (int i = 0; i < (int)HandJointIndex.MaxIndex; i++)
            {
                // Read the joint data
                var ptr = handJointLocationsXR.JointLocations + (i * this.jointLocationSizeBytes);
                var jointLocationXR = Marshal.PtrToStructure<XrHandJointLocationEXT>(ptr);

                // Check if the joint pose is marked as valid.
                validJoints[i] = (jointLocationXR.LocationFlags & XrSpaceLocationFlags.XR_SPACE_LOCATION_POSITION_VALID_BIT) != 0;

                // Check if the joint pose is marked as tracked.
                trackedJoints[i] = (jointLocationXR.LocationFlags & XrSpaceLocationFlags.XR_SPACE_LOCATION_POSITION_TRACKED_BIT) != 0;

                // Construct the joint pose
                var pose = new Pose(jointLocationXR.Pose.Position, jointLocationXR.Pose.Orientation);
                handJoints[i] = pose.ToCoordinateSystem().TransformBy(stereoKitToWorld);
            }

            return new Hand(isActive, handJoints, validJoints, trackedJoints);
        }
    }
}

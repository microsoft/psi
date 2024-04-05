// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using MathNet.Spatial.Units;
    using Microsoft.Psi;
    using Microsoft.Psi.MixedReality.Applications;
    using Microsoft.Psi.MixedReality.StereoKit;
    using StereoKit;
    using Hand = Microsoft.Psi.MixedReality.OpenXR.Hand;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements the gem user interface.
    /// </summary>
    public class GemUserInterface
    {
        private readonly GemUserInterfaceConfiguration configuration;

        // Constants for gem animation
        private readonly float defaultGemSize = 0.008f;
        private readonly float userIsMovingGemSize = 0.016f;
        private readonly float departureAnimationTime = 0.2f;
        private readonly float arrivalAnimationTime = 0.2f;
        private readonly float gemMovementMetersPerSecond = 1f;
        private readonly float gemMovementLineWidth = 0.001f;
        private readonly Color gemColor = new (0f, 160 / 255f, 1f, 0.5f);
        private readonly float gemAnimationExpandRatio = 1.2f;

        private double targetGemSize = 0;

        private bool gemIsRotating = false;

        private float gemSize = 0.008f;
        private CoordinateSystem gemMovementStartPose = default;
        private CoordinateSystem gemMovementEndPose = default;
        private DateTime gemMovementStartTime = DateTime.MinValue;
        private DateTime gemMovementEndTime = DateTime.MinValue;
        private float gemSizeAtStart = 0;
        private float gemSizeAtEnd = 0;

        private bool userIsMovingGemWithLeftHand = false;
        private bool userIsMovingGem = false;

        // rendering resources
        private Mesh gemMesh;
        private Material gemMeshMaterial;

        /// <summary>
        /// Initializes a new instance of the <see cref="GemUserInterface"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public GemUserInterface(GemUserInterfaceConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets or sets the current gem pose.
        /// </summary>
        public CoordinateSystem CurrentGemPose { get; protected set; } = null;

        /// <summary>
        /// Gets or sets the target gem pose.
        /// </summary>
        public CoordinateSystem TargetGemPose { get; protected set; } = null;

        /// <summary>
        /// Gets or sets the event handler for when the gem is moved by the user.
        /// </summary>
        public EventHandler OnGemMovedByUser { get; set; }

        /// <summary>
        /// Updates the user interface state.
        /// </summary>
        /// <param name="command">The gem user interface update command.</param>
        public void Update(GemUserInterfaceCommand command)
        {
            if (command == null)
            {
                return;
            }

            // Update the target gem pose
            if (command.GemPose != null)
            {
                this.TargetGemPose = command.GemPose;
            }

            // Update the target gem size and whether the gem is rotating
            this.targetGemSize = command.GemSize;
            this.gemIsRotating = command.GemIsRotating;

            // If we have a specified gem target but no current gem position (we're at startup)
            if (this.TargetGemPose != null && this.CurrentGemPose == null)
            {
                this.CurrentGemPose = this.TargetGemPose;
                return;
            }

            // If we have a specified gem target, or we're already executing a movement
            if (this.TargetGemPose != null || this.gemMovementStartTime != DateTime.MinValue)
            {
                var now = DateTime.Now;

                // If we're just starting a new gem move animation
                if (this.gemMovementStartTime == DateTime.MinValue &&
                    this.TargetGemPose != null &&
                    this.TargetGemPose.Origin != this.CurrentGemPose.Origin)
                {
                    // Record the start time and the start position
                    this.gemMovementStartPose = this.CurrentGemPose.DeepClone();
                    this.gemMovementEndPose = this.TargetGemPose.DeepClone();
                    this.gemMovementStartTime = now;
                    var movementDuration = Math.Max(
                        this.departureAnimationTime + this.arrivalAnimationTime + 0.3,
                        this.TargetGemPose.Origin.DistanceTo(this.CurrentGemPose.Origin) / this.gemMovementMetersPerSecond);
                    this.gemMovementEndTime = now + TimeSpan.FromSeconds(movementDuration);
                    this.gemSizeAtStart = this.gemSize;
                    this.gemSizeAtEnd = (float)this.targetGemSize;
                }

                // If we're performing a gem movement
                if (this.gemMovementStartTime != DateTime.MinValue)
                {
                    // Compute the new gem location and size
                    var timeSinceStart = now - this.gemMovementStartTime;
                    var totalAnimationTime = (this.gemMovementEndTime - this.gemMovementStartTime).TotalSeconds;

                    // If we're in the departure animation phase
                    if (timeSinceStart < TimeSpan.FromSeconds(this.departureAnimationTime))
                    {
                        if (timeSinceStart < TimeSpan.FromSeconds(this.departureAnimationTime / 2))
                        {
                            var rr = timeSinceStart.Ticks / (float)TimeSpan.FromSeconds(this.departureAnimationTime / 2).Ticks;
                            this.gemSize = this.gemSizeAtStart * (1 - rr) + this.gemSizeAtStart * this.gemAnimationExpandRatio * rr;
                        }
                        else
                        {
                            var rr = (timeSinceStart - TimeSpan.FromSeconds(this.departureAnimationTime / 2)).Ticks / (float)TimeSpan.FromSeconds(this.departureAnimationTime / 2).Ticks;
                            this.gemSize = this.gemSizeAtStart * this.gemAnimationExpandRatio * (1 - rr);
                        }

                        this.CurrentGemPose = this.gemMovementStartPose;
                    }

                    // O/w if we're in the movement animation phase
                    else if (timeSinceStart < TimeSpan.FromSeconds(totalAnimationTime - this.arrivalAnimationTime))
                    {
                        var ratio = (timeSinceStart - TimeSpan.FromSeconds(this.departureAnimationTime)).Ticks / (double)TimeSpan.FromSeconds(totalAnimationTime - this.departureAnimationTime - this.arrivalAnimationTime).Ticks;
                        this.gemSize = 0;
                        this.CurrentGemPose = new CoordinateSystem(
                            new Point3D(
                                this.gemMovementStartPose.Origin.X * (1 - ratio) + this.gemMovementEndPose.Origin.X * ratio,
                                this.gemMovementStartPose.Origin.Y * (1 - ratio) + this.gemMovementEndPose.Origin.Y * ratio,
                                this.gemMovementStartPose.Origin.Z * (1 - ratio) + this.gemMovementEndPose.Origin.Z * ratio),
                            UnitVector3D.XAxis,
                            UnitVector3D.YAxis,
                            UnitVector3D.ZAxis);
                    }

                    // O/w if we're in the arrival animation phase
                    else
                    {
                        if (timeSinceStart < TimeSpan.FromSeconds(totalAnimationTime - this.arrivalAnimationTime / 2))
                        {
                            var rr = (timeSinceStart - TimeSpan.FromSeconds(totalAnimationTime - this.arrivalAnimationTime)).Ticks / (float)TimeSpan.FromSeconds(this.arrivalAnimationTime / 2).Ticks;
                            this.gemSize = this.gemSizeAtEnd * this.gemAnimationExpandRatio * rr;
                        }
                        else if (timeSinceStart < TimeSpan.FromSeconds(totalAnimationTime))
                        {
                            var rr = (timeSinceStart - TimeSpan.FromSeconds(totalAnimationTime - this.arrivalAnimationTime / 2)).Ticks / (float)TimeSpan.FromSeconds(this.arrivalAnimationTime / 2).Ticks;
                            this.gemSize = this.gemSizeAtEnd * this.gemAnimationExpandRatio * (1 - rr) + (float)this.gemSizeAtEnd * rr;
                        }
                        else
                        {
                            // Finish the animation
                            this.gemMovementStartTime = DateTime.MinValue;
                            this.gemMovementEndTime = DateTime.MinValue;
                            this.gemSize = this.gemSizeAtEnd;
                            this.TargetGemPose = null;
                        }

                        this.CurrentGemPose = this.gemMovementEndPose;
                    }
                }
            }
        }

        /// <summary>
        /// Handles user input.
        /// </summary>
        /// <param name="userState">The user state.</param>
        public void HandleUserInputs(UserState userState)
        {
            // If we are not already moving the UI determine if we should enter a moving the UI mode
            if (!this.userIsMovingGem)
            {
                // For each hand
                foreach (var hand in new Hand[] { userState.HandLeft, userState.HandRight })
                {
                    if (hand != null)
                    {
                        // Determine whether the hand is pinching and what the pinch point
                        // is (mid point between index and thumb tip)
                        var isPinching = hand.IsPinching(out var pinchPoint);
                        if (isPinching && this.CurrentGemPose != null)
                        {
                            var distance = pinchPoint.DistanceTo(this.CurrentGemPose.Origin);
                            if (distance < this.configuration.GraspDistance)
                            {
                                this.userIsMovingGem = true;
                                this.gemSize = this.userIsMovingGemSize;
                                this.userIsMovingGemWithLeftHand = hand == userState.HandLeft;
                            }
                        }
                    }
                }
            }
            else
            {
                // o/w if we are editing something but the hand is no longer pinching
                var pinchingHand = this.userIsMovingGemWithLeftHand ? userState.HandLeft : userState.HandRight;
                if (!pinchingHand.IsPinching(out var _))
                {
                    // then move the location to the new point
                    this.userIsMovingGem = false;
                    this.gemSize = this.defaultGemSize;
                }
            }

            // now move the target if we are in moving mode
            if (this.userIsMovingGem)
            {
                // figure out the pinching hand and the pinch point
                var pinchingHand = this.userIsMovingGemWithLeftHand ? userState.HandLeft : userState.HandRight;
                pinchingHand.IsPinching(out var newPosition);
                this.CurrentGemPose = Operators.GetTargetOrientedCoordinateSystem(newPosition, userState.Head.Origin);
                this.OnGemMovedByUser?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Renders the models using the specified renderer.
        /// </summary>
        /// <param name="renderer">The renderer to use when rending the models.</param>
        public void Render(Renderer renderer)
        {
            if (this.CurrentGemPose != null)
            {
                // If the gem has a non-zero radius
                if (this.gemSize > 0)
                {
                    // Then it means we're displaying the sphere
                    this.gemMesh ??= this.CreateGemMesh(renderer);

                    // There's probably a better way to directly scale the coordinate system?
                    var gemPose = this.CurrentGemPose.ToStereoKitPose().ToMatrix(this.gemSize).ToCoordinateSystem();
                    var gemWireframePose = this.CurrentGemPose.ToStereoKitPose().ToMatrix(this.gemSize * 1.05f).ToCoordinateSystem();

                    if (this.gemIsRotating)
                    {
                        var rotation = CoordinateSystem.Yaw(Angle.FromRadians(DateTime.Now.TimeOfDay.TotalSeconds));
                        gemPose = rotation.TransformBy(gemPose);
                        gemWireframePose = rotation.TransformBy(gemWireframePose);
                    }

                    renderer.RenderMesh(gemPose, this.gemMesh, this.gemMeshMaterial);
                    renderer.RenderMesh(gemWireframePose, this.gemMesh, renderer.WireframeMaterial);
                }
                else
                {
                    // O/w it means we're moving so render a line from the current gem location to the start location
                    renderer.RenderLine(this.CurrentGemPose.Origin, this.gemMovementStartPose.Origin, this.gemMovementLineWidth, Color.White);
                }
            }
        }

        /// <summary>
        /// Creates the Sigma gem mesh.
        /// </summary>
        /// <param name="renderer">The renderer used to manage visual resources.</param>
        /// <returns>The bubble mesh.</returns>
        private Mesh CreateGemMesh(Renderer renderer)
        {
            // Create the bubble mesh material
            this.gemMeshMaterial = renderer.GetOrCreateMaterial(this.gemColor);
            this.gemMeshMaterial.Transparency = Transparency.Add;

            // Compute the vertices
            var topFaceRadius = 0.4f;
            var secondFaceRadius = 0.5f;
            var topFaceZ = 0.5f;
            var secondFaceZ = 0.3f;
            var bottomPointZ = -0.5f;
            var vertices = new List<Point3D>()
            {
                // Top face
                new (topFaceRadius * Math.Cos(0 * Math.PI / 3), topFaceRadius * Math.Sin(0 * Math.PI / 3), topFaceZ),  // 0
                new (topFaceRadius * Math.Cos(1 * Math.PI / 3), topFaceRadius * Math.Sin(1 * Math.PI / 3), topFaceZ),  // 1
                new (topFaceRadius * Math.Cos(2 * Math.PI / 3), topFaceRadius * Math.Sin(2 * Math.PI / 3), topFaceZ),  // 2
                new (topFaceRadius * Math.Cos(3 * Math.PI / 3), topFaceRadius * Math.Sin(3 * Math.PI / 3), topFaceZ),  // 3
                new (topFaceRadius * Math.Cos(4 * Math.PI / 3), topFaceRadius * Math.Sin(4 * Math.PI / 3), topFaceZ),  // 4
                new (topFaceRadius * Math.Cos(5 * Math.PI / 3), topFaceRadius * Math.Sin(5 * Math.PI / 3), topFaceZ),  // 5

                // Second face
                new (secondFaceRadius * Math.Cos(0 * Math.PI / 4), secondFaceRadius * Math.Sin(0 * Math.PI / 4), secondFaceZ), // 6
                new (secondFaceRadius * Math.Cos(1 * Math.PI / 4), secondFaceRadius * Math.Sin(1 * Math.PI / 4), secondFaceZ), // 7
                new (secondFaceRadius * Math.Cos(2 * Math.PI / 4), secondFaceRadius * Math.Sin(2 * Math.PI / 4), secondFaceZ), // 8
                new (secondFaceRadius * Math.Cos(3 * Math.PI / 4), secondFaceRadius * Math.Sin(3 * Math.PI / 4), secondFaceZ), // 9
                new (secondFaceRadius * Math.Cos(4 * Math.PI / 4), secondFaceRadius * Math.Sin(4 * Math.PI / 4), secondFaceZ), // 10
                new (secondFaceRadius * Math.Cos(5 * Math.PI / 4), secondFaceRadius * Math.Sin(5 * Math.PI / 4), secondFaceZ), // 11
                new (secondFaceRadius * Math.Cos(6 * Math.PI / 4), secondFaceRadius * Math.Sin(6 * Math.PI / 4), secondFaceZ), // 12
                new (secondFaceRadius * Math.Cos(7 * Math.PI / 4), secondFaceRadius * Math.Sin(7 * Math.PI / 4), secondFaceZ), // 13

                // Bottom point
                new (0, 0, bottomPointZ), // 14

                // Top point
                new (0, 0, topFaceZ), // 15
            };

            var indices = new List<uint>()
            {
                0, 1, 15,
                1, 2, 15,
                2, 3, 15,
                3, 4, 15,
                4, 5, 15,
                5, 0, 15,
                0, 6, 7,
                0, 7, 1,
                1, 7, 8,
                1, 8, 2,
                2, 8, 9,
                2, 9, 3,
                3, 9, 10,
                3, 10, 11,
                3, 11, 4,
                4, 11, 12,
                4, 12, 5,
                5, 12, 13,
                5, 13, 0,
                0, 13, 6,
                6, 14, 7,
                7, 14, 8,
                8, 14, 9,
                9, 14, 10,
                10, 14, 11,
                11, 14, 12,
                12, 14, 13,
                13, 14, 6,
            };

            return renderer.CreateMesh(vertices, indices);
        }
    }
}

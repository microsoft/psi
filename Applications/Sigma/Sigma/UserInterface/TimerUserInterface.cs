// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.MixedReality.Applications;
    using StereoKit;
    using Hand = Microsoft.Psi.MixedReality.OpenXR.Hand;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a StereoKit user interface for displaying a timer.
    /// </summary>
    public class TimerUserInterface : Rectangle3DUserInterface
    {
        private readonly Paragraph timerParagraph = default;
        private readonly TimersUserInterfaceConfiguration configuration;
        private readonly DateTime dateTime;

        private Color color;

        private Point3D location;
        private bool movingTimer = false;
        private bool movingWithLeftHand = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerUserInterface"/> class.
        /// </summary>
        /// <param name="configuration">The timer display configuration.</param>
        /// <param name="location">The initial location for the timer.</param>
        /// <param name="dateTime">The date time for the timer.</param>
        /// <param name="name">An optional name for the timer display.</param>
        public TimerUserInterface(TimersUserInterfaceConfiguration configuration, Point3D location, DateTime dateTime, string name = nameof(TimerUserInterface))
            : base(name)
        {
            this.configuration = configuration;
            this.location = location;
            this.dateTime = dateTime;

            this.Width = this.configuration.Width;

            this.timerParagraph = new Paragraph($"{this.Name}.TimerParagraph");
        }

        /// <summary>
        /// Updates the state of the timer display.
        /// </summary>
        public void Update()
        {
            var timeSpan = DateTime.Now - this.dateTime;
            this.color =
                this.movingTimer ? this.configuration.EditingColor :
                (timeSpan > TimeSpan.Zero ? this.configuration.OvertimeColor : this.configuration.Color);
            var timeSpanString = timeSpan.ToString(@"mm\:ss");
            if (timeSpan > TimeSpan.Zero)
            {
                timeSpanString = "+" + timeSpanString;
            }

            var textStyle =
                this.movingTimer ? this.configuration.EditingTextStyle :
                (timeSpan > TimeSpan.Zero ? this.configuration.OvertimeTextStyle : this.configuration.TextStyle);
            this.timerParagraph.Update(
                timeSpanString,
                textStyle,
                this.configuration.Width,
                bottomMargin: 0.005f,
                centered: true);

            this.Height = this.timerParagraph.Height;
        }

        /// <summary>
        /// Handles user inputs.
        /// </summary>
        /// <param name="userState">The user state.</param>
        public void HandleUserInputs(UserState userState)
        {
            // If we are not already editing the timer, we determine
            // whether we should switch to editing mode (e.g. whether the user is
            // pinching the grasp handle)
            if (!this.movingTimer)
            {
                // For each hand
                foreach (var hand in new Hand[] { userState.HandLeft, userState.HandRight })
                {
                    if (hand != null)
                    {
                        // Determine whether the hand is pinching and what the pinch point
                        // is (mid point between index and thumb tip)
                        var isPinching = hand.IsPinching(out var pinchPoint);
                        if (isPinching)
                        {
                            var distance = pinchPoint.DistanceTo(this.location);
                            if (distance < this.configuration.GraspDistance)
                            {
                                this.movingTimer = true;
                                this.movingWithLeftHand = hand == userState.HandLeft;
                            }
                        }
                    }
                }
            }
            else
            {
                // o/w if we are editing something but the hand is no longer pinching
                var pinchingHand = this.movingWithLeftHand ? userState.HandLeft : userState.HandRight;
                if (!pinchingHand.IsPinching(out var _))
                {
                    // then move the location to the new point
                    this.movingTimer = false;
                }
            }

            // now move the time if we are in moving mode
            if (this.movingTimer)
            {
                // figure out the pinching hand and the pinch point
                var pinchingHand = this.movingWithLeftHand ? userState.HandLeft : userState.HandRight;
                pinchingHand.IsPinching(out this.location);
            }
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            // Render the paragraphs
            var paragraphPose = renderer.GetHorizontalHeadOrientedCoordinateSystem(this.location)
                .ApplyUV(-this.configuration.Width / 2, this.configuration.StickHeight + this.timerParagraph.Height);
            this.timerParagraph.Render(renderer, paragraphPose);

            renderer.RenderSphere(pose.Origin, 0.02f, this.color);
            renderer.RenderLine(pose.Origin, new Point3D(pose.Origin.X, pose.Origin.Y, pose.Origin.Z + this.configuration.StickHeight), 0.002f, this.color);

            return this.GetUserInterfaceState(paragraphPose);
        }
    }
}

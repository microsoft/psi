// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Renderer = Microsoft.Psi.MixedReality.Applications.Renderer;

    /// <summary>
    /// Implements a StereoKit user interface for displaying a dialog with bubbles of text.
    /// </summary>
    internal class BubbleDialogUserInterface : Rectangle3DUserInterface
    {
        private readonly BubbleDialogUserInterfaceConfiguration configuration;
        private readonly Bubble systemPromptBubble;
        private readonly Bubble userResponseBubble;
        private readonly Bubble userResponseSetBubble;
        private readonly Bubble[] utteranceHistoryBubble;

        private string systemPrompt;
        private string userResponseInProgress;
        private string[] userResponseSet;
        private List<(string Utterance, bool System)> utteranceHistory;
        private bool showIsThinkingStatus;

        /// <summary>
        /// Initializes a new instance of the <see cref="BubbleDialogUserInterface"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="name">An optional name for the bubble dialog.</param>
        public BubbleDialogUserInterface(BubbleDialogUserInterfaceConfiguration configuration, string name = nameof(BubbleDialogUserInterface))
            : base(name)
        {
            this.configuration = configuration;
            this.Width = this.configuration.Width;

            this.systemPromptBubble = new Bubble(
                leftOrientation: true,
                this.configuration.BubbleMaxWidth,
                this.configuration.Padding,
                this.configuration.SystemBubbleColor,
                this.configuration.SystemBubbleLineColor,
                this.configuration.BubbleLineWidth,
                this.configuration.SystemBubbleTextStyle,
                $"{this.Name}.SystemPromptBubble");

            this.userResponseBubble = new Bubble(
                leftOrientation: false,
                this.configuration.BubbleMaxWidth,
                this.configuration.Padding,
                this.configuration.UserResponseBubbleColor,
                this.configuration.UserResponseBubbleLineColor,
                this.configuration.BubbleLineWidth,
                this.configuration.UserResponseBubbleTextStyle,
                $"{this.Name}.UserResponseBubble");

            this.userResponseSetBubble = new Bubble(
                leftOrientation: false,
                this.configuration.BubbleMaxWidth,
                this.configuration.Padding,
                this.configuration.UserResponseSetBubbleColor,
                this.configuration.UserResponseSetBubbleLineColor,
                this.configuration.BubbleLineWidth,
                this.configuration.UserResponseSetBubbleTextStyle,
                $"{this.Name}.UserResponseSetBubble");

            this.utteranceHistoryBubble = new Bubble[this.configuration.HistoryBubbleCount];
        }

        /// <summary>
        /// Updates the bubble dialog based on the specified command.
        /// </summary>
        /// <param name="command">The bubble dialog command.</param>
        public void Update(BubbleDialogUserInterfaceCommand command)
        {
            if (this.systemPrompt == command.SystemPrompt &&
                this.userResponseInProgress == command.UserResponseInProgress &&
                this.showIsThinkingStatus == command.ShowIsThinkingStatus &&
                Operators.EnumerableEquals(this.userResponseSet, command.UserResponseSet) &&
                Operators.EnumerableEquals(this.utteranceHistory, command.UtteranceHistory))
            {
                return;
            }
            else
            {
                this.systemPrompt = command.SystemPrompt?.DeepClone();
                this.userResponseInProgress = command.UserResponseInProgress?.DeepClone();
                this.userResponseSet = command.UserResponseSet?.DeepClone();
                this.utteranceHistory = command.UtteranceHistory?.DeepClone();
                this.showIsThinkingStatus = command.ShowIsThinkingStatus;
            }

            // Now update the bubbles and in the process compute the height.
            this.Height = 0;
            var firstBubble = true;
            if (this.userResponseInProgress != null)
            {
                this.userResponseBubble.Update(new string[] { this.userResponseInProgress });
                this.Height += this.userResponseBubble.Height;
                firstBubble = false;
            }

            // O/w if we have a user response set, render the response set
            else if (this.userResponseSet != null)
            {
                this.userResponseSetBubble.Update(this.userResponseSet);
                this.Height += this.userResponseSetBubble.Height;
                firstBubble = false;
            }

            // Update the system prompt bubble
            if (this.systemPrompt != null || this.showIsThinkingStatus)
            {
                if (this.showIsThinkingStatus)
                {
                    this.systemPromptBubble.UpdateIsThinkingIndicator();
                }
                else if (this.systemPrompt != null)
                {
                    this.systemPromptBubble.Update(this.systemPrompt != null ? new string[] { this.systemPrompt } : new string[] { });
                }

                this.Height += (firstBubble ? 0 : this.configuration.BubbleVerticalSpacing) + this.systemPromptBubble.Height;
                firstBubble = false;
            }

            // Update the history bubbles
            if (this.utteranceHistory != null)
            {
                for (int i = 0; i < Math.Min(this.configuration.HistoryBubbleCount, this.utteranceHistory.Count); i++)
                {
                    var utterance = this.utteranceHistory[i];
                    this.utteranceHistoryBubble[i] = new Bubble(
                        leftOrientation: utterance.System,
                        this.configuration.BubbleMaxWidth,
                        this.configuration.Padding,
                        utterance.System ? this.configuration.SystemBubbleColor : this.configuration.UserResponseBubbleColor,
                        utterance.System ? this.configuration.SystemBubbleLineColor : this.configuration.UserResponseBubbleLineColor,
                        this.configuration.BubbleLineWidth,
                        utterance.System ? this.configuration.SystemBubbleTextStyle : this.configuration.UserResponseBubbleTextStyle,
                        $"{this.Name}.UtteranceHistoryBubble[{i}]");
                    this.utteranceHistoryBubble[i].Update(utterance.Utterance != null ? new string[] { utterance.Utterance } : new string[] { });
                    this.Height += (firstBubble ? 0 : this.configuration.BubbleVerticalSpacing) + this.utteranceHistoryBubble[i].Height;

                    firstBubble = false;
                }
            }
        }

        /// <inheritdoc/>
        public override List<Rectangle3DUserInterfaceState> Render(Renderer renderer, CoordinateSystem pose)
        {
            var state = new List<Rectangle3DUserInterfaceState>();

            var currentHeight = 0f;
            var firstBubble = true;

            // Render the user response bubble
            if (this.userResponseInProgress != null)
            {
                currentHeight += this.userResponseBubble.Height;
                var bubblePose = CoordinateSystem.Translation(new Vector3D(0, this.configuration.Width / 2 - this.userResponseBubble.Width, currentHeight));
                state.AddRange(this.userResponseBubble.Render(renderer, bubblePose.TransformBy(pose)));
                firstBubble = false;
            }

            // O/w if we have a user response set, render the response set
            else if (this.userResponseSet != null)
            {
                currentHeight += this.userResponseSetBubble.Height;
                var bubblePose = CoordinateSystem.Translation(new Vector3D(0, this.configuration.Width / 2 - this.userResponseSetBubble.Width, currentHeight));
                state.AddRange(this.userResponseSetBubble.Render(renderer, bubblePose.TransformBy(pose)));
                firstBubble = false;
            }

            // Render the system bubble
            if (this.systemPrompt != null || this.showIsThinkingStatus)
            {
                currentHeight += (firstBubble ? 0 : this.configuration.BubbleVerticalSpacing) + this.systemPromptBubble.Height;
                var bubblePose = CoordinateSystem.Translation(new Vector3D(0, -this.configuration.Width / 2, currentHeight));
                state.AddRange(this.systemPromptBubble.Render(renderer, bubblePose.TransformBy(pose)));
                firstBubble = false;
            }

            // Render the history bubbles
            if (this.utteranceHistory != null)
            {
                for (int i = 0; i < Math.Min(this.configuration.HistoryBubbleCount, this.utteranceHistory.Count); i++)
                {
                    var utterance = this.utteranceHistory[i];
                    currentHeight += (firstBubble ? 0 : this.configuration.BubbleVerticalSpacing) + this.utteranceHistoryBubble[i].Height;

                    var bubblePose = CoordinateSystem.Translation(new Vector3D(0, this.utteranceHistory[i].System ? -this.configuration.Width / 2 : this.configuration.Width / 2 - this.utteranceHistoryBubble[i].Width, currentHeight));
                    state.AddRange(this.utteranceHistoryBubble[i].Render(renderer, bubblePose.TransformBy(pose)));
                    firstBubble = false;
                }
            }

            // Add the state for the entire bubble dialog
            state.AddRange(this.GetUserInterfaceState(pose.ApplyUV(-this.configuration.Width / 2, -currentHeight)));

            return state;
        }
    }
}

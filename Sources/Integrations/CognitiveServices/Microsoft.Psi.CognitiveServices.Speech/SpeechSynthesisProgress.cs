// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    /// <summary>
    /// An enumeration of SpeechSynthesisProgress event types.
    /// </summary>
    public enum SpeechSynthesisProgressEventType
    {
        /// <summary>
        /// The speech synthesis has started.
        /// </summary>
        SynthesisStarted,

        /// <summary>
        /// The speech synthesis is in progress.
        /// </summary>
        SynthesisInProgress,

        /// <summary>
        /// The speech synthesis has completed.
        /// </summary>
        SynthesisCompleted,

        /// <summary>
        /// The speech synthesis was cancelled
        /// </summary>
        SynthesisCancelled,
    }

    /// <summary>
    /// Represents speech synthesis progress events.
    /// </summary>
    public class SpeechSynthesisProgress
    {
        private SpeechSynthesisProgress(SpeechSynthesisProgressEventType eventType, string text = null)
        {
            this.EventType = eventType;
            this.Text = text;
        }

        /// <summary>
        /// Gets the event type.
        /// </summary>
        public SpeechSynthesisProgressEventType EventType { get; }

        /// <summary>
        /// Gets the text associated with the speech synthesis progress event.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Creates a new <see cref="SpeechSynthesisProgressEventType.SynthesisStarted"/> progress event.
        /// </summary>
        /// <returns>The new <see cref="SpeechSynthesisProgress"/> event.</returns>
        public static SpeechSynthesisProgress SynthesisStarted() => new (SpeechSynthesisProgressEventType.SynthesisStarted);

        /// <summary>
        /// Creates a new <see cref="SpeechSynthesisProgressEventType.SynthesisInProgress"/> progress event.
        /// </summary>
        /// <param name="text">The text associated with the event.</param>
        /// <returns>The new <see cref="SpeechSynthesisProgress"/> event.</returns>
        public static SpeechSynthesisProgress SynthesisInProgress(string text) => new (SpeechSynthesisProgressEventType.SynthesisInProgress, text);

        /// <summary>
        /// Creates a new <see cref="SpeechSynthesisProgressEventType.SynthesisCompleted"/> progress event.
        /// </summary>
        /// <param name="text">The text associated with the event.</param>
        /// <returns>The new <see cref="SpeechSynthesisProgress"/> event.</returns>
        public static SpeechSynthesisProgress SynthesisCompleted(string text) => new (SpeechSynthesisProgressEventType.SynthesisCompleted, text);

        /// <summary>
        /// Creates a new <see cref="SpeechSynthesisProgressEventType.SynthesisCancelled"/> progress event.
        /// </summary>
        /// <param name="text">The text associated with the event.</param>
        /// <returns>The new <see cref="SpeechSynthesisProgress"/> event.</returns>
        public static SpeechSynthesisProgress SynthesisCancelled(string text) => new (SpeechSynthesisProgressEventType.SynthesisCancelled, text);

        /// <inheritdoc/>
        public override string ToString()
            => this.EventType switch
            {
                SpeechSynthesisProgressEventType.SynthesisStarted => $"{nameof(SynthesisStarted)}",
                SpeechSynthesisProgressEventType.SynthesisInProgress => $"{nameof(SynthesisInProgress)}({this.Text})",
                SpeechSynthesisProgressEventType.SynthesisCompleted => $"{nameof(SynthesisCompleted)}({this.Text})",
                SpeechSynthesisProgressEventType.SynthesisCancelled => $"{nameof(SynthesisCancelled)}({this.Text})",
                _ => "Unexpected speech synthesizer event type.",
            };
    }
}
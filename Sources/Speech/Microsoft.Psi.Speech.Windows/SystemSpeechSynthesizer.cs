// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using System.IO;
    using System.Speech.AudioFormat;
    using System.Speech.Synthesis;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Component that performs speech synthesis via the desktop speech synthesis engine from `System.Speech`.
    /// </summary>
    /// <remarks>
    /// This component performs text-to-speech synthesis, operating on an input stream of text strings and producing a
    /// stream of audio containing the synthesized speech.
    /// </remarks>
    public sealed class SystemSpeechSynthesizer : ConsumerProducer<string, AudioBuffer>, ISourceComponent, IDisposable
    {
        private static readonly string SpeakSsmlPrefix = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en\">";
        private static readonly string SpeakSsmlPostfix = "</speak>";

        /// <summary>
        /// A pointer to the pipeline.
        /// </summary>
        private readonly Pipeline pipeline;

        /// <summary>
        /// The configuration for this component.
        /// </summary>
        private readonly SystemSpeechSynthesizerConfiguration configuration;

        /// <summary>
        /// The System.Speech speech synthesizer.
        /// </summary>
        private SpeechSynthesizer speechSynthesizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSpeechSynthesizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public SystemSpeechSynthesizer(Pipeline pipeline, SystemSpeechSynthesizerConfiguration configuration)
            : base(pipeline)
        {
            this.pipeline = pipeline;

            // Create additional receivers
            this.SpeakSsml = pipeline.CreateReceiver<string>(this, this.ReceiveSpeakSsml, nameof(this.SpeakSsml));
            this.CancelAll = pipeline.CreateReceiver<bool>(this, this.ReceiveCancelAll, nameof(this.CancelAll));

            // Create the emitters for the various events
            this.BookmarkReached = pipeline.CreateEmitter<BookmarkReachedEventData>(this, nameof(this.BookmarkReached));
            this.PhonemeReached = pipeline.CreateEmitter<PhonemeReachedEventData>(this, nameof(this.PhonemeReached));
            this.SpeakCompleted = pipeline.CreateEmitter<SpeakCompletedEventData>(this, nameof(this.SpeakCompleted));
            this.SpeakProgress = pipeline.CreateEmitter<SpeakProgressEventData>(this, nameof(this.SpeakProgress));
            this.SpeakStarted = pipeline.CreateEmitter<SpeakStartedEventData>(this, nameof(this.SpeakStarted));
            this.StateChanged = pipeline.CreateEmitter<StateChangedEventData>(this, nameof(this.StateChanged));
            this.VisemeReached = pipeline.CreateEmitter<VisemeReachedEventData>(this, nameof(this.VisemeReached));

            // save the configuration
            this.configuration = configuration;

            // create the speech synthesizer
            this.speechSynthesizer = this.CreateSpeechSynthesizer();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSpeechSynthesizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public SystemSpeechSynthesizer(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new SystemSpeechSynthesizerConfiguration() : new ConfigurationHelper<SystemSpeechSynthesizerConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Gets the receiver for issuing speak SSML commands to the component.
        /// </summary>
        public Receiver<string> SpeakSsml { get; private set; }

        /// <summary>
        /// Gets the receiver for issuing a command to stop the speech synthesizer.
        /// </summary>
        public Receiver<bool> CancelAll { get; private set; }

        /// <summary>
        /// Gets the output stream of bookmark reached events.
        /// </summary>
        public Emitter<BookmarkReachedEventData> BookmarkReached { get; private set; }

        /// <summary>
        /// Gets the output stream of phoneme reached events.
        /// </summary>
        public Emitter<PhonemeReachedEventData> PhonemeReached { get; private set; }

        /// <summary>
        /// Gets the output stream of speak completed.
        /// </summary>
        public Emitter<SpeakCompletedEventData> SpeakCompleted { get; private set; }

        /// <summary>
        /// Gets the output stream of speak progress events.
        /// </summary>
        public Emitter<SpeakProgressEventData> SpeakProgress { get; private set; }

        /// <summary>
        /// Gets the output stream of speak started events.
        /// </summary>
        public Emitter<SpeakStartedEventData> SpeakStarted { get; private set; }

        /// <summary>
        /// Gets the output stream of state changed events.
        /// </summary>
        public Emitter<StateChangedEventData> StateChanged { get; private set; }

        /// <summary>
        /// Gets the output stream of viseme reached events.
        /// </summary>
        public Emitter<VisemeReachedEventData> VisemeReached { get; private set; }

        /// <summary>
        /// Gets the configuration for this component.
        /// </summary>
        private SystemSpeechSynthesizerConfiguration Configuration
        {
            get { return this.configuration; }
        }

        /// <summary>
        /// Writes all output stream to a store, with a given prefix.
        /// </summary>
        /// <param name="prefix">The prefix for the streams.</param>
        /// <param name="store">The store to write the streams to.</param>
        public void Write(string prefix, Exporter store)
        {
            this.BookmarkReached.Write($"{prefix}.{nameof(this.BookmarkReached)}", store);
            this.PhonemeReached.Write($"{prefix}.{nameof(this.PhonemeReached)}", store);
            this.SpeakCompleted.Write($"{prefix}.{nameof(this.SpeakCompleted)}", store);
            this.SpeakProgress.Write($"{prefix}.{nameof(this.SpeakProgress)}", store);
            this.SpeakStarted.Write($"{prefix}.{nameof(this.SpeakStarted)}", store);
            this.StateChanged.Write($"{prefix}.{nameof(this.StateChanged)}", store);
            this.VisemeReached.Write($"{prefix}.{nameof(this.VisemeReached)}", store);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.speechSynthesizer != null)
            {
                this.speechSynthesizer.Dispose();
                this.speechSynthesizer = null;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            // Unregister handlers so they won't fire while disposing.
            this.speechSynthesizer.BookmarkReached -= this.OnBookmarkReached;
            this.speechSynthesizer.PhonemeReached -= this.OnPhonemeReached;
            this.speechSynthesizer.SpeakCompleted -= this.OnSpeakCompleted;
            this.speechSynthesizer.SpeakProgress -= this.OnSpeakProgress;
            this.speechSynthesizer.SpeakStarted -= this.OnSpeakStarted;
            this.speechSynthesizer.StateChanged -= this.OnStateChanged;
            this.speechSynthesizer.VisemeReached -= this.OnVisemeReached;

            this.speechSynthesizer.SpeakAsyncCancelAll();
            notifyCompleted();
        }

        /// <inheritdoc/>
        protected override void Receive(string utteranceText, Envelope envelope)
        {
            // Construct the SSML for the text to speak.
            PromptBuilder pb = new PromptBuilder();
            pb.AppendSsmlMarkup(string.Format("<prosody rate=\"{0}\" pitch=\"{1}\" volume=\"{2}\">", this.configuration.ProsodyRate, this.configuration.ProsodyPitch, this.configuration.ProsodyVolume));
            pb.AppendText(utteranceText);
            pb.AppendSsmlMarkup("</prosody>");

            // Speak synchronously
            this.speechSynthesizer.SpeakAsync(pb);
        }

        private void ReceiveSpeakSsml(string speakSsml, Envelope envelope)
        {
            // Construct the prompt based on the utterance SSML
            var ssml = this.CreatePromptSsml(speakSsml);
            var prompt = new Prompt(ssml, SynthesisTextFormat.Ssml);

            // Speak synchronously
            this.speechSynthesizer.SpeakAsync(prompt);
        }

        private void ReceiveCancelAll(bool cancelAll, Envelope envelope)
        {
            this.speechSynthesizer.SpeakAsyncCancelAll();
        }

        private string CreatePromptSsml(string utteranceSsml)
        {
            utteranceSsml = utteranceSsml.Replace(Environment.NewLine, " ");

            int speakPrefixIndex = utteranceSsml.IndexOf(SpeakSsmlPrefix);
            int speakPostfixIndex = utteranceSsml.IndexOf(SpeakSsmlPostfix);

            // construct the adjusted ssml
            if (speakPrefixIndex != -1 && speakPostfixIndex != -1)
            {
                return string.Format(
                    "{0}{1}<prosody rate=\"{2}\" pitch=\"{3}\">{4}</prosody>{5}",
                    utteranceSsml.Substring(0, speakPrefixIndex),
                    SpeakSsmlPrefix,
                    this.configuration.ProsodyRate,
                    this.configuration.ProsodyPitch,
                    utteranceSsml.Substring(speakPrefixIndex + SpeakSsmlPrefix.Length, speakPostfixIndex - speakPrefixIndex - SpeakSsmlPrefix.Length),
                    utteranceSsml.Substring(speakPostfixIndex));
            }
            else if (speakPrefixIndex == -1 && speakPostfixIndex == -1)
            {
                return $"{SpeakSsmlPrefix}<prosody rate=\"{this.configuration.ProsodyRate}\" pitch=\"{this.configuration.ProsodyPitch}\">{utteranceSsml}</prosody>{SpeakSsmlPostfix}";
            }
            else
            {
                throw new InvalidDataException("The provided SSML is not in a valid format.");
            }
        }

        private SpeechSynthesizer CreateSpeechSynthesizer()
        {
            var synthesizer = new SpeechSynthesizer();
            if (!string.IsNullOrEmpty(this.Configuration.Voice))
            {
                synthesizer.SelectVoice(this.Configuration.Voice);
            }

            if (this.Configuration.UseDefaultAudioPlaybackDevice)
            {
                // If specified, don't create an output stream and just set up the
                // synthesizer to play sound directly to the default audio device.
                synthesizer.SetOutputToDefaultAudioDevice();
            }
            else
            {
                // Create the format info from the configuration input format
                SpeechAudioFormatInfo formatInfo = new SpeechAudioFormatInfo(
                    (EncodingFormat)this.Configuration.OutputFormat.FormatTag,
                    (int)this.Configuration.OutputFormat.SamplesPerSec,
                    this.Configuration.OutputFormat.BitsPerSample,
                    this.Configuration.OutputFormat.Channels,
                    (int)this.Configuration.OutputFormat.AvgBytesPerSec,
                    this.Configuration.OutputFormat.BlockAlign,
                    (this.Configuration.OutputFormat is WaveFormatEx) ? ((WaveFormatEx)this.Configuration.OutputFormat).ExtraInfo : null);

                // Configure synthesizer to write to the output stream
                synthesizer.SetOutputToAudioStream(
                    new IOStream(
                        (buffer, offset, count) =>
                        {
                            byte[] audioData = buffer;
                            if (buffer.Length != count)
                            {
                                audioData = new byte[count];
                                Array.Copy(buffer, offset, audioData, 0, count);
                            }

                            this.Out.Post(new AudioBuffer(audioData, this.Configuration.OutputFormat), this.pipeline.GetCurrentTime());
                        }),
                    formatInfo);
            }

            // Register all handlers
            synthesizer.BookmarkReached += this.OnBookmarkReached;
            synthesizer.PhonemeReached += this.OnPhonemeReached;
            synthesizer.SpeakCompleted += this.OnSpeakCompleted;
            synthesizer.SpeakProgress += this.OnSpeakProgress;
            synthesizer.SpeakStarted += this.OnSpeakStarted;
            synthesizer.StateChanged += this.OnStateChanged;
            synthesizer.VisemeReached += this.OnVisemeReached;

            return synthesizer;
        }

        private void OnBookmarkReached(object sender, BookmarkReachedEventArgs e)
        {
            var time = this.pipeline.GetCurrentTime();
            this.BookmarkReached.Post(new BookmarkReachedEventData(e), time);
        }

        private void OnPhonemeReached(object sender, PhonemeReachedEventArgs e)
        {
            var time = this.pipeline.GetCurrentTime();
            this.PhonemeReached.Post(new PhonemeReachedEventData(e), time);
        }

        private void OnSpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            var time = this.pipeline.GetCurrentTime();
            this.SpeakCompleted.Post(new SpeakCompletedEventData(), time);
        }

        private void OnSpeakProgress(object sender, SpeakProgressEventArgs e)
        {
            var time = this.pipeline.GetCurrentTime();
            this.SpeakProgress.Post(new SpeakProgressEventData(e), time);
        }

        private void OnSpeakStarted(object sender, SpeakStartedEventArgs e)
        {
            var time = this.pipeline.GetCurrentTime();
            this.SpeakStarted.Post(new SpeakStartedEventData(), time);
        }

        private void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            var time = this.pipeline.GetCurrentTime();
            this.StateChanged.Post(new StateChangedEventData(e), time);
        }

        private void OnVisemeReached(object sender, VisemeReachedEventArgs e)
        {
            var time = this.pipeline.GetCurrentTime();
            this.VisemeReached.Post(new VisemeReachedEventData(e), time);
        }

        /// <summary>
        /// Represents data from the System.Speech.Synthesis.SpeechSynthesizer.BookmarkReached event.
        /// </summary>
        public class BookmarkReachedEventData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="BookmarkReachedEventData"/> class.
            /// </summary>
            /// <param name="e">
            /// The event args from the System.Speech.Synthesis.SpeechSynthesizer.BookmarkReached event.
            /// </param>
            public BookmarkReachedEventData(BookmarkReachedEventArgs e)
            {
                this.Bookmark = e.Bookmark;
                this.AudioPosition = e.AudioPosition;
            }

            /// <summary>
            /// Gets the name of the bookmark that was reached.
            /// </summary>
            public string Bookmark { get; }

            /// <summary>
            /// Gets the time offset at which the bookmark was reached.
            /// </summary>
            public TimeSpan AudioPosition { get; }
        }

        /// <summary>
        /// Represents data from the System.Speech.Synthesis.SpeechSynthesizer.PhonemeReached event.
        /// </summary>
        public class PhonemeReachedEventData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PhonemeReachedEventData"/> class.
            /// </summary>
            /// <param name="e">
            /// The event args from the System.Speech.Synthesis.SpeechSynthesizer.PhonemeReached event.
            /// </param>
            public PhonemeReachedEventData(PhonemeReachedEventArgs e)
            {
                this.Phoneme = e.Phoneme;
                this.AudioPosition = e.AudioPosition;
                this.Duration = e.Duration;
                this.Emphasis = e.Emphasis;
                this.NextPhoneme = e.NextPhoneme;
            }

            /// <summary>
            /// Gets the phoneme associated with the System.Speech.Synthesis.SpeechSynthesizer.PhonemeReached event.
            /// </summary>
            public string Phoneme { get; }

            /// <summary>
            /// Gets the audio position of the phoneme.
            /// </summary>
            public TimeSpan AudioPosition { get; }

            /// <summary>
            /// Gets the duration of the phoneme.
            /// </summary>
            public TimeSpan Duration { get; }

            /// <summary>
            /// Gets the emphasis of the phoneme.
            /// </summary>
            public SynthesizerEmphasis Emphasis { get; }

            /// <summary>
            /// Gets the phoneme following the phoneme associated with the
            /// System.Speech.Synthesis.SpeechSynthesizer.PhonemeReached event.
            /// </summary>
            public string NextPhoneme { get; }
        }

        /// <summary>
        /// Represents data from the System.Speech.Synthesis.SpeechSynthesizer.SpeakCompleted event.
        /// </summary>
        public class SpeakCompletedEventData
        {
        }

        /// <summary>
        /// Represents data from the System.Speech.Synthesis.SpeechSynthesizer.SpeakProgress event.
        /// </summary>
        public class SpeakProgressEventData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SpeakProgressEventData"/> class.
            /// </summary>
            /// <param name="e">
            /// The event args from the System.Speech.Synthesis.SpeechSynthesizer.SpeakProgress event.
            /// </param>
            public SpeakProgressEventData(SpeakProgressEventArgs e)
            {
                this.AudioPosition = e.AudioPosition;
                this.CharacterPosition = e.CharacterPosition;
                this.CharacterCount = e.CharacterCount;
                this.Text = e.Text;
            }

            /// <summary>
            /// Gets the audio position of the event.
            /// </summary>
            public TimeSpan AudioPosition { get; }

            /// <summary>
            /// Gets the number of characters and spaces from the beginning of the prompt to
            /// the position before the first letter of the word that was just spoken.
            /// </summary>
            public int CharacterPosition { get; }

            /// <summary>
            /// Gets the number of characters in the word that was spoken just before the event
            /// was raised.
            /// </summary>
            public int CharacterCount { get; }

            /// <summary>
            /// Gets the text that was just spoken when the event was raised.
            /// </summary>
            public string Text { get; }
        }

        /// <summary>
        /// Represents data from the System.Speech.Synthesis.SpeechSynthesizer.SpeakStarted event.
        /// </summary>
        public class SpeakStartedEventData
        {
        }

        /// <summary>
        /// Represents data from the System.Speech.Synthesis.SpeechSynthesizer.StateChanged event.
        /// </summary>
        public class StateChangedEventData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="StateChangedEventData"/> class.
            /// </summary>
            /// <param name="e">
            /// The event args from the System.Speech.Synthesis.SpeechSynthesizer.StateChanged event.
            /// </param>
            public StateChangedEventData(StateChangedEventArgs e)
            {
                this.State = e.State;
                this.PreviousState = e.PreviousState;
            }

            /// <summary>
            /// Gets the state of the System.Speech.Synthesis.SpeechSynthesizer after the
            /// System.Speech.Synthesis.SpeechSynthesizer.StateChanged event.
            /// </summary>
            public SynthesizerState State { get; }

            /// <summary>
            /// Gets the state of the System.Speech.Synthesis.SpeechSynthesizer before the
            /// System.Speech.Synthesis.SpeechSynthesizer.StateChanged event.
            /// </summary>
            public SynthesizerState PreviousState { get; }
        }

        /// <summary>
        /// Represents data from the System.Speech.Synthesis.SpeechSynthesizer.VisemeReached event.
        /// </summary>
        public class VisemeReachedEventData
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="VisemeReachedEventData"/> class.
            /// </summary>
            /// <param name="e">
            /// The event args from the System.Speech.Synthesis.SpeechSynthesizer.VisemeReached event.
            /// </param>
            public VisemeReachedEventData(VisemeReachedEventArgs e)
            {
                this.Viseme = e.Viseme;
                this.AudioPosition = e.AudioPosition;
                this.Duration = e.Duration;
                this.Emphasis = e.Emphasis;
                this.NextViseme = e.NextViseme;
            }

            /// <summary>
            /// Gets the value of the viseme.
            /// </summary>
            public int Viseme { get; }

            /// <summary>
            /// Gets the position of the viseme in the audio stream.
            /// </summary>
            public TimeSpan AudioPosition { get; }

            /// <summary>
            /// Gets the duration of the viseme.
            /// </summary>
            public TimeSpan Duration { get; }

            /// <summary>
            /// Gets a System.Speech.Synthesis.SynthesizerEmphasis object that describes the
            /// emphasis of the viseme.
            /// </summary>
            public SynthesizerEmphasis Emphasis { get; }

            /// <summary>
            /// Gets the value of the next viseme.
            /// </summary>
            public int NextViseme { get; }
        }

        #region Synthesizer output stream

        /// <summary>
        /// A <see cref="System.IO.Stream"/> adapter that takes a write delegate.
        /// </summary>
        private class IOStream : System.IO.Stream
        {
            private readonly Action<byte[], int, int> write;

            public IOStream(Action<byte[], int, int> write)
            {
                this.write = write;
            }

            public override bool CanRead
            {
                get { return false; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override long Length
            {
                get { return 0; }
            }

            public override long Position
            {
                get { return 0; } set { }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                this.write(buffer, offset, count);
            }
        }

        #endregion
    }
}

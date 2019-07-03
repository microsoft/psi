// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Speech.Recognition;
    using System.Speech.Recognition.SrgsGrammar;
    using System.Xml;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Language;

    /// <summary>
    /// Component that performs grammar-based intent detection using the desktop speech recognition engine from `System.Speech`.
    /// </summary>
    public sealed class SystemSpeechIntentDetector : ConsumerProducer<string, IntentData>, ISourceComponent, IDisposable
    {
        /// <summary>
        /// The System.Speech speech recognition engine.
        /// </summary>
        private readonly SpeechRecognitionEngine speechRecognitionEngine;

        /// <summary>
        /// The pipeline the component belongs to.
        /// </summary>
        private readonly Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSpeechIntentDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public SystemSpeechIntentDetector(Pipeline pipeline, SystemSpeechIntentDetectorConfiguration configuration)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.Configuration = configuration ?? new SystemSpeechIntentDetectorConfiguration();

            // create the recognition engine
            this.speechRecognitionEngine = this.CreateSpeechRecognitionEngine();

            // create receivers and emitters
            this.ReceiveGrammars = pipeline.CreateReceiver<IEnumerable<string>>(this, this.SetGrammars, nameof(this.ReceiveGrammars), true);
            this.LoadGrammarCompleted = pipeline.CreateEmitter<LoadGrammarCompletedEventArgs>(this, nameof(this.LoadGrammarCompleted));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemSpeechIntentDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public SystemSpeechIntentDetector(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new SystemSpeechIntentDetectorConfiguration() : new ConfigurationHelper<SystemSpeechIntentDetectorConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Gets the receiver for new grammars.
        /// </summary>
        public Receiver<IEnumerable<string>> ReceiveGrammars { get; }

        /// <summary>
        /// Gets the output stream of load grammar completed events.
        /// </summary>
        public Emitter<LoadGrammarCompletedEventArgs> LoadGrammarCompleted { get; }

        /// <summary>
        /// Gets the configuration for this component.
        /// </summary>
        private SystemSpeechIntentDetectorConfiguration Configuration { get; }

        /// <summary>
        /// Replace grammars with given.
        /// </summary>
        /// <param name="srgsXmlGrammars">A collection of XML-format speech grammars that conform to the SRGS 1.0 specification.</param>
        public void SetGrammars(Message<IEnumerable<string>> srgsXmlGrammars)
        {
            this.speechRecognitionEngine.RequestRecognizerUpdate(srgsXmlGrammars.Data.Select(g =>
            {
                using (var xmlReader = XmlReader.Create(new StringReader(g)))
                {
                    return new Grammar(new SrgsDocument(xmlReader));
                }
            }));
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        public void Dispose()
        {
            if (this.speechRecognitionEngine != null)
            {
                // Unregister handlers so they won't fire while disposing.
                this.speechRecognitionEngine.LoadGrammarCompleted -= this.OnLoadGrammarCompleted;
            }
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            // notify that this is an infinite source component
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            notifyCompleted();
        }

        /// <inheritdoc/>
        protected override void Receive(string text, Envelope e)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                this.Out.Post(new IntentData(), e.OriginatingTime);
            }
            else
            {
                var result = this.speechRecognitionEngine.EmulateRecognize(text);
                if (result != null && result.Semantics != null)
                {
                    var intents = SystemSpeech.BuildIntentData(result.Semantics);
                    this.Out.Post(intents, e.OriginatingTime);
                }
                else
                {
                    this.Out.Post(new IntentData(), e.OriginatingTime);
                }
            }
        }

        private SpeechRecognitionEngine CreateSpeechRecognitionEngine()
        {
            // Create the recognizer
            var recognizer = SystemSpeech.CreateSpeechRecognitionEngine(this.Configuration.Language, this.Configuration.Grammars);

            // Attach event handlers for speech recognition events
            recognizer.LoadGrammarCompleted += this.OnLoadGrammarCompleted;

            return recognizer;
        }

        private void OnLoadGrammarCompleted(object sender, LoadGrammarCompletedEventArgs e)
        {
            this.LoadGrammarCompleted.Post(e, this.pipeline.GetCurrentTime());
        }
    }
}

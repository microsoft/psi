// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MicrosoftSpeech
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Language;
    using Microsoft.Speech.Recognition;
    using Microsoft.Speech.Recognition.SrgsGrammar;

    /// <summary>
    /// Component that performs grammar-based intent detection using the speech recognition engine from the Microsoft Speech Platform SDK.
    /// </summary>
    /// <remarks>
    /// Separate download and installation of the Microsoft Speech Platform runtime and language pack are required in order to use this component.
    /// - Click <a href="http://go.microsoft.com/fwlink/?LinkID=223568">here</a> to download the Microsoft Speech Platform runtime.
    /// - Click <a href="http://go.microsoft.com/fwlink/?LinkID=223569">here</a> to download the Microsoft Speech Platform language pack.
    /// </remarks>
    public sealed class MicrosoftSpeechIntentDetector : ConsumerProducer<string, IntentData>, ISourceComponent, IDisposable
    {
        /// <summary>
        /// The pipeline the component belongs to.
        /// </summary>
        private readonly Pipeline pipeline;

        /// <summary>
        /// The Microsoft.Speech speech recognition engine.
        /// </summary>
        private SpeechRecognitionEngine speechRecognitionEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftSpeechIntentDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public MicrosoftSpeechIntentDetector(Pipeline pipeline, MicrosoftSpeechIntentDetectorConfiguration configuration)
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.Configuration = configuration;

            // create receiver of grammar updates
            this.ReceiveGrammars = pipeline.CreateReceiver<IEnumerable<string>>(this, this.SetGrammars, nameof(this.ReceiveGrammars), true);

            // create receiver of grammar updates by name
            this.ReceiveGrammarNames = pipeline.CreateReceiver<string[]>(this, this.EnableGrammars, nameof(this.ReceiveGrammarNames), true);

            // create output streams for all the event args
            this.LoadGrammarCompleted = pipeline.CreateEmitter<LoadGrammarCompletedEventArgs>(this, nameof(LoadGrammarCompletedEventArgs));

            // create the recognition engine
            this.speechRecognitionEngine = this.CreateSpeechRecognitionEngine();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftSpeechIntentDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The Psi pipeline.</param>
        /// <param name="configurationFilename">The name of the configuration file.</param>
        public MicrosoftSpeechIntentDetector(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new MicrosoftSpeechIntentDetectorConfiguration() : new ConfigurationHelper<MicrosoftSpeechIntentDetectorConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Gets the receiver for new grammars.
        /// </summary>
        public Receiver<IEnumerable<string>> ReceiveGrammars { get; }

        /// <summary>
        /// Gets the receiver for new grammars (by name).
        /// </summary>
        public Receiver<string[]> ReceiveGrammarNames { get; }

        /// <summary>
        /// Gets the output stream of load grammar completed events.
        /// </summary>
        public Emitter<LoadGrammarCompletedEventArgs> LoadGrammarCompleted { get; }

        /// <summary>
        /// Gets the configuration for this component.
        /// </summary>
        private MicrosoftSpeechIntentDetectorConfiguration Configuration { get; }

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
        /// Enable all the grammars indicated by name, disabling all others.
        /// </summary>
        /// <param name="grammarNames">Speech grammars.</param>
        public void EnableGrammars(Message<string[]> grammarNames)
        {
            foreach (var g in this.speechRecognitionEngine.Grammars)
            {
                g.Enabled = grammarNames.Data.Contains(g.Name) ? true : false;
            }
        }

        /// <summary>
        /// Disable all of the grammars loaded on this recognition engine.
        /// </summary>
        public void DisableAllGrammars()
        {
            foreach (var g in this.speechRecognitionEngine.Grammars)
            {
                g.Enabled = false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.speechRecognitionEngine != null)
            {
                // Unregister handlers so they won't fire while disposing.
                this.speechRecognitionEngine.LoadGrammarCompleted -= this.OnLoadGrammarCompleted;

                // NOTE: The following Dispose() method will block execution of disposing the SpeechRecognitionEngine object until
                // either all pending event handlers have returned or until 30 seconds have elapsed, whichever occurs first.
                // Be aware that if you call Dispose() in an event handler, that this will create a circular wait for the full 30 seconds,
                // during which execution of the method will be blocked. Typically, calling Dispose() in an event handler is not advised.
                // (From): https://msdn.microsoft.com/en-us/library/office/dd146960(v=office.14).aspx
                this.speechRecognitionEngine.Dispose();
                this.speechRecognitionEngine = null;
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
                    var intents = MicrosoftSpeech.BuildIntentData(result.Semantics);
                    this.Out.Post(intents, e.OriginatingTime);
                }
            }
        }

        /// <summary>
        /// Creates a new speech recognition engine.
        /// </summary>
        /// <returns>A new speech recognition engine object.</returns>
        private SpeechRecognitionEngine CreateSpeechRecognitionEngine()
        {
            // Create the speech recognition engine
            var recognizer = MicrosoftSpeech.CreateSpeechRecognitionEngine(this.Configuration.Language, this.Configuration.Grammars);

            // Attach the event handlers for speech recognition events
            recognizer.LoadGrammarCompleted += this.OnLoadGrammarCompleted;

            return recognizer;
        }

        private void OnLoadGrammarCompleted(object sender, LoadGrammarCompletedEventArgs e)
        {
            this.LoadGrammarCompleted.Post(e, this.pipeline.GetCurrentTime());
        }
    }
}


namespace CMU.Smartlab.Audio
{
    using System;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Audio;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that wraps the Azure Cognitive Services speech recognizer.
    /// </summary>
    public class ContinuousSpeechRecognizer : ConsumerProducer<AudioBuffer, string>, ISourceComponent, IDisposable
    {
        private readonly PushAudioInputStream pushStream;
        private readonly AudioConfig audioInput;
        private readonly SpeechRecognizer recognizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuousSpeechRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline in which to create the component.</param>
        /// <param name="subscriptionKey">The subscription key for the Azure speech resource.</param>
        /// <param name="region">The service region of the Azure speech resource.</param>
        public ContinuousSpeechRecognizer(Pipeline pipeline, string subscriptionKey, string region)
            : base(pipeline)
        {
            var config = SpeechConfig.FromSubscription(subscriptionKey, region);
            this.pushStream = AudioInputStream.CreatePushStream();
            this.audioInput = AudioConfig.FromStreamInput(this.pushStream);
            this.recognizer = new SpeechRecognizer(config, this.audioInput);
        }

        /// <inheritdoc/>
        public void Start(Action<DateTime> notifyCompletionTime)
        {
            this.recognizer.Recognized += this.Recognizer_Recognized;
            this.recognizer.StartContinuousRecognitionAsync().Wait();
            notifyCompletionTime(DateTime.MaxValue);
        }

        /// <inheritdoc/>
        public void Stop(DateTime finalOriginatingTime, Action notifyCompleted)
        {
            this.recognizer.Recognized -= this.Recognizer_Recognized;
            this.pushStream.Close();
            this.recognizer.StopContinuousRecognitionAsync().Wait();
            notifyCompleted();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.recognizer.Dispose();
            this.audioInput.Dispose();
            this.pushStream.Dispose();
        }

        /// <inheritdoc/>
        protected override void Receive(AudioBuffer data, Envelope envelope)
        {
            this.pushStream.Write(data.Data);
        }

        /// <summary>
        /// Handler for the speech recognized event from the recognizer. Posts the recognized text to the output.
        /// </summary>
        private void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            var originatingTime = this.Out.Pipeline.StartTime.AddTicks((long)e.Offset) + e.Result.Duration;
            this.Out.Post(e.Result.Text, originatingTime);
        }
    }
}
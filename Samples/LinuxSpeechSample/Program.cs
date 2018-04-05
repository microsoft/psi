// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Samples.LinuxSpeechSample
{
    using System;
    using System.Linq;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Speech;

    public static class Program
    {
        // This field is required and must be a valid key which may be obtained by signing up at
        // https://azure.microsoft.com/en-us/try/cognitive-services/?api=speech-api.
        private static string bingSubscriptionKey = string.Empty;

        public static void Main(string[] args)
        {
            // Flag to exit the application
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("================================================================================");
                Console.WriteLine("                               Psi Speech Sample");
                Console.WriteLine("================================================================================");
                Console.WriteLine("1) Speech-To-Text using Bing speech recognizer");
                Console.WriteLine("Q) QUIT");
                Console.Write("Enter selection: ");
                ConsoleKey key = Console.ReadKey().Key;
                Console.WriteLine();

                exit = false;
                switch (key)
                {
                    case ConsoleKey.D1:
                        // Bing speech service requires a valid subscription key
                        if (GetSubscriptionKey())
                        {
                            // Demonstrate the use of the BingSpeechRecognizer component
                            RunBingSpeech();
                        }

                        break;

                    case ConsoleKey.Q:
                        exit = true;
                        break;

                    default:
                        exit = false;
                        break;
                }
            }
        }

        /// <summary>
        /// Builds and runs a speech recognition pipeline using the Bing speech recognizer. Requires a valid Cognitive Services
        /// subscription key. See https://docs.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account.
        /// </summary>
        /// <remarks>
        /// If you are getting a <see cref="System.InvalidOperationException"/> with the message 'BingSpeechRecognizer returned
        /// OnConversationError with error code: LoginFailed. Original error text: Transport error', this most likely is due to
        /// an invalid subscription key. Please check your Azure portal at https://portal.azure.com and ensure that you have
        /// added a subscription to the Bing Speech API on your account.
        /// </remarks>
        public static void RunBingSpeech()
        {
            // Create the pipeline object.
            using (Pipeline pipeline = Pipeline.Create())
            {
                // Create the AudioSource component to capture audio from the default device in 16 kHz 1-channel
                // PCM format as required by both the voice activity detector and speech recognition components.
                IProducer<AudioBuffer> audioInput = new AudioSource(pipeline, new AudioConfiguration() { DeviceName = "plughw:0,0", Format = WaveFormat.Create16kHz1Channel16BitPcm() });

                // Perform voice activity detection using the voice activity detector component
                var vad = new SimpleVoiceActivityDetector(pipeline);
                audioInput.PipeTo(vad);

                // Create Bing speech recognizer component
                var recognizer = new BingSpeechRecognizer(pipeline, new BingSpeechRecognizerConfiguration() { SubscriptionKey = Program.bingSubscriptionKey, RecognitionMode = SpeechRecognitionMode.Interactive });

                // The input audio to the Bing speech recognizer needs to be annotated with a voice activity flag.
                // This can be constructed by using the Psi Join() operator to combine the audio and VAD streams.
                var annotatedAudio = audioInput.Join(vad);

                // Subscribe the recognizer to the annotated audio
                annotatedAudio.PipeTo(recognizer);

                // Partial and final speech recognition results are posted on the same stream. Here
                // we use Psi's Where() operator to filter out only the final recognition results.
                var finalResults = recognizer.Out.Where(result => result.IsFinal);

                // Print the recognized text of the final recognition result to the console.
                finalResults.Do(result => Console.WriteLine(result.Text));

                // Register an event handler to catch pipeline errors
                pipeline.PipelineCompletionEvent += PipelineCompletionEvent;

                // Run the pipeline
                pipeline.RunAsync();

                // Bing speech transcribes speech to text
                Console.WriteLine("Say anything");

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// Event handler for the PipelineCompletion event.
        /// </summary>
        /// <param name="sender">The sender which raised the event.</param>
        /// <param name="e">The pipeline completion event arguments.</param>
        private static void PipelineCompletionEvent(object sender, PipelineCompletionEventArgs e)
        {
            Console.WriteLine("Pipeline execution completed with {0} errors", e.Errors.Count);

            // Prints all exceptions that were thrown by the pipeline
            if (e.Errors.Count > 0)
            {
                foreach (var error in e.Errors)
                {
                    Console.WriteLine(error);
                }
            }
        }

        /// <summary>
        /// Prompt user to enter Bing Speech subscription key from Cognitive Services. Or just set the BingSubscriptionKey
        /// static member at the top of this file to avoid having to enter it each time. For more information on how to
        /// register for a subscription, see https://www.microsoft.com/cognitive-services/en-us/sign-up
        /// </summary>
        /// <returns>
        /// True if <see cref="bingSubscriptionKey"/> contains a non-empty key (the key will not actually be
        /// authenticated until the first attempt to access the speech recognition service). False otherwise.
        /// </returns>
        private static bool GetSubscriptionKey()
        {
            Console.WriteLine("A cognitive services Bing Speech subscription key is required to use this. For more info, see 'https://docs.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account'");
            Console.Write("Enter subscription key");
            Console.Write(string.IsNullOrWhiteSpace(Program.bingSubscriptionKey) ? ": " : string.Format(" (current = {0}): ", Program.bingSubscriptionKey));

            // Read a new key or hit enter to keep using the current one (if any)
            string response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                Program.bingSubscriptionKey = response;
            }

            return !string.IsNullOrWhiteSpace(Program.bingSubscriptionKey);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Samples.LinuxSpeechSample
{
    using System;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Speech sample on Linux sample program.
    /// </summary>
    public static class Program
    {
        // A valid subscription key is required which may be obtained by signing up at
        // https://azure.microsoft.com/en-us/services/cognitive-services.
        private static string azureSubscriptionKey = string.Empty;
        private static string azureRegion = string.Empty; // the service region of the speech resource (e.g. "westus")
        private static string deviceName = "plughw:0,0";

        /// <summary>
        /// Main entry point.
        /// </summary>
        public static void Main()
        {
            // Flag to exit the application
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("================================================================================");
                Console.WriteLine("                               Psi Speech Sample");
                Console.WriteLine("================================================================================");
                Console.WriteLine("1) Speech-To-Text using Azure speech recognizer");
                Console.WriteLine("Q) QUIT");
                Console.Write("Enter selection: ");
                ConsoleKey key = Console.ReadKey().Key;
                Console.WriteLine();

                exit = false;
                switch (key)
                {
                    case ConsoleKey.D1:
                        if (GetSubscriptionKeyAndRegion() && GetAudioDeviceName())
                        {
                            RunAzureSpeech();
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
        /// Builds and runs a speech recognition pipeline using the Azure speech service. Requires a valid Cognitive Services
        /// subscription key. See https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started.
        /// </summary>
        public static void RunAzureSpeech()
        {
            // Create the pipeline object.
            using (Pipeline pipeline = Pipeline.Create())
            {
                // Create the AudioSource component to capture audio from the default device in 16 kHz 1-channel
                // PCM format as required by the speech recognition component.
                var audio = new AudioCapture(pipeline, new AudioCaptureConfiguration { DeviceName = deviceName, Format = WaveFormat.Create16kHz1Channel16BitPcm() });

                // Create the speech recognizer component
                var recognizer = new ContinuousSpeechRecognizer(pipeline, azureSubscriptionKey, azureRegion);

                // Subscribe the recognizer to the annotated audio
                audio.PipeTo(recognizer);

                // Print the recognized text of the final recognition result to the console.
                recognizer.Out.Do((result, e) => Console.WriteLine($"{e.OriginatingTime.TimeOfDay}: {result}"));

                // Register an event handler to catch pipeline errors
                pipeline.PipelineExceptionNotHandled += Pipeline_PipelineException;

                // Register an event handler to be notified when the pipeline completes
                pipeline.PipelineCompleted += Pipeline_PipelineCompleted;

                // Run the pipeline
                pipeline.RunAsync();

                // Azure speech transcribes speech to text
                Console.WriteLine("Say anything");

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        /// <summary>
        /// Event handler for the <see cref="Pipeline.PipelineCompleted"/> event.
        /// </summary>
        /// <param name="sender">The sender which raised the event.</param>
        /// <param name="e">The pipeline completion event arguments.</param>
        private static void Pipeline_PipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            Console.WriteLine("Pipeline execution completed with {0} errors", e.Errors.Count);
        }

        /// <summary>
        /// Event handler for the <see cref="Pipeline.PipelineExceptionNotHandled"/> event.
        /// </summary>
        /// <param name="sender">The sender which raised the event.</param>
        /// <param name="e">The pipeline exception event arguments.</param>
        private static void Pipeline_PipelineException(object sender, PipelineExceptionNotHandledEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }

        /// <summary>
        /// Prompt user to enter Azure Speech subscription key from Cognitive Services. Or just set the <see cref="azureSubscriptionKey"/>
        /// static member at the top of this file to avoid having to enter it each time. For more information on how to
        /// sign up for a subscription, see https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started.
        /// </summary>
        /// <returns>
        /// True if <see cref="azureSubscriptionKey"/> contains a non-empty key (the key will not actually be
        /// authenticated until the first attempt to access the speech recognition service). False otherwise.
        /// </returns>
        private static bool GetSubscriptionKeyAndRegion()
        {
            Console.WriteLine("A Cognitive Services Speech subscription key is required to use this. For more info, see 'https://docs.microsoft.com/en-us/azure/cognitive-services/speech-service/get-started'");
            Console.Write("Enter subscription key");
            Console.Write(string.IsNullOrWhiteSpace(azureSubscriptionKey) ? ": " : string.Format(" (current = {0}): ", azureSubscriptionKey));

            // Read a new key or hit enter to keep using the current one (if any)
            string response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                azureSubscriptionKey = response;
            }

            Console.Write("Enter region");
            Console.Write(string.IsNullOrWhiteSpace(azureRegion) ? ": " : string.Format(" (current = {0}): ", azureRegion));

            // Read a new key or hit enter to keep using the current one (if any)
            response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                azureRegion = response;
            }

            return !string.IsNullOrWhiteSpace(azureSubscriptionKey) && !string.IsNullOrWhiteSpace(azureRegion);
        }

        /// <summary>
        /// Prompt user to enter the audio capture device name.
        /// </summary>
        /// <returns>True if <see cref="deviceName"/> contains a non-empty string. False otherwise.</returns>
        private static bool GetAudioDeviceName()
        {
            // Get the Device Name to record audio from
            Console.Write("Enter Device Name");
            Console.Write(string.IsNullOrWhiteSpace(deviceName) ? ": " : string.Format(" (current = {0}): ", deviceName));

            // Read a new key or hit enter to keep using the current one (if any)
            string response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                deviceName = response;
            }

            return !string.IsNullOrWhiteSpace(deviceName);
        }
    }
}

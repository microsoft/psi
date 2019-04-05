// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Represents the configuration for the <see cref="AzureSpeechRecognizer"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="AzureSpeechRecognizer"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class AzureSpeechRecognizerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSpeechRecognizerConfiguration"/> class.
        /// </summary>
        public AzureSpeechRecognizerConfiguration()
        {
            this.Language = "en-us";
            this.SubscriptionKey = null; // This must be set to the key associated with your account
            this.Region = null; // This must be set to the region associated to the key

            // Defaults to 16 kHz, 16-bit, 1-channel PCM samples
            this.InputFormat = WaveFormat.Create16kHz1Channel16BitPcm();
        }

        /// <summary>
        /// Gets or sets the speech recognition language.
        /// </summary>
        /// <remarks>
        /// Use this to set the locale for the speech recognition service. If not specified,
        /// this defaults to "en-us" (U.S. English). Other supported locales include "en-gb",
        /// "de-de", "es-es", "fr-fr", "it-it" and "zh-cn".
        /// </remarks>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the subscription key.
        /// </summary>
        /// <remarks>
        /// This component uses the Azure Speech API to perform speech recognition. A Cognitive Services
        /// subscription is required in order to use this service. The <see cref="AzureSpeechRecognizer"/>
        /// authenticates with the service using a subscription key associated with the subcription. See
        /// https://azure.microsoft.com/en-us/try/cognitive-services/ for more information on obtaining a
        /// subscription.
        /// </remarks>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets the expected input format of the audio stream.
        /// </summary>
        /// <remarks>
        /// Currently, the only supported input audio format is 16000 Hz, 1-channel, 16-bit PCM.
        /// </remarks>
        public WaveFormat InputFormat { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <remarks>
        /// This is the region that is associated to the subscription key.
        /// </remarks>
        public string Region { get; set; }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    using System;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Represents the configuration for the <see cref="BingSpeechRecognizer"/> component.
    /// </summary>
    /// <remarks>
    /// DEPRECATED - As the Bing Speech service will be retired soon, you can no longer
    /// obtain a new subscription key for this service. If you have previously obtained a subscription
    /// key for the Bing Speech service, then this key should continue to work with this component
    /// until the service is retired. If you do not have an existing subscription
    /// key for the Bing Speech service, please use the <see cref="AzureSpeechRecognizer"/> component
    /// instead. You may obtain a subscription key for the Azure Speech service here:
    /// https://azure.microsoft.com/en-us/try/cognitive-services/?api=speech-services.
    ///
    /// Use this class to configure a new instance of the <see cref="BingSpeechRecognizer"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    [Obsolete("The Bing Speech service will be retired soon. Please use the AzureSpeechRecognizer instead.", false)]
    public sealed class BingSpeechRecognizerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BingSpeechRecognizerConfiguration"/> class.
        /// </summary>
        public BingSpeechRecognizerConfiguration()
        {
            this.Language = "en-us";
            this.RecognitionMode = SpeechRecognitionMode.Interactive;
            this.SubscriptionKey = null; // This must be set to the key associated with your account

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
        /// Gets or sets the speech recognition mode.
        /// </summary>
        /// <remarks>
        /// The speech recognition mode must be one of the values defined in the enumeration
        /// <see cref="SpeechRecognitionMode"/>. The default value is <see cref="SpeechRecognitionMode.Interactive"/>.
        /// </remarks>
        public SpeechRecognitionMode RecognitionMode { get; set; }

        /// <summary>
        /// Gets or sets the subscription key.
        /// </summary>
        /// <remarks>
        /// This component uses the Bing Speech API to perform speech recognition. A Cognitive Services
        /// subscription is required in order to use this service. The <see cref="BingSpeechRecognizer"/>
        /// authenticates with the service using a subscription key associated with the subcription.
        /// However, as the Bing Speech service will be retired soon, you can no longer
        /// obtain a new subscription key for this service. If you have previously obtained a subscription
        /// key for the Bing Speech service, then this key should continue to work with this component
        /// until the service is retired. If you do not have an existing subscription
        /// key for the Bing Speech service, please use the <see cref="AzureSpeechRecognizer"/> component
        /// instead. You may obtain a subscription key for the Azure Speech service here:
        /// https://azure.microsoft.com/en-us/try/cognitive-services/?api=speech-services.
        /// </remarks>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets the expected input format of the audio stream.
        /// </summary>
        /// <remarks>
        /// Currently, the only supported input audio format is 16000 Hz, 1-channel, 16-bit PCM.
        /// </remarks>
        public WaveFormat InputFormat { get; set; }
    }
}

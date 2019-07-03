// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Language
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Language;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Component that performs intent detection and entity extraction using the <a href="https://www.luis.ai/">Microsoft Cognitive Services LUIS API</a>.
    /// </summary>
    /// <remarks>
    /// The component takes in a stream of text-based phrases or utterances on its In stream and detects
    /// intents and extracts any entities contained therein, posting the results on its Out stream.
    /// It works in conjunction with the <a href="https://www.luis.ai/">Microsoft Cognitive Services LUIS API</a>
    /// and requires a subscription key in order to use. In addition, a LUIS application needs to be created in the service ahead of time, and the application id
    /// (App Id) should be specified in the component configuration. For more information, see the complete documentation for the
    /// <a href="https://www.luis.ai/help">Microsoft Cognitive Services LUIS API</a>.
    /// </remarks>
    public sealed class LUISIntentDetector : AsyncConsumerProducer<string, IntentData>
    {
        /// <summary>
        /// The configuration for this component.
        /// </summary>
        private readonly LUISIntentDetectorConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="LUISIntentDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public LUISIntentDetector(Pipeline pipeline, LUISIntentDetectorConfiguration configuration)
            : base(pipeline)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LUISIntentDetector"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configurationFilename">The component configuration file.</param>
        public LUISIntentDetector(Pipeline pipeline, string configurationFilename = null)
            : this(
                pipeline,
                (configurationFilename == null) ? new LUISIntentDetectorConfiguration() : new ConfigurationHelper<LUISIntentDetectorConfiguration>(configurationFilename).Configuration)
        {
        }

        /// <summary>
        /// Creates a new IntentData object from the corresponding LUIS JSON object.
        /// </summary>
        /// <param name="json">The JSON object.</param>
        /// <returns>The IntentData object.</returns>
        public static IntentData DeserializeIntentData(string json)
        {
            // Deserialize the JSON into an IntentData object.
            var token = JToken.Parse(json);
            return (token != null) ? ParseIntentData(token) : null;
        }

        /// <inheritdoc/>
        protected override async Task ReceiveAsync(string utterance, Envelope e)
        {
            string endpointUrl = string.Format(this.configuration.EndpointUrl, this.configuration.Region?.Replace(" ", string.Empty));
            using (HttpClient client = new HttpClient() { BaseAddress = new Uri(endpointUrl) })
            {
                // Fire off the request query asynchronously.
                HttpResponseMessage response = await client.GetAsync(
                    string.Format(
                        "{0}?verbose=true&subscription-key={1}&q={2}",
                        this.configuration.ApplicationId,
                        this.configuration.SubscriptionKey,
                        utterance));

                // Read the HTML into a string and start scraping.
                string json = await response.Content.ReadAsStringAsync();

                // Deserialize the JSON into an IntentData object.
                IntentData intentData = DeserializeIntentData(json);
                if (intentData != null)
                {
                    // originating time of utterance flows through to the detected intent data
                    this.Out.Post(intentData, e.OriginatingTime);
                }
            }
        }

        private static IntentData ParseIntentData(JToken token)
        {
            var queryString = ((string)token["query"]) ?? string.Empty;
            var intentsArray = ((JArray)token["intents"]) ?? new JArray();
            var entitiesArray = ((JArray)token["entities"]) ?? new JArray();

            return new IntentData()
            {
                Query = queryString,
                Intents = ParseIntentArray(intentsArray),
                Entities = ParseEntityArray(entitiesArray),
            };
        }

        private static Intent[] ParseIntentArray(JArray array)
        {
            var intents = new Intent[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                var token = array[i];
                intents[i] = new Intent()
                {
                    Value = (string)token["intent"],
                    Score = (double?)token["score"],
                };
            }

            return intents;
        }

        private static Entity[] ParseEntityArray(JArray array)
        {
            var entities = new Entity[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                var token = array[i];
                entities[i] = new Entity()
                {
                    Value = (string)token["entity"],
                    Type = (string)token["type"],
                    Score = (double?)token["score"],
                };
            }

            return entities;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Language
{
    using System.Linq;
    using Microsoft.Bot.Builder.PersonalityChat.Core;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that generates dialogue responses to textual inputs using the <a href="https://labs.cognitive.microsoft.com/en-us/project-personality-chat">Microsoft Cognitive Services Personality Chat API</a>.
    /// </summary>
    /// <remarks>
    /// The component takes in text-based phrases or utterances on its input stream and generates
    /// textual responses, posting the results on its output stream.
    /// It works in conjunction with the <a href="https://labs.cognitive.microsoft.com/en-us/project-personality-chat">Microsoft Cognitive Services Personality Chat API</a>
    /// and requires a subscription key in order to use. For more information, see the complete documentation for the
    /// <a href="https://labs.cognitive.microsoft.com/en-us/documentation">Microsoft Cognitive Services Personality Chat API</a>.
    /// </remarks>
    public sealed class PersonalityChat : ConsumerProducer<string, string>
    {
        /// <summary>
        /// The configuration for this component.
        /// </summary>
        private readonly PersonalityChatConfiguration configuration;
        private readonly PersonalityChatService personalityChatService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonalityChat"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration parameters.</param>
        public PersonalityChat(Pipeline pipeline, PersonalityChatConfiguration configuration)
            : base(pipeline)
        {
            this.configuration = configuration;
            var personalityChatOptions = new PersonalityChatOptions(configuration.PersonalityChatSubscriptionID, PersonalityChatPersona.Professional);
            this.personalityChatService = new PersonalityChatService(personalityChatOptions);
        }

        /// <summary>
        /// Gets the PersonalityChat response to a string of text.
        /// </summary>
        /// <param name="text">A string of text.</param>
        /// <param name="e">The message envelope for the received text.</param>
        protected override void Receive(string text, Envelope e)
        {
            string output = string.Empty;
            if (text.Length > 0)
            {
                var userStr = text;

                var personalityChatResults = this.personalityChatService.QueryServiceAsync(userStr).Result;
                output = personalityChatResults?.ScenarioList?.FirstOrDefault()?.Responses?.FirstOrDefault() ?? string.Empty;
            }

            this.Out.Post(output, e.OriginatingTime);
        }
    }
}

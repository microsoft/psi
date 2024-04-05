// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Azure;
    using Azure.AI.OpenAI;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Implements static helper methods for calling Azure OpenAI APIs.
    /// </summary>
    public sealed class AzureOpenAIService
    {
        /// <summary>
        /// The name for the Azure OpenAI service.
        /// </summary>
        public static readonly string Name = "AzureOpenAI";

        private static readonly ConcurrentDictionary<Guid, ChatCompletionsOptions> Conversations = new ();
        private static OpenAIClient openAIClient;

        /// <summary>
        /// Initialize and store an Azure-based OpenAI client for later prompting.
        /// </summary>
        /// <param name="endpointUri">The endpoint of the Azure OpenAI resource.</param>
        /// <param name="azureKeyCredential">The key for authentication.</param>
        public static void Initialize(Uri endpointUri, AzureKeyCredential azureKeyCredential)
        {
            openAIClient = new OpenAIClient(endpointUri, azureKeyCredential);
        }

        /// <summary>
        /// Gets the list of available deployments (models) from an Azure OpenAI endpoint.
        /// </summary>
        /// <param name="azureEndpoint">The endpoint for an Azure OpenAI resource as
        /// retrieved from, for example, Azure Portal. This should include protocol and hostname.
        /// An example could be: https://my-resource.openai.azure.com .
        /// </param>
        /// <param name="azureKey">The key for authentication.</param>
        /// <param name="azureAPIVersion">The version of the Azure API to use.</param>
        /// <returns>The set of available deployments (models).</returns>
        public static async Task<List<string>> GetAvailableDeploymentsAsync(
            Uri azureEndpoint,
            AzureKeyCredential azureKey,
            string azureAPIVersion = "2023-03-15-preview")
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-key", azureKey.Key);

            var response = await client.GetAsync($"{azureEndpoint}openai/deployments/?api-version={azureAPIVersion}");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Parse the response and populate the list of results
            var results = new List<string>();
            var jObject = JObject.Parse(responseContent);
            var deploymentIds = jObject["data"]?.Where(d => d["id"] != null).Select(d => (string)d["id"]);
            if (deploymentIds is not null)
            {
                results = deploymentIds.ToList();
                results.Sort();
            }

            return results;
        }

        /// <summary>
        /// Get a one-off completion of a user's prompt.
        /// </summary>
        /// <param name="userPrompt">The contents of the user's prompt.</param>
        /// <param name="deploymentName">The deployment (model) to use.</param>
        /// <param name="systemMessage">The system message (optional).</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2.
        /// Higher values like 0.8 will make the output more random, while lower values
        /// like 0.2 will make it more focused and deterministic.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="choiceCount">Number of choices to generate.</param>
        /// <param name="frequencyPenalty">Number between -2.0 and 2.0. Positive values penalize
        /// new tokens based on their existing frequency in the text so far, decreasing the model's
        /// likelihood to repeat the same line verbatim.</param>
        /// <param name="nucleusSamplingFactor">An alternative to sampling with temperature,
        /// where the model considers the results of the tokens with top_p probability mass.</param>
        /// <param name="presencePenalty">Number between -2.0 and 2.0. Positive values penalize
        /// new tokens based on whether they appear in the text so far, increasing the model's
        /// likelihood to talk about new topics.</param>
        /// <param name="stopSequences">Up to 4 sequences where the API will stop generating further tokens.</param>
        /// <returns>The task with the completion response.</returns>
        public static async Task<string> GetChatCompletionAsync(
            string userPrompt,
            string deploymentName,
            string systemMessage = null,
            float? temperature = null,
            int? maxTokens = null,
            int choiceCount = 1,
            float? frequencyPenalty = null,
            float? nucleusSamplingFactor = null,
            float? presencePenalty = null,
            IEnumerable<string> stopSequences = null)
        {
            try
            {
                var (_, response) = await GetChatCompletionAsync(
                    new ChatRequestUserMessage(userPrompt),
                    deploymentName,
                    systemMessage,
                    temperature,
                    maxTokens,
                    choiceCount,
                    frequencyPenalty,
                    nucleusSamplingFactor,
                    presencePenalty,
                    stopSequences);
                return response;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Get a one-off completion of a user's prompt, containing both text and images.
        /// </summary>
        /// <param name="userPrompt">The contents of the user's prompt (text and image links).</param>
        /// <param name="deploymentName">The deployment (model) to use.</param>
        /// <param name="systemMessage">The system message (optional).</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2.
        /// Higher values like 0.8 will make the output more random, while lower values
        /// like 0.2 will make it more focused and deterministic.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="choiceCount">Number of choices to generate.</param>
        /// <param name="frequencyPenalty">Number between -2.0 and 2.0. Positive values penalize
        /// new tokens based on their existing frequency in the text so far, decreasing the model's
        /// likelihood to repeat the same line verbatim.</param>
        /// <param name="nucleusSamplingFactor">An alternative to sampling with temperature,
        /// where the model considers the results of the tokens with top_p probability mass.</param>
        /// <param name="presencePenalty">Number between -2.0 and 2.0. Positive values penalize
        /// new tokens based on whether they appear in the text so far, increasing the model's
        /// likelihood to talk about new topics.</param>
        /// <param name="stopSequences">Up to 4 sequences where the API will stop generating further tokens.</param>
        /// <returns>The task with the completion response.</returns>
        public static async Task<string> GetChatImageCompletionAsync(
            IEnumerable<ChatMessageContentItem> userPrompt,
            string deploymentName,
            string systemMessage = null,
            float? temperature = null,
            int? maxTokens = null,
            int choiceCount = 1,
            float? frequencyPenalty = null,
            float? nucleusSamplingFactor = null,
            float? presencePenalty = null,
            IEnumerable<string> stopSequences = null)
        {
            try
            {
                var (_, response) = await GetChatCompletionAsync(
                    new ChatRequestUserMessage(userPrompt),
                    deploymentName,
                    systemMessage,
                    temperature,
                    maxTokens,
                    choiceCount,
                    frequencyPenalty,
                    nucleusSamplingFactor,
                    presencePenalty,
                    stopSequences);
                return response;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Begin a new chat conversation. This conversation can be continued later.
        /// </summary>
        /// <param name="userContents">The contents of the first user utterance.</param>
        /// <param name="deploymentName">The deployment (model) to use.</param>
        /// <param name="systemMessage">The system message.</param>
        /// <param name="temperature">What sampling temperature to use, between 0 and 2.
        /// Higher values like 0.8 will make the output more random, while lower values
        /// like 0.2 will make it more focused and deterministic.</param>
        /// <param name="maxTokens">Maximum number of tokens to generate.</param>
        /// <param name="choiceCount">Number of choices to generate.</param>
        /// <param name="frequencyPenalty">Number between -2.0 and 2.0. Positive values penalize
        /// new tokens based on their existing frequency in the text so far, decreasing the model's
        /// likelihood to repeat the same line verbatim.</param>
        /// <param name="nucleusSamplingFactor">An alternative to sampling with temperature,
        /// where the model considers the results of the tokens with top_p probability mass.</param>
        /// <param name="presencePenalty">Number between -2.0 and 2.0. Positive values penalize
        /// new tokens based on whether they appear in the text so far, increasing the model's
        /// likelihood to talk about new topics.</param>
        /// <param name="stopSequences">Up to 4 sequences where the API will stop generating further tokens.</param>
        /// <returns>The task with the first chat completion response.
        /// The guid can be used to continue the conversation later on.</returns>
        public static async Task<(Guid Guid, string Response)> BeginNewChatConversationAsync(
            string userContents,
            string deploymentName,
            string systemMessage = "You are a helpful assistant.",
            float? temperature = null,
            int? maxTokens = null,
            int choiceCount = 1,
            float? frequencyPenalty = null,
            float? nucleusSamplingFactor = null,
            float? presencePenalty = null,
            IEnumerable<string> stopSequences = null)
        {
            try
            {
                var (chatCompletionsOptions, assistantResponse) = await GetChatCompletionAsync(
                    new ChatRequestUserMessage(userContents),
                    deploymentName,
                    systemMessage,
                    temperature,
                    maxTokens,
                    choiceCount,
                    frequencyPenalty,
                    nucleusSamplingFactor,
                    presencePenalty,
                    stopSequences);

                // Append the response and save the conversation for continuing later
                chatCompletionsOptions.Messages.Add(new ChatRequestAssistantMessage(assistantResponse));
                var newGuid = Guid.NewGuid();
                Conversations[newGuid] = chatCompletionsOptions;
                return (newGuid, assistantResponse);
            }
            catch (Exception ex)
            {
                return (Guid.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Continues a conversation specified by a guid with the OpenAI service with the next user utterance.
        /// </summary>
        /// <param name="guid">The guid for the chat completions conversation.</param>
        /// <param name="userContents">The contents of the user utterance.</param>
        /// <returns>The task for prompting the OpenAI service. The guid can be used to continue the conversation.</returns>
        public static async Task<(Guid Guid, string Response)> ContinueChatConversationAsync(Guid guid, string userContents)
        {
            // Find the chat state for this conversation
            if (!Conversations.TryGetValue(guid, out var conversation))
            {
                return (Guid.Empty, null);
            }

            try
            {
                conversation.Messages.Add(new ChatRequestUserMessage(userContents));
                var completionResult = await openAIClient.GetChatCompletionsAsync(conversation);
                var assistantResponse = completionResult.Value.Choices.First().Message.Content;
                conversation.Messages.Add(new ChatRequestAssistantMessage(assistantResponse));
                return (guid, assistantResponse);
            }
            catch (Exception ex)
            {
                Conversations.TryRemove(guid, out var _);
                return (Guid.Empty, ex.Message);
            }
        }

        /// <summary>
        /// Ends a chat conversation specified by a guid.
        /// </summary>
        /// <param name="guid">The guid identifying the chat conversation.</param>
        public static void EndChatConversation(Guid guid)
        {
            Conversations.TryRemove(guid, out var _);
        }

        private static async Task<(ChatCompletionsOptions Parameters, string Response)> GetChatCompletionAsync(
            ChatRequestUserMessage userPrompt,
            string deploymentName,
            string systemMessage = null,
            float? temperature = null,
            int? maxTokens = null,
            int choiceCount = 1,
            float? frequencyPenalty = null,
            float? nucleusSamplingFactor = null,
            float? presencePenalty = null,
            IEnumerable<string> stopSequences = null)
        {
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = deploymentName,
                Temperature = temperature,
                MaxTokens = maxTokens,
                ChoiceCount = choiceCount,
                FrequencyPenalty = frequencyPenalty,
                NucleusSamplingFactor = nucleusSamplingFactor,
                PresencePenalty = presencePenalty,
            };

            if (!string.IsNullOrEmpty(systemMessage))
            {
                chatCompletionsOptions.Messages.Add(new ChatRequestSystemMessage(systemMessage));
            }

            chatCompletionsOptions.Messages.Add(userPrompt);

            if (stopSequences != null)
            {
                foreach (var stop in stopSequences)
                {
                    chatCompletionsOptions.StopSequences.Add(stop);
                }
            }

            var completionResult = await openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            return (chatCompletionsOptions, completionResult.Value.Choices.First().Message.Content);
        }
    }
}

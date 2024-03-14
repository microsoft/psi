// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Azure.AI.OpenAI;

    /// <summary>
    /// Represents a large language model prompt.
    /// </summary>
    public class LLMPrompt
    {
        /// <summary>
        /// Gets or sets the name of the prompt.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the prompt template.
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Gets or sets the list of test cases for this prompt.
        /// </summary>
        public List<LLMPromptTestCase> TestCases { get; set; } = new ();

        /// <summary>
        /// Gets or sets the minimum test accuracy required for this prompt.
        /// </summary>
        public double MinimumTestAccuracy { get; set; }

        /// <summary>
        /// Gets or sets the endpoint to use for this prompt.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the deployment to use for this prompt.
        /// </summary>
        public string Deployment { get; set; } = null;

        /// <summary>
        /// Gets or sets the temperature to use for this prompt.
        /// </summary>
        public float? Temperature { get; set; } = null;

        /// <summary>
        /// Gets or sets the maximum number of tokens to generate for this prompt.
        /// </summary>
        public int? MaxTokens { get; set; } = null;

        /// <summary>
        /// Gets or sets the system message to use for this prompt.
        /// </summary>
        public string SystemMessage { get; set; } = null;

        /// <summary>
        /// Runs the prompt with the specified text and image arguments.
        /// </summary>
        /// <param name="textArgs">The text arguments.</param>
        /// <param name="imageArgs">The image arguments.</param>
        /// <returns>A task that resolves to the generated text.</returns>
        public async Task<string> Run(string[] textArgs, string[] imageArgs = null)
        {
            var prompt = this.Template;

            // Fill in the text arguments
            for (int i = textArgs.Length - 1; i >= 0; i--)
            {
                prompt = prompt.Replace($"$arg{i}", textArgs[i]);
            }

            // If doing image completion, progressively split the prompt into structured text and image contents
            if (imageArgs != null && imageArgs.Length != 0)
            {
                // Start with a single text content item
                var structuredContents = new List<ChatMessageContentItem>() { new ChatMessageTextContentItem(prompt) };
                for (int i = imageArgs.Length - 1; i >= 0; i--)
                {
                    for (int j = 0; j < structuredContents.Count; j++)
                    {
                        // Find which text content item contains this image argument
                        if (structuredContents[j] is ChatMessageTextContentItem textContent && textContent.Text.Contains($"$image{i}"))
                        {
                            // Split into (up to) three parts: text before the image argument, the image argument, and text after the image argument
                            var textSplit = textContent.Text.Split(new string[1] { $"$image{i}" }, StringSplitOptions.None);

                            structuredContents.RemoveAt(j);
                            int newContentsCount = 0;

                            // pre-image text
                            if (!string.IsNullOrEmpty(textSplit[0]))
                            {
                                structuredContents.Insert(j, new ChatMessageTextContentItem(textSplit[0]));
                                newContentsCount++;
                            }

                            // Read the referenced image as a string
                            var imageBytes = File.ReadAllBytes(imageArgs[i]);
                            var imageUri = new Uri($"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}");
                            structuredContents.Insert(j + newContentsCount, new ChatMessageImageContentItem(imageUri));
                            newContentsCount++;

                            // post-image text
                            if (!string.IsNullOrEmpty(textSplit[1]))
                            {
                                structuredContents.Insert(j + newContentsCount, new ChatMessageTextContentItem(textSplit[1]));
                            }

                            break;
                        }
                    }
                }

                return await AzureOpenAIService.GetChatImageCompletionAsync(structuredContents, this.Deployment, this.SystemMessage, this.Temperature, this.MaxTokens);
            }
            else
            {
                return await AzureOpenAIService.GetChatCompletionAsync(prompt, this.Deployment, this.SystemMessage, this.Temperature, this.MaxTokens);
            }
        }

        /// <summary>
        /// Runs the test cases for this prompt and returns whether the accuracy is above the minimum.
        /// </summary>
        /// <param name="accuracy">On return, contains the accuracy.</param>
        /// <returns>True if the accuracy is above the minimum, false otherwise.</returns>
        public bool RunTestCases(out double accuracy)
        {
            var correct = 0;
            var total = 0;
            foreach (var testCase in this.TestCases)
            {
                var result = this.Run(testCase.ParameterValues).Result;
                if (result == testCase.ExpectedResult)
                {
                    correct++;
                }

                total++;
            }

            accuracy = correct / (double)total;

            return accuracy > this.MinimumTestAccuracy;
        }

        /// <inheritdoc/>
        public override string ToString() => this.Name;
    }
}

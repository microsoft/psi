// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Linq;
    using Azure;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    /// <summary>
    /// Component that runs an LLM query.
    /// </summary>
    public class LLMQueryRunner : ConsumerProducer<(string Query, string Prompt, string[] Parameters), string>
    {
        private readonly LLMQueryLibrary llmQueryLibrary;

        /// <summary>
        /// Initializes a new instance of the <see cref="LLMQueryRunner"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="llmQueryLibraryFilename">The filename of the LLM query library.</param>
        /// <param name="name">An optional name for the component.</param>
        public LLMQueryRunner(Pipeline pipeline, string llmQueryLibraryFilename, string name = nameof(LLMQueryRunner))
            : base(pipeline, name)
        {
            this.llmQueryLibrary = LLMQueryLibrary.FromJsonFile(llmQueryLibraryFilename);
            this.LLMQuery = pipeline.CreateEmitter<string>(this, nameof(this.LLMQuery));

            AzureOpenAIService.Initialize(
                new Uri(Environment.GetEnvironmentVariable("AzureOpenAI.Endpoint")),
                new AzureKeyCredential(Environment.GetEnvironmentVariable("AzureOpenAI.Key")));
        }

        /// <summary>
        /// Gets the emitter for the LLM query.
        /// </summary>
        public Emitter<string> LLMQuery { get; }

        /// <inheritdoc/>
        protected override async void Receive((string Query, string Prompt, string[] Parameters) data, Envelope envelope)
        {
            var result = await this.llmQueryLibrary.RunQuery(data.Query, data.Prompt, data.Parameters);
            var parameters = string.Join(",", data.Parameters);
            this.LLMQuery.Post($"{data.Query}.{data.Prompt}({parameters})", envelope.OriginatingTime);
            this.Out.Post(result, envelope.OriginatingTime);
        }
    }
}

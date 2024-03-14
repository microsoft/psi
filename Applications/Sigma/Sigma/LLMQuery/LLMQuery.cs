// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a query for a LLM.
    /// </summary>
    public class LLMQuery
    {
        /// <summary>
        /// Gets or sets the name of the query.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the list of LLM prompts.
        /// </summary>
        public List<LLMPrompt> LLMPrompts { get; set; } = new ();

        /// <summary>
        /// Gets the LLM prompt with the given name.
        /// </summary>
        /// <param name="promptName">The prompt name.</param>
        /// <returns>The LLM prompt with the given name.</returns>
        public LLMPrompt this[string promptName] => this.LLMPrompts.FirstOrDefault(q => q.Name == promptName);

        /// <summary>
        /// Runs the LLM query with the given prompt and parameters.
        /// </summary>
        /// <param name="promptName">The prompt name.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A task that resolves to the result of the LLM query.</returns>
        public async Task<string> RunPrompt(string promptName, params string[] parameters)
            => await this[promptName].Run(parameters);

        /// <inheritdoc/>
        public override string ToString() => this.Name;
    }
}
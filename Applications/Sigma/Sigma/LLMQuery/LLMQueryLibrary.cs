// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Psi.Data.Helpers;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents an LLM query library.
    /// </summary>
    public class LLMQueryLibrary
    {
        /// <summary>
        /// Gets or sets the queries.
        /// </summary>
        public List<LLMQuery> Queries { get; set; } = new ();

        /// <summary>
        /// Gets the query with the specified name.
        /// </summary>
        /// <param name="queryName">The name of the query.</param>
        /// <returns>The query.</returns>
        public LLMQuery this[string queryName] => this.Queries.FirstOrDefault(q => q.Name == queryName);

        /// <summary>
        /// Loads a task library from a json file.
        /// </summary>
        /// <param name="jsonFilename">The file to load the task from.</param>
        /// <returns>The task library.</returns>
        public static LLMQueryLibrary FromJsonFile(string jsonFilename)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    SerializationBinder = new SafeSerializationBinder(),
                });

            StreamReader jsonFile = null;
            try
            {
                jsonFile = File.OpenText(jsonFilename);
                using var jsonReader = new JsonTextReader(jsonFile);
                jsonFile = null;

                // Deserialize the visualization container
                return serializer.Deserialize<LLMQueryLibrary>(jsonReader);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        /// <summary>
        /// Runs the specified query.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <param name="prompt">The prompt to run for the specified query.</param>
        /// <param name="parameters">The parameters to the prompt.</param>
        /// <returns>A task that resolves to the result of the query.</returns>
        public async Task<string> RunQuery(string query, string prompt, params string[] parameters)
            => await this[query].RunPrompt(prompt, parameters);

        /// <summary>
        /// Saves the task library to a json file.
        /// </summary>
        /// <param name="jsonFilename">The file to save the task to.</param>
        public void SaveAsJson(string jsonFilename)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    SerializationBinder = new SafeSerializationBinder(),
                });

            StreamWriter jsonFile = null;
            try
            {
                jsonFile = File.CreateText(jsonFilename);
                using var jsonWriter = new JsonTextWriter(jsonFile);
                jsonFile = null;
                serializer.Serialize(jsonWriter, this);
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }
    }
}

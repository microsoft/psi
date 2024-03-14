// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a test case for an LLM prompt.
    /// </summary>
    public class LLMPromptTestCase
    {
        /// <summary>
        /// Gets or sets the paramater values for the test case.
        /// </summary>
        public string[] ParameterValues { get; set; }

        /// <summary>
        /// Gets the parameter values as a short string to render.
        /// </summary>
        [JsonIgnore]
        public string ParameterValuesAsShortString => "[" + string.Join("][", this.ParameterValues.Select(pv => pv.Contains("\\n") ? pv.Substring(0, pv.IndexOf("\\n")) : pv)) + "]";

        /// <summary>
        /// Gets or sets the expected result.
        /// </summary>
        public string ExpectedResult { get; set; }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Language
{
    /// <summary>
    /// Represents the configuration for the <see cref="LUISIntentDetector"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="LUISIntentDetector"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class LUISIntentDetectorConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LUISIntentDetectorConfiguration"/> class.
        /// </summary>
        public LUISIntentDetectorConfiguration()
        {
            this.EndpointUrl = "https://{0}.api.cognitive.microsoft.com/luis/v2.0/apps/";
            this.ApplicationId = null;
            this.SubscriptionKey = null;
            this.Region = "westus";
        }

        /// <summary>
        /// Gets or sets the <a href="http://www.luis.ai/">LUIS</a> endpoint URI.
        /// </summary>
        /// <remarks>
        /// This should be left at its default value and not be changed unless necessary.
        /// </remarks>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Gets or sets the <a href="http://www.luis.ai/">LUIS</a> application ID.
        /// </summary>
        /// <remarks>
        /// In order to use LUIS, a separate LUIS subscription ID and application ID are required. See
        /// https://www.luis.ai/ for more information on obtaining a LUIS subscription and creating LUIS
        /// applications.
        /// </remarks>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets the <a href="http://www.luis.ai/">LUIS</a> subscription ID.
        /// </summary>
        /// <remarks>
        /// In order to use LUIS, a separate LUIS subscription ID and application ID are required. See
        /// https://www.luis.ai/ for more information on obtaining a LUIS subscription and creating LUIS
        /// applications.
        /// </remarks>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <remarks>
        /// This is the region in which the application is published.
        /// </remarks>
        public string Region { get; set; }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Vision
{
    using Microsoft.ProjectOxford.Vision;

    /// <summary>
    /// Represents the configuration for the <see cref="ImageAnalyzer"/> component.
    /// </summary>
    public sealed class ImageAnalyzerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageAnalyzerConfiguration"/> class.
        /// </summary>
        /// <param name="key">The subscription key to use</param>
        /// <param name="features">The list of features to look for</param>
        public ImageAnalyzerConfiguration(string key = null, params VisualFeature[] features)
        {
            this.SubscriptionKey = key;
            this.VisualFeatures = features;
        }

        /// <summary>
        /// Gets or sets the subscription key for Cognitive Services Vision API.
        /// </summary>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets the list of visual features to extract from images
        /// </summary>
        public VisualFeature[] VisualFeatures { get; set; }
    }
}

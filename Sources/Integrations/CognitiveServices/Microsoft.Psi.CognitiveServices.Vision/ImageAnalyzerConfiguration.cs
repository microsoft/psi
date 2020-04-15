// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Vision
{
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;

    /// <summary>
    /// Represents the configuration for the <see cref="ImageAnalyzer"/> component.
    /// </summary>
    public sealed class ImageAnalyzerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageAnalyzerConfiguration"/> class.
        /// </summary>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="features">The list of features to look for.</param>
        public ImageAnalyzerConfiguration(string subscriptionKey = null, string region = null, params VisualFeatureTypes[] features)
        {
            this.SubscriptionKey = subscriptionKey;
            this.VisualFeatures = features;
            this.Region = region;
        }

        /// <summary>
        /// Gets or sets the subscription key for Cognitive Services Vision API.
        /// </summary>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        /// <remarks>The region associated with the subscription key.</remarks>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the list of visual features to extract from images.
        /// </summary>
        public VisualFeatureTypes[] VisualFeatures { get; set; }
    }
}

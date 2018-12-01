// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Vision
{
    using Microsoft.ProjectOxford.Vision;

    /// <summary>
    /// Stream operators and extension methods for Microsoft.Psi.CognitiveServices.Vision
    /// </summary>
    public static class Operators
    {
        /// <summary>
        /// Gets the tags associated with an images via <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Cognitive Services subscription key.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of tags for each image.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<ProjectOxford.Vision.Contract.Tag[]> GetTags(this IProducer<Shared<Imaging.Image>> source, string subscriptionKey, DeliveryPolicy deliveryPolicy = null)
        {
            var imageAnalyzer = new ImageAnalyzer(source.Out.Pipeline, new ImageAnalyzerConfiguration(subscriptionKey, VisualFeature.Tags));
            source.PipeTo(imageAnalyzer.In, deliveryPolicy);
            return imageAnalyzer.Tags;
        }
    }
}
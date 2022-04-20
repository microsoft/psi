// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Vision
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Stream operators and extension methods for Microsoft.Psi.CognitiveServices.Vision.
    /// </summary>
    public static class Operators
    {
        /// <summary>
        /// Gets the adult info on a stream of images via the <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API.</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of adult info.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<AdultInfo> GetAdult(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(GetAdult))
            => source.GetImageAnalyzerInfo(subscriptionKey, region, VisualFeatureTypes.Adult, ia => ia?.Adult, deliveryPolicy, name);

        /// <summary>
        /// Gets the brands info on a stream of images via the <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API.</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of brands info.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<IList<DetectedBrand>> GetBrands(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(GetBrands))
            => source.GetImageAnalyzerInfo(subscriptionKey, region, VisualFeatureTypes.Brands, ia => ia?.Brands, deliveryPolicy, name);

        /// <summary>
        /// Gets the category info on a stream of images via the <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API.</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of category info.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<IList<Category>> GetCategories(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(GetCategories))
            => source.GetImageAnalyzerInfo(subscriptionKey, region, VisualFeatureTypes.Categories, ia => ia?.Categories, deliveryPolicy, name);

        /// <summary>
        /// Gets the color info on a stream of images via the <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API.</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of color info.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<ColorInfo> GetColor(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(GetColor))
            => source.GetImageAnalyzerInfo(subscriptionKey, region, VisualFeatureTypes.Color, ia => ia?.Color, deliveryPolicy, name);

        /// <summary>
        /// Gets the image description details on a stream of images via the <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API.</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of image description details.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<ImageDescriptionDetails> GetDescription(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(GetDescription))
            => source.GetImageAnalyzerInfo(subscriptionKey, region, VisualFeatureTypes.Description, ia => ia?.Description, deliveryPolicy, name);

        /// <summary>
        /// Gets the face description results on a stream of images via the <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API.</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of face descriptions.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<IList<FaceDescription>> GetFaces(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(GetFaces))
            => source.GetImageAnalyzerInfo(subscriptionKey, region, VisualFeatureTypes.Faces, ia => ia?.Faces, deliveryPolicy, name);

        /// <summary>
        /// Gets the image type info on a stream of images via the <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API.</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of image image type info.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<ImageType> GetImageType(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(GetImageType))
            => source.GetImageAnalyzerInfo(subscriptionKey, region, VisualFeatureTypes.ImageType, ia => ia?.ImageType, deliveryPolicy, name);

        /// <summary>
        /// Gets the object detection results on a stream of images via the <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API.</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of detected objects.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<IList<DetectedObject>> GetObjects(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(GetObjects))
            => source.GetImageAnalyzerInfo(subscriptionKey, region, VisualFeatureTypes.Objects, ia => ia?.Objects, deliveryPolicy, name);

        /// <summary>
        /// Gets the tags on a stream of images via <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API.</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="subscriptionKey">The Azure subscription key to use.</param>
        /// <param name="region">The region for the Azure subscription.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the stream operator.</param>
        /// <returns>A stream of tags.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// subscription key is required to use this operator. For more information, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
        /// </remarks>
        public static IProducer<IList<ImageTag>> GetTags(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = nameof(GetTags))
            => source.GetImageAnalyzerInfo(subscriptionKey, region, VisualFeatureTypes.Tags, ia => ia?.Tags, deliveryPolicy, name);

        private static IProducer<T> GetImageAnalyzerInfo<T>(
            this IProducer<Shared<Image>> source,
            string subscriptionKey,
            string region,
            VisualFeatureTypes visualFeatureType,
            Func<ImageAnalysis, T> selector,
            DeliveryPolicy<Shared<Image>> deliveryPolicy = null,
            string name = null)
        {
            var imageAnalyzer = new ImageAnalyzer(source.Out.Pipeline, new ImageAnalyzerConfiguration(subscriptionKey, region, visualFeatureType), name);
            source.PipeTo(imageAnalyzer.In, deliveryPolicy);
            return imageAnalyzer.Out.Select(selector, DeliveryPolicy.SynchronousOrThrottle);
        }
    }
}
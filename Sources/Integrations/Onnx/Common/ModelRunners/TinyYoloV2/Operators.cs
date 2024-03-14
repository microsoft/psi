// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System.Linq;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Implements streaming operators for processing TinyYoloV2 model detection results.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Converts a stream of <see cref="TinyYoloV2DetectionResults"/> to <see cref="Object2DDetectionResults"/>.
        /// </summary>
        /// <param name="source">The stream of <see cref="TinyYoloV2DetectionResults"/>.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <returns>The stream of <see cref="Object2DDetectionResults"/>.</returns>
        public static IProducer<Object2DDetectionResults> ToObject2DDetectionResults(
            this IProducer<TinyYoloV2DetectionResults> source,
            DeliveryPolicy<TinyYoloV2DetectionResults> deliveryPolicy = null,
            string name = nameof(ToObject2DDetectionResults))
            => source.Select(m => m?.ToObject2DDetectionResults(), deliveryPolicy, name);
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System.Linq;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Implements streaming operators for MaskRCNN detection results.
    /// </summary>
    public static partial class Operators
    {
        /// <summary>
        /// Converts a stream of <see cref="MaskRCNNDetectionResults"/> to <see cref="Object2DDetectionResults"/>.
        /// </summary>
        /// <param name="source">The stream of <see cref="MaskRCNNDetectionResults"/>.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <returns>The stream of <see cref="Object2DDetectionResults"/>.</returns>
        public static IProducer<Object2DDetectionResults> ToObject2DDetectionResults(
            this IProducer<MaskRCNNDetectionResults> source,
            DeliveryPolicy<MaskRCNNDetectionResults> deliveryPolicy = null,
            string name = nameof(ToObject2DDetectionResults))
            => source.Select(m => m.ToObject2DDetectionResults(), deliveryPolicy, name);
    }
}
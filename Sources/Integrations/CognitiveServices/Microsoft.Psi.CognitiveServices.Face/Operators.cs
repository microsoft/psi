// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Face
{
    using System.Collections.Generic;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Stream operators and extension methods for Microsoft.Psi.CognitiveServices.Face.
    /// </summary>
    public static class Operators
    {
        /// <summary>
        /// Performs face recognition over a stream of images via <a href="https://azure.microsoft.com/en-us/services/cognitive-services/face/">Microsoft Cognitive Services Face API</a>.
        /// </summary>
        /// <param name="source">The source stream of images.</param>
        /// <param name="configuration">The face recognizer configuration.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>A stream of messages containing detected faces and candidate identities of each person in the image.</returns>
        /// <remarks>
        /// A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/face/">Microsoft Cognitive Services Face API</a>
        /// subscription key is required to use this operators. In addition, a person group needs to be created ahead of time, and the id of the person group
        /// passed to the operator via the configuration. For more information, and to see how to create person groups, see the full direct API for.
        /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/face/">Microsoft Cognitive Services Face API</a>
        /// </remarks>
        public static IProducer<IList<IList<(string Name, double Confidence)>>> RecognizeFace(this IProducer<Shared<Image>> source, FaceRecognizerConfiguration configuration, DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            var faceRecognizer = new FaceRecognizer(source.Out.Pipeline, configuration);
            source.PipeTo(faceRecognizer, deliveryPolicy);
            return faceRecognizer.Out;
        }
    }
}

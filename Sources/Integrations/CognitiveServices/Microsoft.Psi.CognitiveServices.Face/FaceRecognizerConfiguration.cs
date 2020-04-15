// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Face
{
    using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

    /// <summary>
    /// Represents the configuration for the <see cref="FaceRecognizer"/> component.
    /// </summary>
    public sealed class FaceRecognizerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FaceRecognizerConfiguration"/> class.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key to use.</param>
        /// <param name="subscriptionPoint">The endpoint to use.</param>
        /// <param name="personGroupId">The person group id.</param>
        public FaceRecognizerConfiguration(string subscriptionKey, string subscriptionPoint, string personGroupId)
        {
            this.SubscriptionKey = subscriptionKey;
            this.Endpoint = subscriptionPoint;
            this.PersonGroupId = personGroupId;
        }

        /// <summary>
        /// Gets or sets the subscription key for Cognitive Services Face API.
        /// </summary>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Gets or sets the endpoint for Cognitive Services Face API.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the person group id within which to perform face recognition.
        /// </summary>
        public string PersonGroupId { get; set; }

        /// <summary>
        /// Gets or sets the recognition model.
        /// </summary>
        public string RecognitionModelName { get; set; } = RecognitionModel.Recognition02;
    }
}

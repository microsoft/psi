// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Face
{
    using System;

    /// <summary>
    /// Represents the configuration for the <see cref="FaceRecognizer"/> component.
    /// </summary>
    public sealed class FaceRecognizerConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FaceRecognizerConfiguration"/> class.
        /// </summary>
        public FaceRecognizerConfiguration()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FaceRecognizerConfiguration"/> class.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key to use.</param>
        /// <param name="subscriptionPoint">The subscription point to use.</param>
        /// <param name="personGroupId">The person group id.</param>
        public FaceRecognizerConfiguration(string subscriptionKey, string subscriptionPoint, Guid personGroupId)
        {
            this.SubscriptionKey = subscriptionKey;
            this.SubscriptionAccessPoint = subscriptionPoint;
            this.PersonGroupId = personGroupId;
        }

        /// <summary>
        /// Gets or sets the subscription key for Cognitive Services Face API.
        /// </summary>
        public string SubscriptionKey { get; set; } = null;

        /// <summary>
        /// Gets or sets the subscription access point for Cognitive Services Face API.
        /// </summary>
        public string SubscriptionAccessPoint { get; set; } = "https://westus.api.cognitive.microsoft.com/face/v1.0";

        /// <summary>
        /// Gets or sets the person group id within which to perform face recognition.
        /// </summary>
        public Guid PersonGroupId { get; set; }
    }
}

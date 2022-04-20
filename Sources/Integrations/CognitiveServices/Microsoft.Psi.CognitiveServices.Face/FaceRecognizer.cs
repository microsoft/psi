// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Face
{
    using System;
    using System.Collections.Generic;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Vision.Face;
    using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that performs face recognition via <a href="https://azure.microsoft.com/en-us/services/cognitive-services/face/">Microsoft Cognitive Services Face API</a>.
    /// </summary>
    /// <remarks>The component takes in a stream of images and produces a stream of messages containing detected faces and candidate identities of each
    /// person in the image. A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/face/">Microsoft Cognitive Services Face API</a>
    /// subscription key is required to use this component. In addition, a person group needs to be created ahead of time, and the id of the person group
    /// passed to the component via the configuration. For more information, and to see how to create person groups, see the full direct API for.
    /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/face/">Microsoft Cognitive Services Face API</a>
    /// </remarks>
    public sealed class FaceRecognizer : AsyncConsumerProducer<Shared<Image>, IList<IList<(string Name, double Confidence)>>>, IDisposable
    {
        /// <summary>
        /// Empty results.
        /// </summary>
        private static readonly IList<IList<(string, double)>> Empty = new IList<(string, double)>[0];

        /// <summary>
        /// The configuration to use for this component.
        /// </summary>
        private readonly FaceRecognizerConfiguration configuration;

        /// <summary>
        /// The client that communicates with the cloud image analyzer service.
        /// </summary>
        private FaceClient client = null;

        /// <summary>
        /// The group of persons from the cognitive services API.
        /// </summary>
        private Dictionary<Guid, Person> people = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="FaceRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        /// <param name="name">An optional name for the component.</param>
        public FaceRecognizer(Pipeline pipeline, FaceRecognizerConfiguration configuration, string name = nameof(FaceRecognizer))
            : base(pipeline, name)
        {
            this.configuration = configuration;
            this.RateLimitExceeded = pipeline.CreateEmitter<bool>(this, nameof(this.RateLimitExceeded));
            this.client = new FaceClient(new ApiKeyServiceClientCredentials(this.configuration.SubscriptionKey))
            {
                Endpoint = this.configuration.Endpoint,
            };
            this.client.PersonGroupPerson.ListAsync(this.configuration.PersonGroupId).ContinueWith(list =>
            {
                this.people = list.Result.ToDictionary(p => p.PersonId);
            }).Wait();
        }

        /// <summary>
        /// Gets a stream that indicates the rate limit for the service calls was exceeded.
        /// </summary>
        /// <remarks>A value of true is posted on the stream each time the rate limit for calling Cognitive Services FACE API is exceeded.</remarks>
        public Emitter<bool> RateLimitExceeded { get; }

        /// <summary>
        /// Disposes the face recognizer component.
        /// </summary>
        public void Dispose()
        {
            this.client.Dispose();
            this.client = null;
        }

        /// <inheritdoc/>
        protected override async Task ReceiveAsync(Shared<Image> data, Envelope e)
        {
            using Stream imageFileStream = new MemoryStream();
            try
            {
                // convert image to a Stream and send to Cog Services
                data.Resource.ToBitmap(false).Save(imageFileStream, ImageFormat.Jpeg);
                imageFileStream.Seek(0, SeekOrigin.Begin);

                var detected = (await this.client.Face.DetectWithStreamAsync(imageFileStream, recognitionModel: this.configuration.RecognitionModelName)).Select(d => d.FaceId.Value).ToList();

                // Identify each face
                if (detected.Count > 0)
                {
                    var identified = await this.client.Face.IdentifyAsync(detected, this.configuration.PersonGroupId);
                    var results = identified.Select(p => (IList<(string, double)>)p.Candidates.Select(c => (this.people[c.PersonId].Name, c.Confidence)).ToList()).ToList();
                    this.Out.Post(results, e.OriginatingTime);
                }
                else
                {
                    this.Out.Post(Empty, e.OriginatingTime);
                }
            }
            catch (APIErrorException exception)
            {
                // swallow exceptions unless it's a rate limit exceeded
                if (exception.Body.Error.Code == "RateLimitExceeded")
                {
                    this.RateLimitExceeded.Post(true, e.OriginatingTime);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
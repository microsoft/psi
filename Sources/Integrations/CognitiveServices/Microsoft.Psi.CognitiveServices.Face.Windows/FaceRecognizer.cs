// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Face
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ProjectOxford.Face;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that performs face recognition via <a href="https://azure.microsoft.com/en-us/services/cognitive-services/face/">Microsoft Cognitive Services Face API</a>.
    /// </summary>
    /// <remarks>The component takes in a stream of images and produces a stream of messages containing the distribution over the possible identities of the
    /// person in the image. A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/face/">Microsoft Cognitive Services Face API</a>
    /// subscription key is required to use this component. In addition, a person group needs to be created ahead of time, and the id of the person group
    /// passed to the component via the configuration. For more information, and to see how to create person groups, see the full direct API for.
    /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/face/">Microsoft Cognitive Services Face API</a>
    /// </remarks>
    public sealed class FaceRecognizer : AsyncConsumerProducer<Shared<Image>, Dictionary<string, double>>, IDisposable
    {
        /// <summary>
        /// The client that communicates with the cloud image analyzer service.
        /// </summary>
        private FaceServiceClient faceServiceClient;

        /// <summary>
        /// The configuration to use for this component.
        /// </summary>
        private FaceRecognizerConfiguration configuration;

        /// <summary>
        /// The group of persons from the cognitive services API.
        /// </summary>
        private Task<Microsoft.ProjectOxford.Face.Contract.Person[]> persons;

        /// <summary>
        /// Initializes a new instance of the <see cref="FaceRecognizer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The component configuration.</param>
        public FaceRecognizer(Pipeline pipeline, FaceRecognizerConfiguration configuration)
            : base(pipeline)
        {
            this.configuration = configuration;
            this.RateLimitExceeded = pipeline.CreateEmitter<bool>(this, nameof(this.RateLimitExceeded));

            this.faceServiceClient = new FaceServiceClient(this.configuration.SubscriptionKey, this.configuration.SubscriptionAccessPoint);
            this.persons = this.faceServiceClient.ListPersonsAsync(configuration.PersonGroupId.ToString());
            this.persons.Wait();
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
            this.faceServiceClient.Dispose();
            this.faceServiceClient = null;
        }

        /// <inheritdoc/>
        protected override async Task ReceiveAsync(Shared<Image> data, Envelope e)
        {
            using (Stream imageFileStream = new MemoryStream())
            {
                var results = new Dictionary<string, double>();

                try
                {
                    // convert image to a Stream and send to Cog Services
                    data.Resource.ToManagedImage(false).Save(imageFileStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imageFileStream.Seek(0, SeekOrigin.Begin);

                    var faces = await this.faceServiceClient.DetectAsync(imageFileStream);

                    // Identify each face
                    if (faces.Count() > 0)
                    {
                        var identifyResult = await this.faceServiceClient.IdentifyAsync(this.configuration.PersonGroupId.ToString(), faces.Select(ff => ff.FaceId).ToArray());

                        results = identifyResult[0].Candidates.ToDictionary(
                            c => this.persons.Result.FirstOrDefault(p => p.PersonId == c.PersonId)?.Name,
                            c => c.Confidence);
                    }
                }
                catch (FaceAPIException exception)
                {
                    // swallow exceptions unless it's a rate limit exceeded
                    if (exception.ErrorCode == "RateLimitExceeded")
                    {
                        this.RateLimitExceeded.Post(true, e.OriginatingTime);
                    }
                    else
                    {
                        throw exception;
                    }
                }

                this.Out.Post(results, e.OriginatingTime);
            }
        }
    }
}
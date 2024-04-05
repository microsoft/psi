// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Text;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Format;
    using NetMQ;
    using NetMQ.Sockets;
    using Image = Microsoft.Psi.Imaging.Image;

    /// <summary>
    /// Component implementing a client for the Detic Model server.
    /// </summary>
    public class DeticModelRunner : ConsumerProducer<(Shared<Image> Image, List<string> Classes), DeticDetectionResults>, IDisposable
    {
        private readonly bool verbose = false;
        private readonly Pipeline pipeline;
        private readonly string name;
        private readonly TimeSpan responseTimeout;
        private readonly bool useSourceOriginatingTimes;
        private readonly string requestAddress;
        private readonly string responseAddress;
        private readonly IImageToStreamEncoder imageToStreamEncoder;

        private PublisherSocket requestSocket;
        private SubscriberSocket responseSocket;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeticModelRunner"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="requestPort">The request port.</param>
        /// <param name="responsePort">The response port.</param>
        /// <param name="imageToStreamEncoder">An image to stream encoder.</param>
        /// <param name="responseTimeout">Optional time allowed while waiting for a response (default 10s).</param>
        /// <param name="useSourceOriginatingTimes">Optional flag indicating whether or not to post with originating times received over the socket (default true). If false, we ignore them and instead use pipeline's current time.</param>
        /// <param name="name">An optional name for the component.</param>
        public DeticModelRunner(
            Pipeline pipeline,
            int requestPort,
            int responsePort,
            IImageToStreamEncoder imageToStreamEncoder,
            TimeSpan? responseTimeout = null,
            bool useSourceOriginatingTimes = true,
            string name = nameof(DeticModelRunner))
            : base(pipeline)
        {
            this.pipeline = pipeline;
            this.name = name;
            this.imageToStreamEncoder = imageToStreamEncoder;

            this.useSourceOriginatingTimes = useSourceOriginatingTimes;
            this.requestSocket = new PublisherSocket();
            this.requestSocket.Options.SendHighWatermark = -1;
            this.requestSocket.Options.ReceiveHighWatermark = -1;
            this.requestAddress = $"tcp://127.0.0.1:{requestPort}";
            this.responseSocket = new SubscriberSocket();
            this.responseSocket.Options.SendHighWatermark = -1;
            this.responseSocket.Options.ReceiveHighWatermark = -1;
            this.responseAddress = $"tcp://127.0.0.1:{responsePort}";
            this.responseTimeout = responseTimeout ?? TimeSpan.FromSeconds(3);

            pipeline.PipelineRun += (s, e) =>
            {
                this.requestSocket.Bind(this.requestAddress);
                this.responseSocket.Connect(this.responseAddress);
                this.responseSocket.Subscribe("predictions");
                this.responseSocket.ReceiveReady += (_, _) => { /* required event handler */ };
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (this.requestSocket != null)
            {
                this.requestSocket.Dispose();
                this.requestSocket = null;
            }

            if (this.responseSocket != null)
            {
                this.responseSocket.Dispose();
                this.responseSocket = null;
            }
        }

        /// <inheritdoc />
        public override string ToString() => this.name;

        /// <inheritdoc />
        protected override void Receive((Shared<Image> Image, List<string> Classes) data, Envelope envelope)
        {
            var originatingTimeAsString = envelope.OriginatingTime.ToString("HH:mm:ss.ffff");

            if (data.Image == null || data.Image.Resource == null || data.Classes == null || data.Classes.Count == 0)
            {
                this.Out.Post(default, this.useSourceOriginatingTimes ? envelope.OriginatingTime : this.pipeline.GetCurrentTime());
                return;
            }

            // Create the results
            var results = new DeticDetectionResults(data.Image.Resource.Width, data.Image.Resource.Height);

            // Compose the request message
            using var encodedImage = EncodedImagePool.GetOrCreate(data.Image.Resource.Width, data.Image.Resource.Height, data.Image.Resource.PixelFormat);
            encodedImage.Resource.EncodeFrom(data.Image.Resource, this.imageToStreamEncoder);
            var buffer = encodedImage.Resource.GetBuffer();
            var imageBytes = new byte[buffer.Length];
            buffer.CopyTo(imageBytes, 0);

            // Serialize the message, truncate bytes return to specified length
            var (bytes, index, length) = MessagePackFormat.Instance.SerializeMessage((imageBytes, data.Classes), envelope.OriginatingTime);
            if (index != 0)
            {
                var slice = new byte[length];
                Array.Copy(bytes, index, slice, 0, length);
                bytes = slice;
            }

            var requestSuccess = false;
            var response = default(dynamic);
            var responseOriginatingTime = DateTime.MinValue;

            while (!requestSuccess)
            {
                // Send the request
                this.Log($"{this.name}@{originatingTimeAsString}: Sending {length} bytes).");
                this.requestSocket.SendMoreFrame("images").SendFrame(bytes, length);
                var sendTryCount = 1;

                // Estimate how much to wait based on the number of images
                var timeout = TimeSpan.FromTicks(this.responseTimeout.Ticks);

                // If we don't get a response
                while (!this.responseSocket.Poll(timeout))
                {
                    this.Log($"{this.name}@{originatingTimeAsString}: Timeout after {sendTryCount} tries / {length} bytes). Resending.");
                    this.requestSocket.SendMoreFrame("images").SendFrame(bytes, length);
                    sendTryCount++;
                }

                // We have a response, now wait in a loop to receive a response for the right envelope originating time
                while (responseOriginatingTime != envelope.OriginatingTime)
                {
                    var frames = new List<byte[]>();

                    if (responseOriginatingTime != DateTime.MinValue)
                    {
                        this.Log($"{this.name}@{originatingTimeAsString}: Retrying receive ... ");
                    }

                    if (!this.responseSocket.TryReceiveMultipartBytes(TimeSpan.FromSeconds(1), ref frames, 2))
                    {
                        this.Log($"{this.name}@{originatingTimeAsString}: Retrying receive failed. Reissuing request. ");
                        break;
                    }

                    if (responseOriginatingTime != DateTime.MinValue)
                    {
                        this.Log($"{this.name}@{originatingTimeAsString}: Retrying receive complete. ");
                    }

                    var receivedTopic = Encoding.Default.GetString(frames[0]);
                    if (receivedTopic != "predictions")
                    {
                        throw new Exception($"Unexpected topic name received ({receivedTopic})");
                    }

                    if (frames.Count < 2)
                    {
                        throw new Exception($"No payload message received for topic: predictions");
                    }

                    if (frames.Count > 2)
                    {
                        throw new Exception($"Multiple interleaved messages received on topic: predictions. Is the sender on the other side sending messages on multiple threads? You may need to add a lock over there.");
                    }

                    try
                    {
                        (response, responseOriginatingTime) = MessagePackFormat.Instance.DeserializeMessage(frames[1], 0, frames[1].Length);

                        if (responseOriginatingTime == envelope.OriginatingTime)
                        {
                            // Then we construct the answer and post
                            for (int i = 0; i < response.pred_classes.Length; i++)
                            {
                                var x1 = (int)response.pred_boxes[4 * i];
                                var y1 = (int)response.pred_boxes[4 * i + 1];
                                var x2 = (int)response.pred_boxes[4 * i + 2];
                                var y2 = (int)response.pred_boxes[4 * i + 3];
                                var box = new Rectangle(x1, y1, x2 - x1, y2 - y1);
                                var classIndex = (int)response.pred_classes[i];
                                var mask = new bool[response.pred_masks[i].Length][];
                                for (int r = 0; r < response.pred_masks[i].Length; r++)
                                {
                                    mask[r] = new bool[response.pred_masks[i][r].Length];
                                    for (int c = 0; c < response.pred_masks[i][r].Length; c++)
                                    {
                                        mask[r][c] = response.pred_masks[i][r][c];
                                    }
                                }

                                results.Detections.Add(new DeticDetection()
                                {
                                    Class = data.Classes[classIndex],
                                    Score = (float)response.scores[i],
                                    Rectangle = box,
                                    Mask = mask,
                                });
                            }

                            // The request has suceeded
                            requestSuccess = true;

                            // We break out of the loop waiting for the right response (and post)
                            break;
                        }
                        else
                        {
                            this.Log($"{this.name}@{originatingTimeAsString}: Response originating time {responseOriginatingTime.Ticks} does not match envelope {envelope.OriginatingTime.Ticks}");
                        }
                    }
                    catch
                    {
                        // We have a deserialization exception
                        this.Log($"{this.name}@{originatingTimeAsString}: Deserialize exception.");

                        // The request has failed, post default
                        this.Out.Post(default, this.useSourceOriginatingTimes ? responseOriginatingTime : this.pipeline.GetCurrentTime());
                        return;
                    }
                }
            }

            // At the end, post the result
            this.Out.Post(results, this.useSourceOriginatingTimes ? responseOriginatingTime : this.pipeline.GetCurrentTime());
        }

        private void Log(string log)
        {
            if (this.verbose)
            {
                Console.WriteLine(log);
            }
        }
    }
}

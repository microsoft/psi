// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that runs an ImageNet image classification model.
    /// </summary>
    /// <remarks>
    /// This class implements a \psi component that runs an ONNX model trained
    /// on the ImageNet dataset that operates on 224x224 RGB images and scores
    /// the image for each of the 1000 ImageNet classes. It takes an input
    /// stream of \psi images, applies a center-crop, rescales and normalizes
    /// the pixel values into the input vector expected by the model. It also
    /// parses the model outputs into a list of <see cref="LabeledPrediction"/>
    /// values, corresponding to the top N predictions by the model. For
    /// convenience, a set of pre-defined model runner configurations are
    /// defined for a number of image classification models available in the
    /// ONNX Model Zoo (https://github.com/onnx/models/tree/master/vision/classification).
    /// The ONNX model file for the corresponding configuration will need to be
    /// downloaded locally and the path to the model file will need to be
    /// specified when creating the configuration.
    /// </remarks>
    public class ImageNetModelRunner : ConsumerProducer<Shared<Image>, List<LabeledPrediction>>, IDisposable
    {
        private readonly ImageNetModelRunnerConfiguration configuration;
        private readonly float[] onnxInputVector = new float[3 * 224 * 224];
        private readonly ImageNetModelOutputParser outputParser;
        private OnnxModel onnxModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageNetModelRunner"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.ModelRunners.Gpu library instead of Microsoft.Psi.Onnx.ModelRunners.Cpu, and set
        /// the value of the <pararef name="gpuDeviceId"/> parameter to a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        public ImageNetModelRunner(Pipeline pipeline, ImageNetModelRunnerConfiguration configuration, string name = nameof(ImageNetModelRunner))
            : base(pipeline, name)
        {
            this.configuration = configuration;

            // create an ONNX model based on the supplied ImageNet model runner configuration
            this.onnxModel = new OnnxModel(new OnnxModelConfiguration()
            {
                ModelFileName = configuration.ModelFilePath,
                InputVectorName = configuration.InputVectorName,
                InputVectorSize = 3 * 224 * 224,
                OutputVectorName = configuration.OutputVectorName,
                GpuDeviceId = configuration.GpuDeviceId,
            });

            this.outputParser = new ImageNetModelOutputParser(configuration.ImageClassesFilePath, configuration.ApplySoftmaxToModelOutput);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.onnxModel?.Dispose();
            this.onnxModel = null;
        }

        /// <inheritdoc/>
        protected override void Receive(Shared<Image> data, Envelope envelope)
        {
            // Handle incoming nulls by outputting null
            if (data == null)
            {
                this.Out.Post(null, envelope.OriginatingTime);
                return;
            }

            // construct the ONNX model input vector (stored in this.onnxInputVector)
            // based on the incoming image
            this.ConstructOnnxInputVector(data);

            // run the model over the input vector
            var outputVector = this.onnxModel.GetPrediction(this.onnxInputVector);

            // parse the model output into an ordered list of the top-N predictions
            var results = this.outputParser.GetTopNLabeledPredictions(outputVector, this.configuration.NumberOfPredictions);

            // post the results
            this.Out.Post(results, envelope.OriginatingTime);
        }

        /// <summary>
        /// Constructs the input vector for the ImageNet model for a specified image.
        /// </summary>
        /// <param name="sharedImage">The image to construct the input vector for.</param>
        private void ConstructOnnxInputVector(Shared<Image> sharedImage)
        {
            var inputImage = sharedImage.Resource;
            var inputWidth = sharedImage.Resource.Width;
            var inputHeight = sharedImage.Resource.Height;

            // crop a center square
            var squareSize = Math.Min(inputWidth, inputHeight);
            using var squareImage = ImagePool.GetOrCreate(squareSize, squareSize, sharedImage.Resource.PixelFormat);
            if (inputWidth > inputHeight)
            {
                inputImage.Crop(squareImage.Resource, (inputWidth - squareSize) / 2, 0, squareSize, squareSize);
            }
            else
            {
                inputImage.Crop(squareImage.Resource, 0, (inputHeight - squareSize) / 2, squareSize, squareSize);
            }

            // resize the image to 224 x 224
            using var resizedImage = ImagePool.GetOrCreate(224, 224, sharedImage.Resource.PixelFormat);
            squareImage.Resource.Resize(resizedImage.Resource, 224, 224, SamplingMode.Bilinear);

            // if the pixel format does not match, do a conversion before extracting the bytes
            var bytes = default(byte[]);
            if (sharedImage.Resource.PixelFormat != PixelFormat.BGR_24bpp)
            {
                using var reformattedImage = ImagePool.GetOrCreate(224, 224, PixelFormat.BGR_24bpp);
                resizedImage.Resource.CopyTo(reformattedImage.Resource);
                bytes = reformattedImage.Resource.ReadBytes(3 * 224 * 224);
            }
            else
            {
                // get the bytes
                bytes = resizedImage.Resource.ReadBytes(3 * 224 * 224);
            }

            // Now populate the onnxInputVector float array / tensor by normalizing
            // using mean = [0.485, 0.456, 0.406] and std = [0.229, 0.224, 0.225].
            int fi = 0;

            // first the red bytes
            for (int i = 2; i < bytes.Length; i += 3)
            {
                this.onnxInputVector[fi++] = ((bytes[i] / 255.0f) - 0.485f) / 0.229f;
            }

            // then the green bytes
            for (int i = 1; i < bytes.Length; i += 3)
            {
                this.onnxInputVector[fi++] = ((bytes[i] / 255.0f) - 0.456f) / 0.224f;
            }

            // then the blue bytes
            for (int i = 0; i < bytes.Length; i += 3)
            {
                this.onnxInputVector[fi++] = ((bytes[i] / 255.0f) - 0.406f) / 0.225f;
            }
        }
    }
}

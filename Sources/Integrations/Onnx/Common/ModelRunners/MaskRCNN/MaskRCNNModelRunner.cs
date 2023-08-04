// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that runs the Mask R-CNN object masking and detection model.
    /// </summary>
    /// <remarks>
    /// This class implements a \psi component that runs the Mask R-CNN ONNX model.
    /// It uses an input stream of \psi images, which should be of sizes matching
    /// the configured width/height. For best performance, images width and height
    /// should be between 800 and 1312, inclusive, and divisible by 32. This
    /// component parses the outputs into a list of <see cref="MaskRCNNDetection"/>
    /// instances, corresponding to each object detection result. The component
    /// requires the filename where the Mask R-CNN model can be found. The model
    /// is available here in the ONNX Model Zoo at:
    /// https://github.com/onnx/models/blob/main/vision/object_detection_segmentation/mask-rcnn/model/MaskRCNN-10.onnx .
    /// and the classes file is available here:
    /// https://github.com/onnx/models/blob/main/vision/object_detection_segmentation/mask-rcnn/dependencies/coco_classes.txt .
    /// </remarks>
    public class MaskRCNNModelRunner : ConsumerProducer<Shared<Image>, MaskRCNNDetectionResults>, IDisposable
    {
        private readonly string[] classes;
        private readonly float confidenceThreshold;
        private readonly int imageWidth;
        private readonly int imageHeight;
        private readonly Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator;

        private MaskRCNNOnnxModel onnxModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskRCNNModelRunner"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="modelFileName">The name of the model file (see remarks on <see cref="MaskRCNNModelRunner" />).</param>
        /// <param name="classesFileName">The name of the file containing class names (see remarks on <see cref="MaskRCNNModelRunner" />).</param>
        /// <param name="confidenceThreshold">The confidence threshold to use in filtering results.</param>
        /// <param name="imageWidth">Image width to resize to before sending to Mask R-CNN (should be between 800 and 1312, inclusive, and divisible by 32).</param>
        /// <param name="imageHeight">Image height to resize to before sending to Mask R-CNN (should be between 800 and 1312, inclusive, and divisible by 32).</param>
        /// <param name="gpuDeviceId">The GPU device ID to run execution on, or null to run on CPU.</param>
        /// <param name="sharedImageAllocator">An optional image allocator used to create shared images for passing to MaskRCNN.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.ModelRunners.Gpu library instead of Microsoft.Psi.Onnx.ModelRunners.Cpu, and set
        /// the value of the <pararef name="gpuDeviceId"/> parameter to a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        public MaskRCNNModelRunner(
            Pipeline pipeline,
            string modelFileName,
            string classesFileName,
            float confidenceThreshold,
            int imageWidth,
            int imageHeight,
            int? gpuDeviceId = null,
            Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null,
            string name = nameof(MaskRCNNModelRunner))
            : base(pipeline, name)
        {
            this.classes = File.ReadAllLines(classesFileName);
            this.confidenceThreshold = confidenceThreshold;
            this.imageWidth = imageWidth;
            this.imageHeight = imageHeight;
            this.sharedImageAllocator = sharedImageAllocator ??= ImagePool.GetOrCreate;
            this.onnxModel = new MaskRCNNOnnxModel(imageWidth, imageHeight, modelFileName, gpuDeviceId);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskRCNNModelRunner"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="sharedImageAllocator">Optional image allocator to create new shared image.</param>
        /// <param name="name">An optional name for the component.</param>
        public MaskRCNNModelRunner(
            Pipeline pipeline,
            MaskRCNNModelConfiguration configuration,
            Func<int, int, PixelFormat, Shared<Image>> sharedImageAllocator = null,
            string name = nameof(MaskRCNNModelRunner))
            : this(
                  pipeline,
                  configuration.ModelFileName,
                  configuration.ClassesFileName,
                  configuration.ConfidenceThreshold,
                  configuration.ImageWidth,
                  configuration.ImageHeight,
                  configuration.GpuDeviceId,
                  sharedImageAllocator,
                  name)
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.onnxModel.Dispose();
            this.onnxModel = null;
        }

        /// <inheritdoc/>
        protected override void Receive(Shared<Image> data, Envelope envelope)
        {
            var input = this.ConstructOnnxInput(data);
            var output = this.onnxModel.GetPrediction(input);
            var detections = MaskRCNNModelOutputParser.Extract(
                output.Scores,
                output.Boxes,
                (float)data.Resource.Width / (float)this.imageWidth,
                (float)data.Resource.Height / (float)this.imageHeight,
                output.Labels,
                output.Masks,
                this.classes,
                this.confidenceThreshold);
            var results = new MaskRCNNDetectionResults(detections, data.Resource.Width, data.Resource.Height);
            this.Out.Post(results, envelope.OriginatingTime);
        }

        /// <summary>
        /// Constructs the input vectors for the Mask R-CNN model for a specified image.
        /// </summary>
        /// <param name="sharedImage">The shared image to construct the input vector for.</param>
        private float[] ConstructOnnxInput(Shared<Image> sharedImage)
        {
            var size = 3 * this.imageWidth * this.imageHeight;

            float[] ConstructInputVectorFromBytes(byte[] bytes)
            {
                var inputVector = new float[size];
                int j = 0;

                void CopyChannel(int offset, float normalization)
                {
                    for (int i = offset; i < bytes.Length; i += 3)
                    {
                        inputVector[j++] = bytes[i] - normalization;
                    }
                }

                CopyChannel(2, 102.9801f); // blue
                CopyChannel(1, 115.9465f); // green
                CopyChannel(0, 122.7717f); // red

                return inputVector;
            }

            float[] ConstructInputVectorFromImage(Image image)
            {
                if (image.Width != this.imageWidth || image.Height != this.imageHeight)
                {
                    // resize to match configured input size
                    using var resizedSharedImage = this.sharedImageAllocator(this.imageWidth, this.imageHeight, PixelFormat.BGR_24bpp);
                    image.Resize(resizedSharedImage.Resource, this.imageWidth, this.imageHeight, SamplingMode.Bicubic);
                    return ConstructInputVectorFromBytes(resizedSharedImage.Resource.ReadBytes(size));
                }

                return ConstructInputVectorFromBytes(image.ReadBytes(size));
            }

            var inputImage = sharedImage.Resource;
            if (inputImage.PixelFormat != PixelFormat.BGR_24bpp)
            {
                // convert before extracting bytes
                using var reformattedImage = this.sharedImageAllocator(inputImage.Width, inputImage.Height, PixelFormat.BGR_24bpp);
                inputImage.CopyTo(reformattedImage.Resource);
                return ConstructInputVectorFromImage(reformattedImage.Resource);
            }

            return ConstructInputVectorFromImage(inputImage);
        }
    }
}

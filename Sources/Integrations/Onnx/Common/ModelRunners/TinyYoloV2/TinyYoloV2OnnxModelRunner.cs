// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that runs the Tiny Yolo V2 object detection model.
    /// </summary>
    /// <remarks>
    /// This class implements a \psi component that runs the Tiny Yolo V2
    /// ONNX model. It uses an input stream of \psi images, and internally
    /// converts the received images by applying a center-crop and rescaling
    /// the images to a 416 x 416 format expected by the model. It also
    /// parses the model outputs into a list of <see cref="TinyYoloV2Detection"/>
    /// instances, corresponding to each object detection result. The
    /// component requires the filename where the Tiny Yolo V2 model can
    /// be loaded from. The model is available in the ONNX Model Zoo at
    /// https://github.com/onnx/models/raw/3d4b2c28f951064ab35c89d5f5c3ffe74a149e4b/vision/object_detection_segmentation/tiny-yolov2/model/tinyyolov2-8.onnx.
    /// </remarks>
    public class TinyYoloV2OnnxModelRunner : ConsumerProducer<Shared<Image>, List<TinyYoloV2Detection>>, IDisposable
    {
        private readonly float[] onnxInputVector = new float[3 * 416 * 416];
        private OnnxModel onnxModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="TinyYoloV2OnnxModelRunner"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="modelFileName">The name of the model.</param>
        /// <param name="gpuDeviceId">The GPU device ID to run execution on, or null to run on CPU.</param>
        /// <param name="name">An optional name for the component.</param>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.ModelRunners.Gpu library instead of Microsoft.Psi.Onnx.ModelRunners.Cpu, and set
        /// the value of the <pararef name="gpuDeviceId"/> parameter to a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        public TinyYoloV2OnnxModelRunner(Pipeline pipeline, string modelFileName, int? gpuDeviceId = null, string name = nameof(TinyYoloV2OnnxModelRunner))
            : base(pipeline, name)
        {
            // create an ONNX model, with a configuration that matches the structure
            // of the Tiny Yolo V2 model
            this.onnxModel = new OnnxModel(new OnnxModelConfiguration()
            {
                ModelFileName = modelFileName,
                InputVectorName = "image",
                InputVectorSize = 3 * 416 * 416,
                OutputVectorName = "grid",
                GpuDeviceId = gpuDeviceId,
            });
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
            // construct the ONNX model input vector (stored in this.onnxInputVector)
            // based on the incoming image
            this.ConstructOnnxInputVector(data);

            // run the model over the input vector
            var outputVector = this.onnxModel.GetPrediction(this.onnxInputVector);

            // parse the model output into a set of detection results
            var detections = TinyYoloV2ModelOutputParser.ExtractBoundingBoxes(outputVector);

            // convert back based on the cropping performed
            var results = this.ConvertBoundingBoxesToImageSpace(detections, data);

            // post the results
            this.Out.Post(results, envelope.OriginatingTime);
        }

        /// <summary>
        /// Constructs the input vector for the Tiny Yolo V2 model for a specified image.
        /// </summary>
        /// <param name="sharedImage">The shared image to construct the input vector for.</param>
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

            // resize the image to 416 x 416
            using var resizedImage = ImagePool.GetOrCreate(416, 416, sharedImage.Resource.PixelFormat);
            squareImage.Resource.Resize(resizedImage.Resource, 416, 416, SamplingMode.Bilinear);

            // if the pixel format does not match, do a conversion before extracting the bytes
            var bytes = default(byte[]);
            if (sharedImage.Resource.PixelFormat != PixelFormat.BGR_24bpp)
            {
                using var reformattedImage = ImagePool.GetOrCreate(416, 416, PixelFormat.BGR_24bpp);
                resizedImage.Resource.CopyTo(reformattedImage.Resource);
                bytes = reformattedImage.Resource.ReadBytes(3 * 416 * 416);
            }
            else
            {
                // get the bytes
                bytes = resizedImage.Resource.ReadBytes(3 * 416 * 416);
            }

            // now populate the onnxInputVector float array / tensor
            int fi = 0;

            // first the blue bytes
            for (int i = 2; i < bytes.Length; i += 3)
            {
                this.onnxInputVector[fi++] = bytes[i];
            }

            // then the green bytes
            for (int i = 1; i < bytes.Length; i += 3)
            {
                this.onnxInputVector[fi++] = bytes[i];
            }

            // then the red bytes
            for (int i = 0; i < bytes.Length; i += 3)
            {
                this.onnxInputVector[fi++] = bytes[i];
            }
        }

        /// <summary>
        /// Converts the detections expressed in the Tiny Yolo 416x416 space back to the original
        /// image space.
        /// </summary>
        /// <param name="detections">The set of detections in Tiny Yolo 416x416 space.</param>
        /// <param name="sharedImage">The original image.</param>
        /// <returns>The detections converted to the original image space.</returns>
        private List<TinyYoloV2Detection> ConvertBoundingBoxesToImageSpace(List<TinyYoloV2Detection> detections, Shared<Image> sharedImage)
        {
            var inputImage = sharedImage.Resource;
            var inputWidth = sharedImage.Resource.Width;
            var inputHeight = sharedImage.Resource.Height;
            var squareSize = Math.Min(inputWidth, inputHeight);
            var scaleFactor = squareSize / 416.0f;

            return detections.Select(detection =>
            {
                if (inputWidth > inputHeight)
                {
                    return new TinyYoloV2Detection()
                    {
                        BoundingBox = new System.Drawing.RectangleF(
                            (inputWidth - squareSize) / 2 + detection.BoundingBox.X * scaleFactor,
                            detection.BoundingBox.Y * scaleFactor,
                            detection.BoundingBox.Width * scaleFactor,
                            detection.BoundingBox.Height * scaleFactor),
                        Label = detection.Label,
                        Confidence = detection.Confidence,
                    };
                }
                else
                {
                    return new TinyYoloV2Detection()
                    {
                        BoundingBox = new System.Drawing.RectangleF(
                            detection.BoundingBox.X * scaleFactor,
                            (inputHeight - squareSize) / 2 + detection.BoundingBox.Y * scaleFactor,
                            detection.BoundingBox.Width * scaleFactor,
                            detection.BoundingBox.Height * scaleFactor),
                        Label = detection.Label,
                        Confidence = detection.Confidence,
                    };
                }
            }).ToList();
        }
    }
}

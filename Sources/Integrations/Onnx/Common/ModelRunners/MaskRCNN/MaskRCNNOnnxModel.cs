// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ML;
    using Microsoft.ML.Data;
    using Microsoft.ML.Transforms.Onnx;

    /// <summary>
    /// Implements an ONNX model for Mask R-CNN.
    /// </summary>
    public class MaskRCNNOnnxModel : IDisposable
    {
        private const string BOXES = "6568";
        private const string LABELS = "6570";
        private const string SCORES = "6572";
        private const string MASKS = "6887";

        private readonly MLContext context = new ();
        private readonly SchemaDefinition schemaDefinition;
        private OnnxTransformer onnxTransformer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskRCNNOnnxModel"/> class.
        /// </summary>
        /// <param name="imageWidth">Input image width.</param>
        /// <param name="imageHeight">Input image height.</param>
        /// <param name="modelFileName">Model file name.</param>
        /// <param name="gpuDeviceId">GPU device ID to run execution on, or null to run on CPU.</param>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.Gpu library instead of Microsoft.Psi.Onnx.Cpu, and pass
        /// a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        public MaskRCNNOnnxModel(int imageWidth, int imageHeight, string modelFileName, int? gpuDeviceId = null)
        {
            //// REVIEW: note the ColumnType below is (float, 3, height, width), while the plain OnnxModel is simply (float, size)
            ////         the rest is very similar to OnnxModel

            this.schemaDefinition = SchemaDefinition.Create(typeof(OnnxInputVector));
            this.schemaDefinition[nameof(OnnxInputVector.Vector)].ColumnType = new VectorDataViewType(NumberDataViewType.Single, 3, imageHeight, imageWidth);
            this.schemaDefinition[nameof(OnnxInputVector.Vector)].ColumnName = "image";

            var onnxEmptyInputDataView = this.context.Data.LoadFromEnumerable(new List<OnnxInputVector>(), this.schemaDefinition);
            var shapeDictionary = new Dictionary<string, int[]>() { { "image", new int[] { 3, imageHeight, imageWidth } } };

            var scoringEstimator =
                this.context.Transforms.ApplyOnnxModel(
                    modelFile: modelFileName,
                    inputColumnNames: new[] { "image" },
                    outputColumnNames: new[] { BOXES, LABELS, SCORES, MASKS },
                    shapeDictionary: shapeDictionary,
                    gpuDeviceId: gpuDeviceId,
                    fallbackToCpu: false);
            this.onnxTransformer = scoringEstimator.Fit(onnxEmptyInputDataView);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.onnxTransformer?.Dispose();
            this.onnxTransformer = null;
        }

        /// <summary>
        /// Runs the ONNX model on an input vector.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <returns>The set of output vectors produced by the ONNX model.</returns>
        public (float[] Scores, float[] Boxes, long[] Labels, float[] Masks) GetPrediction(float[] input)
        {
            //// REVIEW: note that OnnxModel is merely float[] -> float[]

            // construct a data view over the input
            var onnxInput = new List<OnnxInputVector> { new OnnxInputVector { Vector = input } };
            var onnxInputDataView = this.context.Data.LoadFromEnumerable(onnxInput, this.schemaDefinition);

            // apply the onnxTransformer and extract the results
            var prediction = this.onnxTransformer.Transform(onnxInputDataView);
            var scores = prediction.GetColumn<float[]>(SCORES).ToArray()[0];
            var boxes = prediction.GetColumn<float[]>(BOXES).ToArray()[0];
            var labels = prediction.GetColumn<long[]>(LABELS).ToArray()[0];
            var masks = prediction.GetColumn<float[]>(MASKS).ToArray()[0];
            return (scores, boxes, labels, masks);
        }

        /// <summary>
        /// Represents a vector input for an ONNX model.
        /// </summary>
        internal class OnnxInputVector
        {
            /// <summary>
            /// Gets or sets the vector data.
            /// </summary>
            public float[] Vector { get; set; }
        }
    }
}

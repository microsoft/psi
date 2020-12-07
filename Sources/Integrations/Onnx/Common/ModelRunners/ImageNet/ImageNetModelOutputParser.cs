// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi;

    /// <summary>
    /// Internal class that parses the outputs from the ImageNet model into
    /// a set of image classification results.
    /// </summary>
    internal class ImageNetModelOutputParser
    {
        private readonly string[] labels;
        private readonly int maxPredictions;
        private readonly bool applySoftmax;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageNetModelOutputParser"/> class.
        /// </summary>
        /// <param name="imageClassesFile">The path to the file containing the list of 1000 ImageNet classes.</param>
        /// <param name="maxPredictions">The maximum number of predictions to return.</param>
        /// <param name="applySoftmax">Whether the softmax function should be applied to the raw model output.</param>
        /// <remarks>
        /// The file referenced by <paramref name="imageClassesFile"/> may be downloaded from the following location:
        /// https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt.
        /// </remarks>
        public ImageNetModelOutputParser(string imageClassesFile, int maxPredictions, bool applySoftmax)
        {
            this.labels = File.ReadAllLines(imageClassesFile);
            if (this.labels.Length != 1000)
            {
                throw new ArgumentException($"The file {imageClassesFile} does not appear to be in the correct format. This file should contain exactly 1000 lines representing an ordered list of the 1000 ImageNet classes.");
            }

            this.maxPredictions = maxPredictions;
            this.applySoftmax = applySoftmax;
        }

        /// <summary>
        /// Gets the predictions from the model output.
        /// </summary>
        /// <param name="modelOutput">The model output vector of class probabilities.</param>
        /// <returns>A list of the top-N predictions, in descending probability order.</returns>
        public List<LabeledPrediction> GetPredictions(float[] modelOutput)
        {
            return GetTopResults(this.applySoftmax ? Softmax(modelOutput) : modelOutput, this.maxPredictions)
                .Select(c => new LabeledPrediction { Label = this.labels[c.Index], Confidence = c.Value })
                .ToList();
        }

        private static IEnumerable<(int Index, float Value)> GetTopResults(IEnumerable<float> predictedClasses, int count)
        {
            return predictedClasses
                .Select((predictedClass, index) => (Index: index, Value: predictedClass))
                .OrderByDescending(result => result.Value)
                .Take(count);
        }

        private static IEnumerable<float> Softmax(IEnumerable<float> values)
        {
            var maxVal = values.Max();
            var exp = values.Select(v => Math.Exp(v - maxVal));
            var sumExp = exp.Sum();

            return exp.Select(v => (float)(v / sumExp));
        }
    }
}

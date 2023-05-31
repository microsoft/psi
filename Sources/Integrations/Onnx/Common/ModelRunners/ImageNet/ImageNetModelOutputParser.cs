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
    public class ImageNetModelOutputParser
    {
        /// <summary>
        /// Gets the classes count.
        /// </summary>
        public static readonly int ClassesCount = 1000;

        private readonly string[] labels;
        private readonly bool applySoftmax;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageNetModelOutputParser"/> class.
        /// </summary>
        /// <param name="imageClassesFile">The path to the file containing the list of 1000 ImageNet classes.</param>
        /// <param name="applySoftmax">Whether the softmax function should be applied to the raw model output.</param>
        /// <remarks>
        /// The file referenced by <paramref name="imageClassesFile"/> may be downloaded from the following location:
        /// https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt.
        /// </remarks>
        public ImageNetModelOutputParser(string imageClassesFile, bool applySoftmax)
        {
            this.labels = File.ReadAllLines(imageClassesFile);
            if (this.labels.Length != ClassesCount)
            {
                throw new ArgumentException($"The file {imageClassesFile} does not appear to be in the correct format. This file should contain exactly 1000 lines representing an ordered list of the 1000 ImageNet classes.");
            }

            this.applySoftmax = applySoftmax;
        }

        /// <summary>
        /// Gets the list of predictions from the model output.
        /// </summary>
        /// <param name="modelOutput">The model output vector of class probabilities.</param>
        /// <returns>The unsorted list of predictions.</returns>
        public List<LabeledPrediction> GetLabeledPredictions(float[] modelOutput)
            => GetResults(this.applySoftmax ? Softmax(modelOutput) : modelOutput)
                .Select(c => new LabeledPrediction { Label = this.labels[c.Index], Confidence = c.Value })
                .ToList();

        /// <summary>
        /// Gets the top-N predictions from the model output.
        /// </summary>
        /// <param name="modelOutput">The model output vector of class probabilities.</param>
        /// <param name="count">The number of top-predictions to return.</param>
        /// <returns>A list of the top-N predictions, in descending probability order.</returns>
        public List<LabeledPrediction> GetTopNLabeledPredictions(float[] modelOutput, int count)
            => GetTopNResults(this.applySoftmax ? Softmax(modelOutput) : modelOutput, count)
                .Select(c => new LabeledPrediction { Label = this.labels[c.Index], Confidence = c.Value })
                .ToList();

        private static IEnumerable<(int Index, float Value)> GetResults(IEnumerable<float> predictedClasses)
            => predictedClasses.Select((predictedClass, index) => (Index: index, Value: predictedClass));

        private static IEnumerable<(int Index, float Value)> GetTopNResults(IEnumerable<float> predictedClasses, int count)
            => predictedClasses
                .Select((predictedClass, index) => (Index: index, Value: predictedClass))
                .OrderByDescending(result => result.Value)
                .Take(count);

        private static IEnumerable<float> Softmax(IEnumerable<float> values)
        {
            var maxVal = values.Max();
            var exp = values.Select(v => Math.Exp(v - maxVal));
            var sumExp = exp.Sum();

            return exp.Select(v => (float)(v / sumExp));
        }
    }
}

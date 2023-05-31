// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Microsoft.Psi;

    /// <summary>
    /// Internal static class that parses the outputs from the Tiny Yolo V2 model into
    /// a set of object detection results.
    /// </summary>
    /// This code was adapted from the ML.NET tutorial that explains how to run Tiny Yolo V2.
    /// https://github.com/dotnet/machinelearning-samples/tree/ffac232a1d599903b8591b68dabd237af48f462f/samples/csharp/getting-started/DeepLearning_ObjectDetection_Onnx
    /// https://docs.microsoft.com/en-us/dotnet/machine-learning/tutorials/object-detection-onnx
    internal static class TinyYoloV2ModelOutputParser
    {
        private static readonly int RowCount = 13;
        private static readonly int ColumnCount = 13;
        private static readonly int BoxesPerCell = 5;
        private static readonly int BoxInfoFeatureCount = 5;
        private static readonly int ClassCount = 20;
        private static readonly float CellWidth = 32;
        private static readonly float CellHeight = 32;
        private static readonly int ChannelStride = RowCount * ColumnCount;

        private static readonly float[] Anchors = new float[]
        {
            1.08F, 1.19F, 3.42F, 4.41F, 6.63F, 11.38F, 9.42F, 5.11F, 16.62F, 10.52F,
        };

        private static readonly string[] Labels = new string[]
        {
            "aeroplane",
            "bicycle",
            "bird",
            "boat",
            "bottle",
            "bus",
            "car",
            "cat",
            "chair",
            "cow",
            "diningtable",
            "dog",
            "horse",
            "motorbike",
            "person",
            "pottedplant",
            "sheep",
            "sofa",
            "train",
            "tvmonitor",
        };

        /// <summary>
        /// Parses the model outputs into a list of object detection results.
        /// </summary>
        /// <param name="modelOutput">The vector for the model output.</param>
        /// <param name="threshold">The confidence threshold to use in filtering results.</param>
        /// <returns>The list of detection results.</returns>
        internal static List<TinyYoloV2Detection> ExtractBoundingBoxes(float[] modelOutput, float threshold = .3F)
        {
            var results = new List<TinyYoloV2Detection>();

            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    for (int box = 0; box < BoxesPerCell; box++)
                    {
                        var channel = box * (ClassCount + BoxInfoFeatureCount);

                        var boundingBoxDimensions = ExtractBoundingBoxDimensions(modelOutput, row, column, channel);

                        float confidence = GetConfidence(modelOutput, row, column, channel);

                        var mappedBoundingBox = MapBoundingBoxToCell(row, column, box, boundingBoxDimensions);

                        if (confidence < threshold)
                        {
                            continue;
                        }

                        float[] predictedClasses = ExtractClasses(modelOutput, row, column, channel);

                        var (topResultIndex, topResultScore) = GetTopResult(predictedClasses);
                        var topScore = topResultScore * confidence;

                        if (topScore < threshold)
                        {
                            continue;
                        }

                        results.Add(new TinyYoloV2Detection()
                        {
                            BoundingBox = new RectangleF
                            {
                                X = mappedBoundingBox.X - mappedBoundingBox.Width / 2,
                                Y = mappedBoundingBox.Y - mappedBoundingBox.Height / 2,
                                Width = mappedBoundingBox.Width,
                                Height = mappedBoundingBox.Height,
                            },

                            Confidence = topScore,
                            Label = Labels[topResultIndex],
                        });
                    }
                }
            }

            return FilterBoundingBoxes(results, 5, .5F);
        }

        private static List<TinyYoloV2Detection> FilterBoundingBoxes(List<TinyYoloV2Detection> detections, int limit, float threshold)
        {
            var activeCount = detections.Count;
            var isActiveBoxes = new bool[detections.Count];

            for (int i = 0; i < isActiveBoxes.Length; i++)
            {
                isActiveBoxes[i] = true;
            }

            var sortedBoxes = detections.Select((b, i) => new { Box = b, Index = i })
                                .OrderByDescending(b => b.Box.Confidence)
                                .ToList();

            var results = new List<TinyYoloV2Detection>();

            for (int i = 0; i < detections.Count; i++)
            {
                if (isActiveBoxes[i])
                {
                    var boxA = sortedBoxes[i].Box;
                    results.Add(boxA);

                    if (results.Count >= limit)
                    {
                        break;
                    }

                    for (var j = i + 1; j < detections.Count; j++)
                    {
                        if (isActiveBoxes[j])
                        {
                            var boxB = sortedBoxes[j].Box;

                            if (IntersectionOverUnion(boxA.BoundingBox, boxB.BoundingBox) > threshold)
                            {
                                isActiveBoxes[j] = false;
                                activeCount--;

                                if (activeCount <= 0)
                                {
                                    break;
                                }
                            }
                        }
                    }

                    if (activeCount <= 0)
                    {
                        break;
                    }
                }
            }

            return results;
        }

        private static RectangleF ExtractBoundingBoxDimensions(float[] modelOutput, int x, int y, int channel)
        {
            return new RectangleF
            {
                X = modelOutput[GetOffset(x, y, channel)],
                Y = modelOutput[GetOffset(x, y, channel + 1)],
                Width = modelOutput[GetOffset(x, y, channel + 2)],
                Height = modelOutput[GetOffset(x, y, channel + 3)],
            };
        }

        private static int GetOffset(int x, int y, int channel)
        {
            return (channel * ChannelStride) + (y * ColumnCount) + x;
        }

        private static float GetConfidence(float[] modelOutput, int x, int y, int channel)
        {
            return Sigmoid(modelOutput[GetOffset(x, y, channel + 4)]);
        }

        private static float Sigmoid(float value)
        {
            var k = (float)Math.Exp(value);
            return k / (1.0f + k);
        }

        private static RectangleF MapBoundingBoxToCell(int x, int y, int box, RectangleF boxDimensions)
        {
            return new RectangleF
            {
                X = (x + Sigmoid(boxDimensions.X)) * CellWidth,
                Y = (y + Sigmoid(boxDimensions.Y)) * CellHeight,
                Width = (float)Math.Exp(boxDimensions.Width) * CellWidth * Anchors[box * 2],
                Height = (float)Math.Exp(boxDimensions.Height) * CellHeight * Anchors[box * 2 + 1],
            };
        }

        private static float[] ExtractClasses(float[] modelOutput, int x, int y, int channel)
        {
            float[] predictedClasses = new float[ClassCount];
            int predictedClassOffset = channel + BoxInfoFeatureCount;
            for (int predictedClass = 0; predictedClass < ClassCount; predictedClass++)
            {
                predictedClasses[predictedClass] = modelOutput[GetOffset(x, y, predictedClass + predictedClassOffset)];
            }

            return Softmax(predictedClasses);
        }

        private static float[] Softmax(float[] values)
        {
            var maxVal = values.Max();
            var exp = values.Select(v => Math.Exp(v - maxVal));
            var sumExp = exp.Sum();

            return exp.Select(v => (float)(v / sumExp)).ToArray();
        }

        private static (int, float) GetTopResult(float[] predictedClasses)
        {
            return predictedClasses
                .Select((predictedClass, index) => (Index: index, Value: predictedClass))
                .OrderByDescending(result => result.Value)
                .First();
        }

        private static float IntersectionOverUnion(RectangleF boundingBoxA, RectangleF boundingBoxB)
        {
            var areaA = boundingBoxA.Width * boundingBoxA.Height;

            if (areaA <= 0)
            {
                return 0;
            }

            var areaB = boundingBoxB.Width * boundingBoxB.Height;

            if (areaB <= 0)
            {
                return 0;
            }

            var minX = Math.Max(boundingBoxA.Left, boundingBoxB.Left);
            var minY = Math.Max(boundingBoxA.Top, boundingBoxB.Top);
            var maxX = Math.Min(boundingBoxA.Right, boundingBoxB.Right);
            var maxY = Math.Min(boundingBoxA.Bottom, boundingBoxB.Bottom);

            var intersectionArea = Math.Max(maxY - minY, 0) * Math.Max(maxX - minX, 0);

            return intersectionArea / (areaA + areaB - intersectionArea);
        }
    }
}

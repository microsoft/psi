// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Onnx
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Onnx;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Test.Psi.Common;

    [TestClass]
    public class TinyYoloV2Tester
    {
        [TestMethod]
        [Timeout(60000)]
        public void TinyYoloV2ObjectDetectionTest()
        {
            // This test expects a set of resources at a path specified via the PsiTestResources environment
            // variable. Specifically, it expects a subfolder TinyYoloV2 containg the file:
            // - TinyYolo2_model.onnx: is the model, available from the onnx model zoo:
            //     https://github.com/onnx/models/raw/3d4b2c28f951064ab35c89d5f5c3ffe74a149e4b/vision/object_detection_segmentation/tiny-yolov2/model/tinyyolov2-8.onnx,
            //     or in the ML.NET samples repo,
            //     https://github.com/dotnet/machinelearning-samples/tree/ffac232a1d599903b8591b68dabd237af48f462f/samples/csharp/getting-started/DeepLearning_ObjectDetection_Onnx
            //     in the ObjectDetectionConsoleApp/assets/Model folder
            // and a subfolder TestImages containing the file:
            // - image1.jpg: is the test image, available from the ML.NET samples repo, 
            //     https://github.com/dotnet/machinelearning-samples/tree/ffac232a1d599903b8591b68dabd237af48f462f/samples/csharp/getting-started/DeepLearning_ObjectDetection_Onnx
            //     in the ObjectDetectionConsoleApp/assets/images folder

            if (TestRunner.TestResourcesPath != null)
            {
                var yoloModel = Path.Combine(TestRunner.TestResourcesPath, "TinyYoloV2", "TinyYolo2_model.onnx");
                var testImage = Path.Combine(TestRunner.TestResourcesPath, "TestImages", "image1.jpg");

                var labels = new string[] { "car", "car", "person", "car", "car" };
                var confidences = new float[] { 0.972779453f, 0.7202784f, 0.512937546f, 0.476578265f, 0.452380836f };
                var boxX = new float[] { 77.54233f, 145.92926f, 614.441833f, 252.64949f, 241.590149f };
                var boxY = new float[] { 0.00280600321f, 46.5406f, 129.180725f, 96.3321457f, 109.464722f };

                var labelsResults = new List<string>();
                var confidencesResults = new List<float>();
                var boxXResults = new List<float>();
                var boxYResults = new List<float>();

                var image = Shared.Create(Image.FromBitmap(new System.Drawing.Bitmap(testImage)));

                var p = Pipeline.Create();

                var tinyYolo = new TinyYoloV2OnnxModelRunner(p, yoloModel);                
                Generators.Return(p, image).PipeTo(tinyYolo)
                    .Do(list =>
                    {
                        foreach (var det in list)
                        {
                            labelsResults.Add(det.Label);
                            confidencesResults.Add(det.Confidence);
                            boxXResults.Add(det.BoundingBox.X);
                            boxYResults.Add(det.BoundingBox.Y);
                        }
                    });

                p.Run();

                for (int i = 0; i < confidences.Length; i++)
                {
                    Assert.AreEqual(labels[i], labelsResults[i]);
                    Assert.AreEqual(confidences[i], confidencesResults[i], 0.000001);
                    Assert.AreEqual(boxX[i], boxXResults[i], 0.001);
                    Assert.AreEqual(boxY[i], boxYResults[i], 0.001);
                }
            }
            else
            {
                Assert.Inconclusive($"Warning: Test {nameof(TinyYoloV2ObjectDetectionTest)} not run because 'PsiTestResources' environment variable not found.");
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Onnx
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Onnx;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Test.Psi.Common;

    [TestClass]
    public class ImageNetTester
    {
        [TestMethod]
        [Timeout(60000)]
        public void ResNet50Caffe2ImageClassificationTest()
        {
            // This test expects a set of resources at the path specified by the PsiTestResources environment
            // variable. Specifically, it expects a subfolder named ResNet containing the file:
            // - resnet50-caffe2-v1-7.onnx: this is the model, available from the onnx model zoo:
            //     https://github.com/onnx/models/raw/b9a54e89508f101a1611cd64f4ef56b9cb62c7cf/vision/classification/resnet/model/resnet50-caffe2-v1-7.onnx
            //     (licensed at that commit under the Apache 2.0 License)
            // a subfolder ImageNet containing the file:
            // - synset.txt: this is a file containing an ordered list of the 1000 ImageNet classes, available from the onnx model zoo:
            //     https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt
            // and a subfolder TestImages containing the file:
            // - image1.jpg: this is the test image, available from the ML.NET samples repo, 
            //     https://github.com/dotnet/machinelearning-samples/tree/ffac232a1d599903b8591b68dabd237af48f462f/samples/csharp/getting-started/DeepLearning_ObjectDetection_Onnx
            //     in the ObjectDetectionConsoleApp/assets/images folder

            if (TestRunner.TestResourcesPath != null)
            {
                string modelFile = Path.Combine(TestRunner.TestResourcesPath, "ResNet", "resnet50-caffe2-v1-7.onnx");
                string imageClassesFile = Path.Combine(TestRunner.TestResourcesPath, "ImageNet", "synset.txt");
                string testImageFile = Path.Combine(TestRunner.TestResourcesPath, "TestImages", "image1.jpg");

                // create a configuration for the ResNet50Caffe2 model
                var modelConfig = ImageNetModelRunnerConfiguration.CreateResNet50Caffe2(modelFile, imageClassesFile);
                var testImage = Shared.Create(Image.FromBitmap(new System.Drawing.Bitmap(testImageFile)));

                this.ImageClassificationTest(
                    modelConfig,
                    testImage,
                    new[] {
                        ("n03100240 convertible", 0.834800065f),
                        ("n02814533 beach wagon, station wagon, wagon, estate car, beach waggon, station waggon, waggon", 0.05797491f),
                        ("n03930630 pickup, pickup truck", 0.0569703728f),
                        ("n03594945 jeep, landrover", 0.0181391146f),
                        ("n02974003 car wheel", 0.009720574f),
                    });
            }
            else
            {
                Assert.Inconclusive($"Test not run because 'PsiTestResources' environment variable not found.");
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void ResNet50V2ImageClassificationTest()
        {
            // This test expects a set of resources at the path specified by the PsiTestResources environment
            // variable. Specifically, it expects a subfolder named ResNet containing the file:
            // - resnet50-v2-7.onnx: this is the model, available from the onnx model zoo:
            //     https://github.com/onnx/models/raw/b9a54e89508f101a1611cd64f4ef56b9cb62c7cf/vision/classification/resnet/model/resnet50-v2-7.onnx
            //     (licensed at that commit under the Apache 2.0 License)
            // a subfolder ImageNet containing the file:
            // - synset.txt: this is a file containing an ordered list of the 1000 ImageNet classes, available from the onnx model zoo:
            //     https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt
            // and a subfolder TestImages containing the file:
            // - image1.jpg: this is the test image, available from the ML.NET samples repo, 
            //     https://github.com/dotnet/machinelearning-samples/tree/ffac232a1d599903b8591b68dabd237af48f462f/samples/csharp/getting-started/DeepLearning_ObjectDetection_Onnx
            //     in the ObjectDetectionConsoleApp/assets/images folder

            if (TestRunner.TestResourcesPath != null)
            {
                string modelFile = Path.Combine(TestRunner.TestResourcesPath, "ResNet", "resnet50-v2-7.onnx");
                string imageClassesFile = Path.Combine(TestRunner.TestResourcesPath, "ImageNet", "synset.txt");
                string testImageFile = Path.Combine(TestRunner.TestResourcesPath, "TestImages", "image1.jpg");

                // create a configuration for the ResNet50v2 model
                var modelConfig = ImageNetModelRunnerConfiguration.CreateResNet50v2(modelFile, imageClassesFile);
                var testImage = Shared.Create(Image.FromBitmap(new System.Drawing.Bitmap(testImageFile)));

                this.ImageClassificationTest(
                    modelConfig,
                    testImage,
                    new[] {
                        ("n03930630 pickup, pickup truck", 0.62304384f),
                        ("n03100240 convertible", 0.325131f),
                        ("n04461696 tow truck, tow car, wrecker", 0.0126348855f),
                        ("n02974003 car wheel", 0.010971873f),
                        ("n03459775 grille, radiator grille", 0.005041402f),
                    });
            }
            else
            {
                Assert.Inconclusive($"Test not run because 'PsiTestResources' environment variable not found.");
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void Vgg16ImageClassificationTest()
        {
            // This test expects a set of resources at the path specified by the PsiTestResources environment
            // variable. Specifically, it expects a subfolder named VGG containing the file:
            // - vgg16-7.onnx: this is the model, available from the onnx model zoo:
            //     https://github.com/onnx/models/raw/f884b33c3e2371952aad7ea091898f418c830fe5/vision/classification/vgg/model/vgg16-7.onnx
            //     (licensed at that commit under the Apache 2.0 License)
            // a subfolder ImageNet containing the file:
            // - synset.txt: this is a file containing an ordered list of the 1000 ImageNet classes, available from the onnx model zoo:
            //     https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt
            // and a subfolder TestImages containing the file:
            // - image1.jpg: this is the test image, available from the ML.NET samples repo, 
            //     https://github.com/dotnet/machinelearning-samples/tree/ffac232a1d599903b8591b68dabd237af48f462f/samples/csharp/getting-started/DeepLearning_ObjectDetection_Onnx
            //     in the ObjectDetectionConsoleApp/assets/images folder

            if (TestRunner.TestResourcesPath != null)
            {
                string modelFile = Path.Combine(TestRunner.TestResourcesPath, "VGG", "vgg16-7.onnx");
                string imageClassesFile = Path.Combine(TestRunner.TestResourcesPath, "ImageNet", "synset.txt");
                string testImageFile = Path.Combine(TestRunner.TestResourcesPath, "TestImages", "image1.jpg");

                // create a configuration for the VGG 16 model
                var modelConfig = ImageNetModelRunnerConfiguration.CreateVgg16(modelFile, imageClassesFile);
                var testImage = Shared.Create(Image.FromBitmap(new System.Drawing.Bitmap(testImageFile)));

                this.ImageClassificationTest(
                    modelConfig,
                    testImage,
                    new[] {
                        ("n03100240 convertible", 0.71793914f),
                        ("n03930630 pickup, pickup truck", 0.13835359f),
                        ("n02974003 car wheel", 0.027275257f),
                        ("n03459775 grille, radiator grille", 0.026480924f),
                        ("n03594945 jeep, landrover", 0.026157515f),
                    });
            }
            else
            {
                Assert.Inconclusive($"Test not run because 'PsiTestResources' environment variable not found.");
            }
        }

        [TestMethod]
        [Timeout(60000)]
        public void ShuffleNet2ImageClassificationTest()
        {
            // This test expects a set of resources at the path specified by the PsiTestResources environment
            // variable. Specifically, it expects a subfolder named ShuffleNet containing the file:
            // - shufflenet-v2-10: this is the model, available from the onnx model zoo:
            //     https://github.com/onnx/models/raw/b9a54e89508f101a1611cd64f4ef56b9cb62c7cf/vision/classification/shufflenet/model/shufflenet-v2-10.onnx
            //     (licensed at that commit under the BSD 3-Clause License)
            // a subfolder ImageNet containing the file:
            // - synset.txt: this is a file containing an ordered list of the 1000 ImageNet classes, available from the onnx model zoo:
            //     https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt
            // and a subfolder TestImages containing the file:
            // - image1.jpg: this is the test image, available from the ML.NET samples repo, 
            //     https://github.com/dotnet/machinelearning-samples/tree/ffac232a1d599903b8591b68dabd237af48f462f/samples/csharp/getting-started/DeepLearning_ObjectDetection_Onnx
            //     in the ObjectDetectionConsoleApp/assets/images folder

            if (TestRunner.TestResourcesPath != null)
            {
                string modelFile = Path.Combine(TestRunner.TestResourcesPath, "ShuffleNet", "shufflenet-v2-10.onnx");
                string imageClassesFile = Path.Combine(TestRunner.TestResourcesPath, "ImageNet", "synset.txt");
                string testImageFile = Path.Combine(TestRunner.TestResourcesPath, "TestImages", "image1.jpg");

                // create a configuration for the ShuffleNet V2 model
                var modelConfig = ImageNetModelRunnerConfiguration.CreateShuffleNet2(modelFile, imageClassesFile);
                var testImage = Shared.Create(Image.FromBitmap(new System.Drawing.Bitmap(testImageFile)));

                this.ImageClassificationTest(
                    modelConfig,
                    testImage,
                    new[] {
                        ("n03100240 convertible", 0.44455552f),
                        ("n02701002 ambulance", 0.17488436f),
                        ("n02974003 car wheel", 0.08973005f),
                        ("n03459775 grille, radiator grille", 0.063590035f),
                        ("n03930630 pickup, pickup truck", 0.060100798f),
                    });
            }
            else
            {
                Assert.Inconclusive($"Test not run because 'PsiTestResources' environment variable not found.");
            }
        }

        private void ImageClassificationTest(
            ImageNetModelRunnerConfiguration testConfig,
            Shared<Image> testImage,
            IList<(string Label, float Confidence)> expectedPredictions)
        {
            List<LabeledPrediction> labeledResults = null;

            // create a pipeline and run the model on the test image
            using (var p = Pipeline.Create())
            {
                var resNet = new ImageNetModelRunner(p, testConfig);
                Generators.Return(p, testImage)
                    .PipeTo(resNet)
                    .Do(results => labeledResults = results.DeepClone());

                p.Run();
            }

            // verify the top prediction of the model matches the expected top prediction and the confidence scores are within 0.05 of each other
            Assert.AreEqual(expectedPredictions[0].Label, labeledResults[0].Label);
            Assert.AreEqual(expectedPredictions[0].Confidence, labeledResults[0].Confidence, 0.05);

            // verify the rest of the predictions have similar confidence scores (within 0.05) to those in the expected set of predictions (order may vary)
            for (int i = 1; i < labeledResults.Count; i++)
            {
                // Find the expected prediction which matches the label of the current prediction
                var expectedPrediction = expectedPredictions.FirstOrDefault(p => p.Label == labeledResults[i].Label);

                Assert.AreEqual(expectedPrediction.Label, labeledResults[i].Label);
                Assert.AreEqual(expectedPrediction.Confidence, labeledResults[i].Confidence, 0.05);
            }
        }
    }
}

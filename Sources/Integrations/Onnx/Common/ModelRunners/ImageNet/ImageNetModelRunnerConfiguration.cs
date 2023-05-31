// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    /// <summary>
    /// Represents the configuration for the <see cref="ImageNetModelRunner"/> class.
    /// </summary>
    /// <remarks>
    /// For convenience, a set of pre-defined model runner configurations are defined for a few image
    /// classification models via static creation methods. The model files themselves are available
    /// in the ONNX Model Zoo (https://github.com/onnx/models/tree/master/vision/classification). They
    /// will need to be downloaded separately and the path to the model file will need to be supplied
    /// when creating the configuration.
    /// </remarks>
    public class ImageNetModelRunnerConfiguration
    {
        /// <summary>
        /// Gets or sets the path to the model file.
        /// </summary>
        public string ModelFilePath { get; set; }

        /// <summary>
        /// Gets or sets the path to a text file containing the 1000 ImageNet classes.
        /// </summary>
        public string ImageClassesFilePath { get; set; }

        /// <summary>
        /// Gets or sets the name of the input vector.
        /// </summary>
        public string InputVectorName { get; set; }

        /// <summary>
        /// Gets or sets the name of the output vector.
        /// </summary>
        public string OutputVectorName { get; set; }

        /// <summary>
        /// Gets or sets the number of predictions to include in the output. The
        /// top-N predictions (where N = NumberOfPredictions) are output by the
        /// component in order of decreasing probability.
        /// </summary>
        public int NumberOfPredictions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to apply the softmax
        /// function to the raw model scores to obtain the class probabilities.
        /// </summary>
        public bool ApplySoftmaxToModelOutput { get; set; }

        /// <summary>
        /// Gets or sets the GPU device ID to run on, or null to run on CPU.
        /// </summary>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.Gpu library instead of Microsoft.Psi.Onnx.Cpu, and set the value of
        /// the <see cref="GpuDeviceId"/> property to a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        public int? GpuDeviceId { get; set; }

        /// <summary>
        /// Creates an <see cref="ImageNetModelRunner"/> configuration for the ResNet50-caffe2 model, available at
        /// https://github.com/onnx/models/raw/b9a54e89508f101a1611cd64f4ef56b9cb62c7cf/vision/classification/resnet/model/resnet50-caffe2-v1-7.onnx
        /// and licensed at that commit under the Apache 2.0 License. This file must be downloaded separately
        /// and the path to it should be specified in the <paramref name="modelFilePath"/> parameter. Additionally,
        /// the path to a file containing the 1000 ImageNet classes must also be supplied in the
        /// <paramref name="imageClassesFilePath"/>, similar to the one available at
        /// https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt.
        /// </summary>
        /// <param name="modelFilePath">The path to the model file.</param>
        /// <param name="imageClassesFilePath">The path to the ImageNet classes file.</param>
        /// <param name="numberOfPredictions">The number of predictions that the component should output.</param>
        /// <param name="gpuDeviceId">The GPU device ID to run on, or null to run on CPU.</param>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.Gpu library instead of Microsoft.Psi.Onnx.Cpu, and set the value of
        /// the <see cref="GpuDeviceId"/> property to a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        /// <returns>The model runner configuration.</returns>
        public static ImageNetModelRunnerConfiguration CreateResNet50Caffe2(
            string modelFilePath,
            string imageClassesFilePath,
            int numberOfPredictions = 5,
            int? gpuDeviceId = null)
        {
            return new ImageNetModelRunnerConfiguration
            {
                ModelFilePath = modelFilePath,
                ImageClassesFilePath = imageClassesFilePath,
                InputVectorName = "gpu_0/data_0",
                OutputVectorName = "gpu_0/softmax_1",
                ApplySoftmaxToModelOutput = false,
                NumberOfPredictions = numberOfPredictions,
                GpuDeviceId = gpuDeviceId,
            };
        }

        /// <summary>
        /// Creates an <see cref="ImageNetModelRunner"/> configuration for the ResNet50-V2 model, available at
        /// https://github.com/onnx/models/raw/b9a54e89508f101a1611cd64f4ef56b9cb62c7cf/vision/classification/resnet/model/resnet50-v2-7.onnx
        /// and licensed at that commit under the Apache 2.0 Licese. This file must be downloaded separately
        /// and the path to it should be specified in the <paramref name="modelFilePath"/> parameter. Additionally,
        /// the path to a file containing the 1000 ImageNet classes must also be supplied in the
        /// <paramref name="imageClassesFilePath"/>, similar to the one available at
        /// https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt.
        /// </summary>
        /// <param name="modelFilePath">The path to the model file.</param>
        /// <param name="imageClassesFilePath">The path to the ImageNet classes file.</param>
        /// <param name="numberOfPredictions">The number of predictions that the component should output.</param>
        /// <param name="gpuDeviceId">The GPU device ID to run on, or null to run on CPU.</param>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.Gpu library instead of Microsoft.Psi.Onnx.Cpu, and set the value of
        /// the <see cref="GpuDeviceId"/> property to a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        /// <returns>The model runner configuration.</returns>
        public static ImageNetModelRunnerConfiguration CreateResNet50v2(
            string modelFilePath,
            string imageClassesFilePath,
            int numberOfPredictions = 5,
            int? gpuDeviceId = null)
        {
            return new ImageNetModelRunnerConfiguration
            {
                ModelFilePath = modelFilePath,
                ImageClassesFilePath = imageClassesFilePath,
                InputVectorName = "data",
                OutputVectorName = "resnetv24_dense0_fwd",
                ApplySoftmaxToModelOutput = true,
                NumberOfPredictions = numberOfPredictions,
                GpuDeviceId = gpuDeviceId,
            };
        }

        /// <summary>
        /// Creates an <see cref="ImageNetModelRunner"/> configuration for the VGG 16 model, available at
        /// https://github.com/onnx/models/raw/f884b33c3e2371952aad7ea091898f418c830fe5/vision/classification/vgg/model/vgg16-7.onnx
        /// and licensed at that commit under the Apache 2.0 License. This file must be downloaded separately
        /// and the path to it should be specified in the <paramref name="modelFilePath"/> parameter. Additionally,
        /// the path to a file containing the 1000 ImageNet classes must also be supplied in the
        /// <paramref name="imageClassesFilePath"/>, similar to the one available at
        /// https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt.
        /// </summary>
        /// <param name="modelFilePath">The path to the model file.</param>
        /// <param name="imageClassesFilePath">The path to the ImageNet classes file.</param>
        /// <param name="numberOfPredictions">The number of predictions that the component should output.</param>
        /// <param name="gpuDeviceId">The GPU device ID to run on, or null to run on CPU.</param>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.Gpu library instead of Microsoft.Psi.Onnx.Cpu, and set the value of
        /// the <see cref="GpuDeviceId"/> property to a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        /// <returns>The model runner configuration.</returns>
        public static ImageNetModelRunnerConfiguration CreateVgg16(
            string modelFilePath,
            string imageClassesFilePath,
            int numberOfPredictions = 5,
            int? gpuDeviceId = null)
        {
            return new ImageNetModelRunnerConfiguration
            {
                ModelFilePath = modelFilePath,
                ImageClassesFilePath = imageClassesFilePath,
                InputVectorName = "data",
                OutputVectorName = "vgg0_dense2_fwd",
                ApplySoftmaxToModelOutput = true,
                NumberOfPredictions = numberOfPredictions,
                GpuDeviceId = gpuDeviceId,
            };
        }

        /// <summary>
        /// Creates an <see cref="ImageNetModelRunner"/> configuration for the ShuffleNetV2 model, available at
        /// https://github.com/onnx/models/raw/b9a54e89508f101a1611cd64f4ef56b9cb62c7cf/vision/classification/shufflenet/model/shufflenet-v2-10.onnx
        /// and licensed at that commit under the BSD 3-Clause License. This file must be downloaded separately
        /// and the path to it should be specified in the <paramref name="modelFilePath"/> parameter. Additionally,
        /// the path to a file containing the 1000 ImageNet classes must also be supplied in the
        /// <paramref name="imageClassesFilePath"/>, similar to the one available at
        /// https://github.com/onnx/models/raw/8d50e3f598e6d5c67c7c7253e5a203a26e731a1b/vision/classification/synset.txt.
        /// </summary>
        /// <param name="modelFilePath">The path to the model file.</param>
        /// <param name="imageClassesFilePath">The path to the ImageNet classes file.</param>
        /// <param name="numberOfPredictions">The number of predictions that the component should output.</param>
        /// <param name="gpuDeviceId">The GPU device ID to run on, or null to run on CPU.</param>
        /// <remarks>
        /// To run on a GPU, use the Microsoft.Psi.Onnx.Gpu library instead of Microsoft.Psi.Onnx.Cpu, and set the value of
        /// the <see cref="GpuDeviceId"/> property to a valid non-negative integer. Typical device ID values are 0 or 1.
        /// </remarks>
        /// <returns>The model runner configuration.</returns>
        public static ImageNetModelRunnerConfiguration CreateShuffleNet2(
            string modelFilePath,
            string imageClassesFilePath,
            int numberOfPredictions = 5,
            int? gpuDeviceId = null)
        {
            return new ImageNetModelRunnerConfiguration
            {
                ModelFilePath = modelFilePath,
                ImageClassesFilePath = imageClassesFilePath,
                InputVectorName = "input",
                OutputVectorName = "output",
                ApplySoftmaxToModelOutput = true,
                NumberOfPredictions = numberOfPredictions,
                GpuDeviceId = gpuDeviceId,
            };
        }
    }
}

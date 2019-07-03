// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.CognitiveServices.Vision
{
    using System;
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
    using Microsoft.Psi;
    using Microsoft.Psi.CognitiveServices.Vision;
    using Microsoft.Psi.Kinect;

    /// <summary>
    /// Test runner to make debugging easier and faster.
    /// </summary>
    public class ConsoleMain
    {
        /// <summary>
        /// Entry point to make debugging easier and faster.
        /// </summary>
        /// <param name="args">Command-line args.</param>
        public static void Main(string[] args)
        {
            using (var pipeline = Pipeline.Create())
            {
                var kinectSensor = new KinectSensor(pipeline, KinectSensorConfiguration.Default);
                var kinectJpeg = kinectSensor.ColorImage;
                var imageAnalyzer = new ImageAnalyzer(pipeline, new ImageAnalyzerConfiguration(null, null, VisualFeatureTypes.Description, VisualFeatureTypes.Faces, VisualFeatureTypes.Categories, VisualFeatureTypes.Tags));
                kinectJpeg.Sample(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(1)).PipeTo(imageAnalyzer.In);
                imageAnalyzer.Out.Do(result =>
                {
                    Console.Clear();
                    Console.WriteLine(ImageAnalyzer.AnalysisResultToString(result));
                });
                pipeline.RunAsync();
                Console.ReadLine();
            }
        }
    }
}

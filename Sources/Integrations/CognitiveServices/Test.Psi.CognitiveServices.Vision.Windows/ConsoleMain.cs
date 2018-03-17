// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.CognitiveServices.Vision
{
    using System;
    using Microsoft.ProjectOxford.Vision;
    using Microsoft.Psi;
    using Microsoft.Psi.CognitiveServices.Vision;
    using Microsoft.Psi.Kinect;

    public class ConsoleMain
    {
        public static void Main(string[] args)
        {
            using (var pipeline = Pipeline.Create())
            {
                var kinectSensor = new KinectSensor(pipeline, KinectSensorConfiguration.Default);
                var kinectJpeg = kinectSensor.ColorImage;
                var imageAnalyzer = new ImageAnalyzer(pipeline, new ImageAnalyzerConfiguration(null, VisualFeature.Description, VisualFeature.Faces, VisualFeature.Categories, VisualFeature.Tags));
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

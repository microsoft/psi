// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace MultiModalSpeechDetection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Speech;

    /// <summary>
    /// Kinect sample program.
    /// </summary>
    public class Program
    {
        // Variables related to Psi
        private const string ApplicationName = "KinectSample";
        private static TimeSpan hundredMs = TimeSpan.FromSeconds(0.1);

        /// <summary>
        /// Main entry point.
        /// </summary>
        public static void Main()
        {
            Program prog = new Program();
            prog.PerformMultiModalSpeechDetection();
        }

        /// <summary>
        /// Event handler for the <see cref="Pipeline.PipelineCompleted"/> event.
        /// </summary>
        /// <param name="sender">The sender which raised the event.</param>
        /// <param name="e">The pipeline completion event arguments.</param>
        private static void Pipeline_PipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            Console.WriteLine("Pipeline execution completed with {0} errors", e.Errors.Count);
        }

        /// <summary>
        /// Event handler for the <see cref="Pipeline.PipelineExceptionNotHandled"/> event.
        /// </summary>
        /// <param name="sender">The sender which raised the event.</param>
        /// <param name="e">The pipeline exception event arguments.</param>
        private static void Pipeline_PipelineException(object sender, PipelineExceptionNotHandledEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }

        /// <summary>
        /// This is the main code for our Multimodal Speech Detection demo.
        /// </summary>
        private void PerformMultiModalSpeechDetection()
        {
            Console.WriteLine("Initializing Psi.");

            bool detected = false;

            // First create our \Psi pipeline
            using (var pipeline = Pipeline.Create("MultiModalSpeechDetection"))
            {
                // Register an event handler to catch pipeline errors
                pipeline.PipelineExceptionNotHandled += Pipeline_PipelineException;

                // Register an event handler to be notified when the pipeline completes
                pipeline.PipelineCompleted += Pipeline_PipelineCompleted;

                // Next create our Kinect sensor. We will be using the color images, face tracking, and audio from the Kinect sensor
                var kinectSensorConfig = new KinectSensorConfiguration
                {
                    OutputColor = true,
                    OutputAudio = true,
                    OutputBodies = true, // In order to detect faces using Kinect you must also enable detection of bodies
                };
                var kinectSensor = new KinectSensor(pipeline, kinectSensorConfig);
                var kinectFaceDetector = new Microsoft.Psi.Kinect.Face.KinectFaceDetector(pipeline, kinectSensor, Microsoft.Psi.Kinect.Face.KinectFaceDetectorConfiguration.Default);

                // Create our Voice Activation Detector
                var speechDetector = new SystemVoiceActivityDetector(pipeline);
                var convertedAudio = kinectSensor.Audio.Resample(WaveFormat.Create16kHz1Channel16BitPcm());
                convertedAudio.PipeTo(speechDetector);

                // Use the Kinect's face track to determine if the mouth is opened
                var mouthOpenAsFloat = kinectFaceDetector.Faces.Where(faces => faces.Count > 0).Select((List<Microsoft.Psi.Kinect.Face.KinectFace> list) =>
                {
                    if (!detected)
                    {
                        detected = true;
                        Console.WriteLine("Found your face");
                    }

                    bool open = (list[0] != null) ? list[0].FaceProperties[Microsoft.Kinect.Face.FaceProperty.MouthOpen] == Microsoft.Kinect.DetectionResult.Yes : false;
                    return open ? 1.0 : 0.0;
                });

                // Next take the "mouthOpen" value and create a hold on that value (so that we don't see 1,0,1,0,1 but instead would see 1,1,1,1,0.8,0.6,0.4)
                var mouthOpen = mouthOpenAsFloat.Hold(0.1);

                // Next join the results of the speechDetector with the mouthOpen generator and only select samples where
                // we have detected speech and that the mouth was open.
                var mouthAndSpeechDetector = speechDetector.Join(mouthOpen, hundredMs).Select((t, e) => t.Item1 && t.Item2);

                // Convert our speech into text
                var speechRecognition = convertedAudio.SpeechToText(mouthAndSpeechDetector);
                speechRecognition.Do((s, t) =>
                {
                    if (s.Item1.Length > 0)
                    {
                        Console.WriteLine("You said: " + s.Item1);
                    }
                });

                // Create a stream of landmarks (points) from the face detector
                var facePoints = new List<Tuple<System.Windows.Point, string>>();
                var landmarks = kinectFaceDetector.Faces.Where(faces => faces.Count > 0).Select((List<Microsoft.Psi.Kinect.Face.KinectFace> list) =>
                {
                    facePoints.Clear();
                    System.Windows.Point pt1 = new System.Windows.Point(
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.EyeLeft].X,
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.EyeLeft].Y);
                    facePoints.Add(Tuple.Create(pt1, string.Empty));

                    System.Windows.Point pt2 = new System.Windows.Point(
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.EyeRight].X,
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.EyeRight].Y);
                    facePoints.Add(Tuple.Create(pt2, string.Empty));

                    System.Windows.Point pt3 = new System.Windows.Point(
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.MouthCornerLeft].X,
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.MouthCornerLeft].Y);
                    facePoints.Add(Tuple.Create(pt3, string.Empty));

                    System.Windows.Point pt4 = new System.Windows.Point(
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.MouthCornerRight].X,
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.MouthCornerRight].Y);
                    facePoints.Add(Tuple.Create(pt4, string.Empty));

                    System.Windows.Point pt5 = new System.Windows.Point(
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.Nose].X,
                            list[0].FacePointsInColorSpace[Microsoft.Kinect.Face.FacePointType.Nose].Y);
                    facePoints.Add(Tuple.Create(pt5, string.Empty));
                    return facePoints;
                });

                // ********************************************************************
                // Finally create a Live Visualizer using PsiStudio.
                // We must persist our streams to a store in order for Live Viz to work properly
                // ********************************************************************

                // Create store for the data. Live Visualizer can only read data from a store.
                var pathToStore = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
                var store = PsiStore.Create(pipeline, ApplicationName, pathToStore);

                mouthOpen.Select(v => v ? 1d : 0d).Write("MouthOpen", store);

                speechDetector.Select(v => v ? 1d : 0d).Write("VAD", store);

                mouthAndSpeechDetector.Write("Join(MouthOpen,VAD)", store);

                kinectSensor.Audio.Write("Audio", store);

                var images = kinectSensor.ColorImage.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                images.Write("Images", store, true, DeliveryPolicy.LatestMessage);

                landmarks.Write("FaceLandmarks", store);

                // Run the pipeline
                pipeline.RunAsync();

                Console.WriteLine("Press any key to finish recording");
                Console.ReadKey();
            }
        }
    }
}

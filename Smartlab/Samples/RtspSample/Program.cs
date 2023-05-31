namespace RtspSample
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using CMU.Smartlab.Communication;
    using CMU.Smartlab.Rtsp;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;

    class Program
    {
        private const string ApplicationName = "RTSP Sample";

        public static readonly object SendLock = new object();

        private static DateTime frameTime = new DateTime(0);

        private static volatile CommunicationManager manager;

        private static bool sending = false;

        static void Main(string[] args)
        {
            // Flag to exit the application
            bool exit = false;
            manager = new CommunicationManager();

            while (!exit)
            {
                Console.WriteLine("================================================================================");
                Console.WriteLine("                               Psi Web Camera + Audio Sample");
                Console.WriteLine("================================================================================");
                Console.WriteLine("1) Start Recording. Please any key to finish recording");
                Console.WriteLine("Q) QUIT");
                Console.Write("Enter selection: ");
                ConsoleKey key = Console.ReadKey().Key;
                Console.WriteLine();

                exit = false;
                switch (key)
                {
                    case ConsoleKey.D1:
                        // Record video+audio to a store in the user's MyVideos folder
                        var folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        Console.WriteLine(folder);
                        RecordRtsp(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
                        break;
                    case ConsoleKey.Q:
                        exit = true;
                        break;
                }
            }
        }
        /// <summary>
        /// Builds and runs a webcam pipeline and records the data to a Psi store.
        /// </summary>
        /// <param name="pathToStore">The path to directory where store should be saved.</param>
        public static void RecordRtsp(string pathToStore)
        {
            // Create the pipeline object.
            using (Pipeline pipeline = Pipeline.Create())
            {
                // Register an event handler to catch pipeline errors
                pipeline.PipelineExceptionNotHandled += Pipeline_PipelineException;

                // Register an event handler to be notified when the pipeline completes
                pipeline.PipelineCompleted += Pipeline_PipelineCompleted;

                // Create store
                Microsoft.Psi.Data.Exporter store = Store.Create(pipeline, ApplicationName, pathToStore);

                // Create our webcam
                var serverUriPSIb = new Uri("rtsp://lorex5416b1.pc.cs.cmu.edu");
                var credentialsPSIb = new NetworkCredential("admin", "54Lorex16");
                RtspCapture webcamPSIb = new RtspCapture(pipeline, serverUriPSIb, credentialsPSIb, true);
                /*
                var serverUriB2 = new Uri("rtsp://lorex5416b2.pc.cs.cmu.edu");
                var credentialsB2 = new NetworkCredential("admin", "admin");
                MediaCaptureRtsp webcamB2 = new MediaCaptureRtsp(pipeline, serverUriB2, credentialsB2, true);
                var serverUriA1 = new Uri("rtsp://lorex5416a1.pc.cs.cmu.edu");
                var credentialsA1 = new NetworkCredential("admin", "Lorex5416");
                MediaCaptureRtsp webcamA1 = new MediaCaptureRtsp(pipeline, serverUriA1, credentialsA1, true);
                var serverUriA2 = new Uri("rtsp://lorex5416a2.pc.cs.cmu.edu");
                var credentialsA2 = new NetworkCredential("admin", "admin");
                MediaCaptureRtsp webcamA2 = new MediaCaptureRtsp(pipeline, serverUriA2, credentialsA2, true);
                */

                // Create the AudioCapture component to capture audio from the default device in 16 kHz 1-channel
                var audioInputPSIb = webcamPSIb.Audio;
                /*
                var audioInputB2 = webcamB2.Audio;
                var audioInputA1 = webcamA1.Audio;
                var audioInputA2 = webcamA2.Audio;
                */

                var imagesPSIb = webcamPSIb.Out.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                /*
                var imagesB2 = webcamB2.Out.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                var imagesA1 = webcamA1.Out.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                var imagesA2 = webcamA2.Out.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                */

                // Attach the webcam's image output to the store. We will write the images to the store as compressed JPEGs.
                Store.Write(imagesPSIb, "ImagePSIb", store, true, DeliveryPolicy.LatestMessage);
                /*
                Store.Write(imagesB2, "ImageB2", store, true, DeliveryPolicy.LatestMessage);
                Store.Write(imagesA1, "ImageA1", store, true, DeliveryPolicy.LatestMessage);
                Store.Write(imagesA2, "ImageA2", store, true, DeliveryPolicy.LatestMessage);
                */

                // Attach the audio input to the store
                Store.Write(audioInputPSIb, "AudioPSIb", store, true, DeliveryPolicy.LatestMessage);
                /*
                Store.Write(audioInputB2, "AudioB2", store, true, DeliveryPolicy.LatestMessage);
                Store.Write(audioInputA1, "AudioA1", store, true, DeliveryPolicy.LatestMessage);
                Store.Write(audioInputA2, "AudioA2", store, true, DeliveryPolicy.LatestMessage);
                */

                // Run the pipeline
                pipeline.RunAsync();

                Console.WriteLine("Press any key to finish recording");
                Console.ReadKey();
            }
        }

        private static void SendImage(Shared<Image> image, Envelope envelope)
        {
            // manager.SendText("test", "New image received" + image.ToString());
            Image rawData = image.Resource;
            Task task = new Task(() =>
            {
                lock (SendLock)
                {
                    try
                    {
                        int w = rawData.Width;
                        float scale = 720.0f / w;
                        rawData = rawData.Scale(scale, scale, SamplingMode.Bilinear).Resource;
                        manager.SendText("testRTSP_Time", envelope.OriginatingTime.Ticks.ToString());
                        manager.SendImage("testRTSP_Image", rawData);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                    finally
                    {
                        sending = false;
                    }
                }
            });
            if (!sending && envelope.OriginatingTime.CompareTo(frameTime) > 0)
            {
                Console.WriteLine("sending");
                sending = true;
                frameTime = envelope.OriginatingTime;
                task.Start();
            }
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
    }
}

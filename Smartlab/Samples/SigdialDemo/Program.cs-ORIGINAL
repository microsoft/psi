namespace Smartlab_Demo_v2_1
{
    using CMU.Smartlab.Communication;
    using CMU.Smartlab.Identity;
    using CMU.Smartlab.Rtsp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Kinect;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Kinect;
    using Apache.NMS;
    using Apache.NMS.ActiveMQ.Transport.Discovery;
    using System.Net;

    class Program
    {
        private const string AppName = "SmartLab Project - Demo v3.0 (for SigDial Demo)";

        private const string TopicToBazaar = "PSI_Bazaar_Text";
        private const string TopicToPython = "PSI_Python_Image";
        private const string TopicToMacaw = "PSI_Macaw_Text";
        private const string TopicToNVBG = "PSI_NVBG_Location";
        private const string TopicToVHText = "PSI_VHT_Text";
        private const string TopicFromPython = "Python_PSI_Location";
        private const string TopicFromBazaar = "Bazaar_PSI_Text";
        private const string TopicFromPython_QueryKinect = "Python_PSI_QueryKinect";
        private const string TopicToPython_AnswerKinect = "PSI_Python_AnswerKinect";

        private const int SendingImageWidth = 360;
        private const int MaxSendingFrameRate = 15;
        // private const string TcpIPSubscriber = "tcp://127.0.0.1:5555";
        // private const string TcpIPPublisher = "tcp://127.0.0.1:5556";

        private const int KinectImageWidth = 1920;
        private const int KinectImageHeight = 1080;

        private const double SocialDistance = 183;
        private const double DistanceWarningCooldown = 30.0;
        private const double NVBGCooldownLocation = 8.0;
        private const double NVBGCooldownAudio = 3.0;

        private static string AzureSubscriptionKey = "abee363f8d89444998c5f35b6365ca38";
        private static string AzureRegion = "eastus";

        private static CommunicationManager manager;
        // private static NetMqPublisher netmqpublisher;
        // private static NetMqSubscriber netmqsubscriber;
        public static readonly object SendToBazaarLock = new object();
        public static readonly object SendToPythonLock = new object();
        public static readonly object LocationLock = new object();
        public static readonly object AudioSourceLock = new object();

        public static volatile bool AudioSourceFlag = true;

        public static DateTime LastLocSendTime = new DateTime();
        public static DateTime LastDistanceWarning = new DateTime();
        public static DateTime LastNVBGTime = new DateTime();

        public static List<IdentityInfo> IdInfoList;
        public static Dictionary<string, IdentityInfo> IdHead;
        public static Dictionary<string, IdentityInfo> IdTail;
        public static SortedList<DateTime, CameraSpacePoint[]> KinectMappingBuffer;
        public static List<String> AudioSourceList;

        public static CameraInfo KinectInfo;
        public static CameraInfo VhtInfo;

        static void Main(string[] args)
        {
            SetConsole();
            if (Initialize())
            {
                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("############################################################################");
                    Console.WriteLine("1) Multimodal streaming, Kinect. Press any key to finish streaming.");
                    Console.WriteLine("2) Multimodal streaming, Webcam. Press any key to finish streaming.");
                    Console.WriteLine("3) Multimodal streaming, Lorex camera. Press any key to finish streaming.");
                    Console.WriteLine("4) Multimodal streaming, Amcrest camera on ethernet. Press any key to finish streaming.");
                    Console.WriteLine("5) Multimodal streaming, Amcrest camera on wifi. Press any key to finish streaming.");
                    Console.WriteLine("6) Multimodal streaming, Foscam camera on ethernet. Press any key to finish streaming.");
                    Console.WriteLine("7) Multimodal streaming, Foscam camera on wifi. Press any key to finish streaming.");
                    Console.WriteLine("8) Audio only. Press any key to finish streaming.");
                    Console.WriteLine("Q) Quit.");
                    ConsoleKey key = Console.ReadKey().Key;
                    Console.WriteLine();
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            RunDemo(false, "Kinect");
                            break;
                        case ConsoleKey.D2:
                            RunDemo(false, "webcam");
                            break;
                        case ConsoleKey.D3:
                            RunDemo(false, "lorex");
                            break;
                        case ConsoleKey.D4:
                            Console.WriteLine("Streaming Amcrest camera on ethernet ...");
                            RunDemo(false, "amcrest_ethernet");
                            break;
                        case ConsoleKey.D5:
                            Console.WriteLine("Streaming Amcrest camera on wifi ...");
                            RunDemo(false, "amcrest_wifi");
                            break;
                        case ConsoleKey.D6:
                            Console.WriteLine("Streaming Foscam camera on ethernet ...");
                            RunDemo(false, "foscam_ethernet");
                            break;
                        case ConsoleKey.D7:
                            Console.WriteLine("Streaming Foscam camera on wifi ...");
                            RunDemo(false, "foscam_wifi");
                            break;
                        case ConsoleKey.D8:
                            RunDemo(true);
                            break;
                        case ConsoleKey.Q:
                            exit = true;
                            break;
                    }
                }
            }
            else
            {

                Console.ReadLine();
            }
        }

        private static void SetConsole()
        {
            Console.Title = AppName;
            Console.Write(@"                                                                                                    
                                                                                                                   
                                                                                                   ,]`             
                                                                                                 ,@@@              
            ]@@\                                                           ,/\]                ,@/=@*              
         ,@@[@@/                                           ,@@           ,@@[@@/.                 =\               
      .//`   [      ,`                 ,]]]]`             .@@^           @@`            ]]]]]     @^               
    .@@@@@\]]`    .@@`  /]   ]]      ,@/,@@^    /@@@,@@@@@@@@@@[`        @@           /@`\@@     ,@@@@@@@^         
             \@@` =@^ ,@@@`//@@^    .@^ =@@^     ,@@`     /@*           ,@^          =@*.@@@*    =@   ,@/          
             ,@@* =@,@` =@@` =@^  ` @@ //\@@  ,\ @@^     ,@^            /@          =@^,@[@@^ ./`=@. /@`           
    ,@^    ,/@[   =@@. ,@@`  ,@^//.=@\@` ,@@@@` .@@     .@@^  /@    ,@\]@`     ,@@/ @@//  \@@@/  @@]@`             
    ,\/@[[`      =@@`  \/`    [[`  =@/    ,@`   ,[`      @@@@/      [[@@@@@@@@@[`  .@@`    \/*  /@/`               
                  ,`                                                                           ,`                  
                                                                                                                   
                                                                                                                   
                                                                                                                 
");
            Console.WriteLine("############################################################################");
        }

        static bool Initialize()
        {
            if (!GetSubscriptionKey())
            {
                Console.WriteLine("Missing Subscription Key!");
                return false;
            }
            IdInfoList = new List<IdentityInfo>();
            KinectMappingBuffer = new SortedList<DateTime, CameraSpacePoint[]>();
            AudioSourceList = new List<string>();
            KinectInfo = new CameraInfo(
                location: new Point3D(29.2, 12.7, 125.7),
                dir_x: new Point3D(-17.27, 19.26, 1.01284),
                dir_y: null,
                dir_z: new Point3D(357.1, 319.2, 19.08)
            );
            VhtInfo = new CameraInfo(
                location: new Point3D(-26.67, 93.98, 104.78),
                dir_x: new Point3D(0.0, 1.0, 0.0),
                dir_y: null,
                dir_z: new Point3D(1.0, 0.0, 0.0)
                );

            IdHead = new Dictionary<string, IdentityInfo>();
            IdTail = new Dictionary<string, IdentityInfo>();
            manager = new CommunicationManager();
            manager.subscribe(TopicFromPython, ProcessLocation);
            manager.subscribe(TopicFromBazaar, ProcessText);
            manager.subscribe(TopicFromPython_QueryKinect, HandleKinectQuery);
            // netmqsubscriber = new NetMqSubscriber(TcpIPSubscriber);
            // netmqsubscriber.RegisterSubscriber(TopicFromBazaar); 

            // netmqpublisher = new NetMqPublisher(TcpIPPublisher);
            // netmqpublisher.RegisterSubscriber(TopicToBazaar);
            return true;
        }

        private static void HandleKinectQuery(byte[] b)
        {
            string text = Encoding.ASCII.GetString(b);
            //Console.WriteLine($"Queried for the depth information. Query: {text}");
            string[] infos = text.Split(';');
            long ticks = long.Parse(infos[0]);
            // x should from left to right and y should from up to down
            double x = double.Parse(infos[1]);
            double y = double.Parse(infos[2]);
            //Console.WriteLine($"Parsed: {ticks}, {x}, {y}");
            if (KinectMappingBuffer is null || KinectMappingBuffer.Count == 0)
            {
                manager.SendText(TopicToPython_AnswerKinect, $"{ticks};null");
                // Console.WriteLine($"Answering Query: {ticks};null");
                return;
            }

            // Binary search for the nearest Mapper
            int left = 0;
            int right = KinectMappingBuffer.Count;
            while (right - left > 1)
            {
                // Console.WriteLine($"left: {left}, right: {right}");
                int mid = (right + left) / 2;
                if (KinectMappingBuffer.ElementAt(mid).Key.Ticks <= ticks)
                {
                    left = mid;
                }
                else
                {
                    right = mid;
                }
            }

            long diff1 = Math.Abs(KinectMappingBuffer.ElementAt(left).Key.Ticks - ticks);
            long diff2;
            if (left + 1 < KinectMappingBuffer.Count)
            {
                diff2 = Math.Abs(KinectMappingBuffer.ElementAt(left).Key.Ticks - ticks);
            }
            else
            {
                diff2 = long.MaxValue;
            }

            CameraSpacePoint[] mapper;
            if (diff1 < diff2)
            {
                mapper = KinectMappingBuffer.ElementAt(left).Value;
            }
            else
            {
                mapper = KinectMappingBuffer.ElementAt(left + 1).Value;
            }

            // Convert to original image size:
            int real_x = (int)(x * KinectImageWidth);
            int real_y = (int)(y * KinectImageHeight);
            CameraSpacePoint result = new CameraSpacePoint();
            result.X = 0;
            result.Y = 0;
            result.Z = 0;
            int valid = 0;
            for (int i = real_x - 5; i < real_x + 6; ++i)
            {
                for (int j = real_y - 5; j < real_y + 6; ++j)
                {
                    if ((i < 0) || (j < 0) || (i > KinectImageWidth) || (j > KinectImageHeight))
                    {
                        continue;
                    }
                    CameraSpacePoint p = mapper[j * KinectImageWidth + i];
                    if (p.X + p.Y + p.Z < -1000000 || p.X + p.Y + p.Z > 1000000)
                    {
                        continue;
                    }
                    valid++;
                    result.X += p.X;
                    result.Y += p.Y;
                    result.Z += p.Z;
                }
            }
            if (valid > 0)
            {
                Point3D to_send = new Point3D(result.X / valid, result.Y / valid, result.Z / valid) * 100;
                to_send = KinectInfo.Cam2World(to_send);
                manager.SendText(TopicToPython_AnswerKinect, $"{ticks};{to_send.x};{to_send.y};{to_send.z}");
                //Console.WriteLine($"Answering Query: {ticks};{result.X / valid};{result.Y / valid};{result.Z / valid}");
            }
            else
            {
                manager.SendText(TopicToPython_AnswerKinect, $"{ticks};null");
                // Console.WriteLine($"Answering Query: {ticks};null");
            }
        }

        /*
         * Process location information received from Realmodal.
        */
        private static void ProcessLocation(byte[] b)
        {
            string text = Encoding.ASCII.GetString(b);
            string[] infos = text.Split(';');
            int num = int.Parse(infos[0]);
            long ts = long.Parse(infos[1]);
            if (num >= 1)
            {
                for (int i = 2; i < infos.Length; ++i)
                {
                    // Construct identity information instance.
                    IdentityInfo info = IdentityInfo.Parse(ts, infos[i]);

                    // Discard invalid instance
                    if (info.Position.IsZero())
                    {
                        continue;
                    }

                    // Find the identity information that could be the same person.
                    IdentityInfo match = null;
                    foreach (var kv in IdTail)
                    {
                        var id = kv.Value;
                        while (id != null)
                        {
                            int flag = info.SameAs(id);
                            if (flag == 1)
                            {
                                match = id;
                                break;
                            }
                            else if (flag == -1)
                            {
                                break;
                            }
                            else
                            {
                                id = id.LastMatch;
                            }
                        }
                        if (!(match is null))
                        {
                            break;
                        }
                    }

                    lock (AudioSourceLock)
                    {
                        if (!(match is null))
                        {
                            // Do clusterring.
                            IdentityInfo.MakeLink(match, info);
                            IdTail[match.TrueIdentity] = info;
                        }
                        else
                        {
                            // Build a new cluster.
                            info.NewIdentity();
                            IdHead[info.TrueIdentity] = info;
                            IdTail[info.TrueIdentity] = info;
                        }
                        // Store the inden2tity information and send it to other module.
                        IdInfoList.Add(info);
                    }
                    //Console.WriteLine($"Received location message from RealModal: multimodal:true;%;identity:{info.TrueIdentity}(Detected: {info.Identity});%;location:{infos[i].Split('&')[1]}");
                    if (DateTime.Now.Subtract(LastNVBGTime).TotalSeconds > NVBGCooldownLocation)
                    {
                        Point3D pos2send = IdInfoList?.Last().Position;
                        pos2send = VhtInfo.World2Cam(pos2send);
                        Console.WriteLine($"Send location message to NVBG: multimodal:true;%;identity:{info.TrueIdentity}(Detected: {info.Identity});%;location:{pos2send.x}:{pos2send.y}:{pos2send.z}");
                        manager.SendText(TopicToNVBG, $"multimodal:true;%;identity:{info.TrueIdentity};%;location:{pos2send.x}:{pos2send.y}:{pos2send.z}");
                        LastNVBGTime = DateTime.Now;
                    }
                }

                // Discard information long ago.
                lock (AudioSourceLock)
                {
                    while (IdInfoList.Count > 0 && IdInfoList.Last().Timestamp.Subtract(IdInfoList.First().Timestamp).TotalSeconds > 20)
                    {
                        IdentityInfo infoToRemove = IdInfoList[0];
                        IdInfoList.RemoveAt(0);
                        if (!(infoToRemove.NextMatch is null))
                        {
                            IdHead[infoToRemove.TrueIdentity] = infoToRemove.NextMatch;
                        }
                        else
                        {
                            IdHead.Remove(infoToRemove.TrueIdentity);
                        }
                        infoToRemove.Dispose();
                    }
                }

                // Detect whether there're two people that violate the social distancing.
                if (DateTime.Now.Subtract(LastDistanceWarning).TotalSeconds > DistanceWarningCooldown)
                {
                    Dictionary<string, Point3D> locations = new Dictionary<string, Point3D>();
                    foreach (var kv in IdTail)
                    {
                        if (IdInfoList.Last().Timestamp.Subtract(kv.Value.Timestamp).TotalSeconds > 1)
                        {
                            break;
                        }
                        var cur = kv.Value;
                        foreach (var kv2 in locations)
                        {
                            if (PUtil.Distance(kv2.Value, cur.Position) < SocialDistance)
                            {
                                LastDistanceWarning = DateTime.Now;
                                manager.SendText(TopicToBazaar, "multimodal:true;%;identity:group;%;pose:too_close");
                                Console.WriteLine($"{kv2.Key} is too close to {cur.TrueIdentity}! Distance:{PUtil.Distance(kv2.Value, cur.Position)}");
                                Console.WriteLine("Send message to Bazaar: multimodal:true;%;identity:group;%;pose:too_close");
                                break;
                            }
                        }
                        locations.Add(cur.TrueIdentity, cur.Position);
                    }
                }
            }
        }

        private static void ProcessText(String s)
        {
            if (s != null)
            {
                Console.WriteLine($">>> Send MULTIMODAL message to VHT: multimodal:false;%;identity:someone;%;text:{s}");
                manager.SendText(TopicToVHText, s);
            }
        }

        public static void RunDemo(bool AudioOnly = false, string cameraType = "webcam")
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                pipeline.PipelineExceptionNotHandled += Pipeline_PipelineException;
                pipeline.PipelineCompleted += Pipeline_PipelineCompleted;

                // var store = Store.Open(pipeline, Program.LogName, Program.LogPath);
                // Send video part to Python

                // var video = store.OpenStream<Shared<EncodedImage>>("Image");
                if (!AudioOnly && cameraType == "Kinect")
                {
                    var kinectSensorConfig = new KinectSensorConfiguration
                    {
                        OutputColor = true,
                        OutputDepth = true,
                        OutputRGBD = true,
                        OutputColorToCameraMapping = true,
                        OutputBodies = false,
                        OutputAudio = true,
                    };
                    var kinectSensor = new Microsoft.Psi.Kinect.KinectSensor(pipeline, kinectSensorConfig);
                    var kinectColor = kinectSensor.ColorImage;
                    var kinectMapping = kinectSensor.ColorToCameraMapper;
                    var kinectAudio = kinectSensor.AudioBeamInfo.Where(result => result.Confidence > 0.7);
                    kinectMapping.Do(AddNewMapper);
                    kinectAudio.Do(FindAudioSource);
                   
                    EncodedImageSendHelper helper = new EncodedImageSendHelper(manager, "webcam", Program.TopicToPython, Program.SendToPythonLock, Program.MaxSendingFrameRate);
                    var scaled = kinectColor.Resize((float)Program.SendingImageWidth, (float)Program.SendingImageWidth / Program.KinectImageWidth * Program.KinectImageHeight);
                    var encoded = scaled.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                    encoded.Do(helper.SendImage);
                   
                }
                else if (!AudioOnly && cameraType == "webcam")
                {
                    MediaCapture webcam = new MediaCapture(pipeline, 1280, 720, 30);
                    
                    EncodedImageSendHelper helper = new EncodedImageSendHelper(manager, "webcam", Program.TopicToPython, Program.SendToPythonLock, Program.MaxSendingFrameRate);
                    var scaled = webcam.Out.Resize((float)Program.SendingImageWidth, Program.SendingImageWidth / 1280.0f * 720.0f);
                    var encoded = scaled.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                    encoded.Do(helper.SendImage);                    
                }
                else if (!AudioOnly && cameraType == "lorex")
                {
                    var serverUriPSIb = new Uri("rtsp://lorex5416b1.pc.cs.cmu.edu");
                    var credentialsPSIb = new NetworkCredential("admin", "54Lorex16");
                    RtspCapture rtspPSIb = new RtspCapture(pipeline, serverUriPSIb, credentialsPSIb, true);

                    EncodedImageSendHelper helper = new EncodedImageSendHelper(manager, "webcam", Program.TopicToPython, Program.SendToPythonLock, Program.MaxSendingFrameRate);
                    var scaled = rtspPSIb.Out.Resize((float)Program.SendingImageWidth, Program.SendingImageWidth / 1280.0f * 720.0f);
                    var encoded = scaled.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                    encoded.Do(helper.SendImage);
                }
                else if (!AudioOnly && cameraType == "amcrest_ethernet")
                {
                    var serverUriPSIb = new Uri("rtsp://amcrest1041a.pc.cs.cmu.edu");
                    // var credentialsPSIb = new NetworkCredential("admin", "5416AmcrestA");
                    var credentialsPSIb = new NetworkCredential("admin", "admin");
                    RtspCapture rtspPSIb = new RtspCapture(pipeline, serverUriPSIb, credentialsPSIb, true);

                    EncodedImageSendHelper helper = new EncodedImageSendHelper(manager, "webcam", Program.TopicToPython, Program.SendToPythonLock, Program.MaxSendingFrameRate);
                    var scaled = rtspPSIb.Out.Resize((float)Program.SendingImageWidth, Program.SendingImageWidth / 1280.0f * 720.0f);
                    var encoded = scaled.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                    encoded.Do(helper.SendImage);
                }
                else if (!AudioOnly && cameraType == "amcrest_wifi")
                {
                    var serverUriPSIb = new Uri("rtsp://amcrest1041a.wifi.local.cmu.edu");
                    // var credentialsPSIb = new NetworkCredential("admin", "5416AmcrestA");
                    var credentialsPSIb = new NetworkCredential("admin", "admin");
                    RtspCapture rtspPSIb = new RtspCapture(pipeline, serverUriPSIb, credentialsPSIb, true);

                    EncodedImageSendHelper helper = new EncodedImageSendHelper(manager, "webcam", Program.TopicToPython, Program.SendToPythonLock, Program.MaxSendingFrameRate);
                    var scaled = rtspPSIb.Out.Resize((float)Program.SendingImageWidth, Program.SendingImageWidth / 1280.0f * 720.0f);
                    var encoded = scaled.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                    encoded.Do(helper.SendImage);
                }
                else if (!AudioOnly && cameraType == "foscam_ethernet")
                {
                    var serverUriPSIb = new Uri("rtsp://foscamr4sa.pc.cs.cmu.edu:88/videoMain");
                    var credentialsPSIb = new NetworkCredential("admin5416", "5416FoscamA");
                    RtspCapture rtspPSIb = new RtspCapture(pipeline, serverUriPSIb, credentialsPSIb, true);

                    EncodedImageSendHelper helper = new EncodedImageSendHelper(manager, "webcam", Program.TopicToPython, Program.SendToPythonLock, Program.MaxSendingFrameRate);
                    var scaled = rtspPSIb.Out.Resize((float)Program.SendingImageWidth, Program.SendingImageWidth / 1280.0f * 720.0f);
                    var encoded = scaled.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                    encoded.Do(helper.SendImage);
                }
                else if (!AudioOnly && cameraType == "foscam_wifi")
                {
                    var serverUriPSIb = new Uri("rtsp://foscamr4sa.wifi.local.cmu.edu:88/videoMain");
                    var credentialsPSIb = new NetworkCredential("admin5416", "5416FoscamA");
                    RtspCapture rtspPSIb = new RtspCapture(pipeline, serverUriPSIb, credentialsPSIb, true);

                    EncodedImageSendHelper helper = new EncodedImageSendHelper(manager, "webcam", Program.TopicToPython, Program.SendToPythonLock, Program.MaxSendingFrameRate);
                    var scaled = rtspPSIb.Out.Resize((float)Program.SendingImageWidth, Program.SendingImageWidth / 1280.0f * 720.0f);
                    var encoded = scaled.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                    encoded.Do(helper.SendImage);
                }

                // Send audio part to Bazaar

                // var audio = store.OpenStream<AudioBuffer>("Audio");
                var audioConfig = new AudioCaptureConfiguration()
                {
                    OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm(),
                    DropOutOfOrderPackets = true
                };
                IProducer<AudioBuffer> audio = new AudioCapture(pipeline, audioConfig);

                var vad = new SystemVoiceActivityDetector(pipeline);
                audio.PipeTo(vad);

                var recognizer = new AzureSpeechRecognizer(pipeline, new AzureSpeechRecognizerConfiguration()
                {
                    SubscriptionKey = Program.AzureSubscriptionKey,
                    Region = Program.AzureRegion
                });
                var annotatedAudio = audio.Join(vad);
                annotatedAudio.PipeTo(recognizer);

                var finalResults = recognizer.Out.Where(result => result.IsFinal);
                finalResults.Do(SendDialogToBazaar);

                // Todo: Add some data storage here
                // var dataStore = Store.Create(pipeline, Program.AppName, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));

                pipeline.RunAsync();
                if (AudioOnly)
                {
                    Console.WriteLine("Running Smart Lab Project Demo v3.0 - Audio Only.");
                }
                else
                {
                    Console.WriteLine("Running Smart Lab Project Demo v3.0");
                }
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        private static void FindAudioSource(KinectAudioBeamInfo audioInfo, Envelope envelope)
        {
            // System.Threading.Thread.Sleep(1000);
            AudioSourceFlag = false;
            double angle = audioInfo.Angle;
            Line3D soundPlane = new Line3D(
                KinectInfo.Cam2World(new Point3D(0, 0, 0)),
                KinectInfo.Cam2World(new Point3D(Math.Cos(angle), 0, -Math.Sin(angle))) - KinectInfo.Cam2World(new Point3D(0, 0, 0))
            );
            if (IdInfoList.Count > 0)
            {
                double nearestDis = 10000;
                IdentityInfo nearestID = null;
                lock (AudioSourceLock)
                {
                    foreach (var kv in IdTail)
                    {
                        var p = kv.Value;
                        while (p.LastMatch != null)
                        {
                            if (p.LastMatch.Timestamp < envelope.OriginatingTime)
                            {
                                break;
                            }
                            p = p.LastMatch;
                        }
                        double dis = 100000;
                        if (Math.Abs(p.Timestamp.Subtract(envelope.OriginatingTime).TotalSeconds) < 8)
                        {
                            double temp = Math.Abs((p.Position - soundPlane.p0) * soundPlane.t / soundPlane.t.Length());
                            if (temp < dis)
                            {
                                dis = temp;
                            }
                        }
                        if (p.LastMatch != null && Math.Abs(p.LastMatch.Timestamp.Subtract(envelope.OriginatingTime).TotalSeconds) < 8)
                        {
                            double temp = Math.Abs((p.LastMatch.Position - soundPlane.p0) * soundPlane.t / soundPlane.t.Length());
                            if (temp < dis)
                            {
                                dis = temp;
                            }
                        }
                        if (dis < nearestDis)
                        {
                            nearestID = p;
                            nearestDis = dis;
                        }
                    }
                    if (nearestID != null)
                    {
                        // Console.WriteLine(angle);
                        // Console.WriteLine($"{nearestID.TrueIdentity}: {nearestDis}");
                        AudioSourceList.Add(nearestID.TrueIdentity);
                        if (DateTime.Now.Subtract(LastNVBGTime).TotalSeconds > NVBGCooldownAudio)
                        {
                            Point3D pos2send = nearestID.Position;
                            pos2send = VhtInfo.World2Cam(pos2send);
                            Console.WriteLine($"Send location message to NVBG: multimodal:true;%;identity:{nearestID.TrueIdentity}(Detected: {nearestID.Identity});%;location:{pos2send.x}:{pos2send.y}:{pos2send.z}");
                            manager.SendText(TopicToNVBG, $"multimodal:true;%;identity:{nearestID.TrueIdentity};%;location:{pos2send.x}:{pos2send.y}:{pos2send.z}");
                            LastNVBGTime = DateTime.Now;
                        }
                    }
                }
            }
        }

        private static void AddNewMapper(CameraSpacePoint[] mapper, Envelope envelope)
        {
            var time = envelope.OriginatingTime;
            KinectMappingBuffer.Add(time, mapper);
            while (KinectMappingBuffer.Last().Key.Subtract(KinectMappingBuffer.First().Key).TotalSeconds > 10)
            {
                var rem_time = KinectMappingBuffer.First().Key;
                KinectMappingBuffer.RemoveAt(0);
            }
        }

        private static void SendDialogToBazaar(IStreamingSpeechRecognitionResult result, Envelope envelope)
        {
            String speech = result.Text;
            if (speech != "")
            {
                if (AudioSourceList.Count > 0)
                {
                    Dictionary<string, int> temp = new Dictionary<string, int>();
                    foreach (var name in AudioSourceList)
                    {
                        if (temp.ContainsKey(name))
                        {
                            temp[name] += 1;
                        }
                        else
                        {
                            temp[name] = 1;
                        }
                    }
                    int max = 0;
                    string id = null;
                    foreach (var kv in temp)
                    {
                        if (kv.Value > max)
                        {
                            max = kv.Value;
                            id = kv.Key;
                        }
                    }
                    Console.WriteLine($"{max}, {id}");
                    if (id != null)
                    {
                        AudioSourceList.Clear();
                        String messageToBazaar = $"multimodal:true;%;identity:{id};%;speech:{result.Text}";
                        Console.WriteLine($"Send text message to Bazaar: {messageToBazaar}");
                        // Console.WriteLine("Sending message to Bazaar through NetMQ: {0}", messageToBazaar);
                        // netmqpublisher = new NetMqPublisher(TcpIPPublisher);
                        // netmqpublisher.Publish("TcpToBazaar", messageToBazaar);
                    /*    using (var pubSocket = new PublisherSocket())
                        {
                            pubSocket.Options.SendHighWatermark = 1000;
                            pubSocket.Bind(TcpIPPublisher);
                            Console.WriteLine("Sending message to Bazaar : {0}", messageToBazaar);
                            pubSocket.SendMoreFrame("TcpToBazaar").SendFrame(messageToBazaar);
                        }*/
                        
                        manager.SendText(TopicToBazaar, messageToBazaar);
                        manager.SendText(TopicToMacaw, result.Text);
                        return;
                    }
                }
                if (IdInfoList != null && IdInfoList.Count > 0)
                {
                    String messageToBazaar = $"multimodal:true;%;identity:{IdInfoList.Last().TrueIdentity};%;speech:{result.Text}";
                    Console.WriteLine($"Send text message to Bazaar: {messageToBazaar}");
                    manager.SendText(TopicToBazaar, messageToBazaar);
                    manager.SendText(TopicToMacaw, result.Text);
                    // netmqpublisher = new NetMqPublisher(TcpIPPublisher);
                    // netmqpublisher.Publish("TcpToBazaar", messageToBazaar);
                    /*using (var pubSocket = new PublisherSocket())
                    {
                        pubSocket.Options.SendHighWatermark = 1000;
                        pubSocket.Bind(TcpIPPublisher);
                        Console.WriteLine("Sending message to Bazaar : {0}", messageToBazaar);
                        pubSocket.SendMoreFrame("TcpToBazaar").SendFrame(messageToBazaar);
                    }*/
                }
                else
                {
                    String name = getRandomName();
                    String messageToBazaar = $"multimodal:true;%;identity:{name};%;speech:{result.Text}";
                    //String location = getRandomLocation(); 
                    // Console.WriteLine("Sending message to Bazaar through NetMQ: {0}", messageToBazaar);
                    // netmqpublisher = new NetMqPublisher(TcpIPPublisher);
                    // netmqpublisher.Publish("TcpToBazaar", messageToBazaar);
                   /* using (var pubSocket = new PublisherSocket())
                    {
                        pubSocket.Options.SendHighWatermark = 1000;
                        pubSocket.Bind(TcpIPPublisher);
                        Console.WriteLine("Sending message to Bazaar : {0}", messageToBazaar);
                        pubSocket.SendMoreFrame("TcpToBazaar").SendFrame(messageToBazaar);
                    }*/
                    Console.WriteLine($"Please open the Realmodal first!.Send fake text message to Bazaar: {messageToBazaar}");
                    manager.SendText(TopicToBazaar, messageToBazaar);
                    manager.SendText(TopicToMacaw, result.Text);
                }
            }
        }

        private static String getRandomName()
        {
            Random randomFunc = new Random();
            int randomNum = randomFunc.Next(0, 3);
            if (randomNum == 1)
                return "Haogang";
            else
                return "Yansen";
        }

        private static String getRandomLocation()
        {
            Random randomFunc = new Random();
            int randomNum = randomFunc.Next(0, 4);
            switch (randomNum)
            {
                case 0:
                    return "0:0:0";
                case 1:
                    return "75:100:0";
                case 2:
                    return "150:200:0";
                case 3:
                    return "225:300:0";
                default:
                    return "0:0:0";
            }
        }


        private static void Pipeline_PipelineCompleted(object sender, PipelineCompletedEventArgs e)
        {
            Console.WriteLine("Pipeline execution completed with {0} errors", e.Errors.Count);
        }

        private static void Pipeline_PipelineException(object sender, PipelineExceptionNotHandledEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }

        private static bool GetSubscriptionKey()
        {
            Console.WriteLine("A cognitive services Azure Speech subscription key is required to use this. For more info, see 'https://docs.microsoft.com/en-us/azure/cognitive-services/cognitive-services-apis-create-account'");
            Console.Write("Enter subscription key");
            Console.Write(string.IsNullOrWhiteSpace(Program.AzureSubscriptionKey) ? ": " : string.Format(" (current = {0}): ", Program.AzureSubscriptionKey));

            // Read a new key or hit enter to keep using the current one (if any)
            string response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                Program.AzureSubscriptionKey = response;
            }

            Console.Write("Enter region");
            Console.Write(string.IsNullOrWhiteSpace(Program.AzureRegion) ? ": " : string.Format(" (current = {0}): ", Program.AzureRegion));

            // Read a new key or hit enter to keep using the current one (if any)
            response = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(response))
            {
                Program.AzureRegion = response;
            }

            return !string.IsNullOrWhiteSpace(Program.AzureSubscriptionKey) && !string.IsNullOrWhiteSpace(Program.AzureRegion);
        }
    }
}

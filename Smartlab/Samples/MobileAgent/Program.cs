// This application entry point is based on ASP.NET Core new project templates and is included
// as a starting point for app host configuration.
// This file may need to be updated according to the specific scenario of the application being upgraded.
// For more information on ASP.NET Core hosting, see https://docs.microsoft.com/aspnet/core/fundamentals/host/web-host

using CMU.Smartlab.Communication;
using CMU.Smartlab.Identity;
// using CMU.Smartlab.Rtsp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
// using Microsoft.Kinect;
using Microsoft.Psi;
using Microsoft.Psi.Components;
using Microsoft.Psi.Audio;
using Microsoft.Psi.CognitiveServices;
using Microsoft.Psi.CognitiveServices.Speech;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Media;
using Microsoft.Psi.Speech;
using Microsoft.Psi.Interop.Format;
using Microsoft.Psi.Interop.Transport;
// using Microsoft.Psi.Kinect;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Transport.Discovery;
using Rectangle = System.Drawing.Rectangle;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
// using NetMQSource;
// using ZeroMQ; 
// using Operators; 


namespace SigdialDemo
{
    public class sensorIPs
    {
        public string sensorAudio { get; set; }
        public string sensorDOA { get; set; }
        public string sensorVAD { get; set; }
        public string sensorVideoText { get; set; }
    }
    public class Program
    {
        private const string AppName = "SmartLab Project - Demo v3.0 (for SigDial Demo)";
        private const string TopicToBazaar = "PSI_Bazaar_Text";
        private const string TopicToPython = "PSI_Python_Image";
        private const string TopicToMacaw = "PSI_Macaw_Text";
        private const string TopicToNVBG = "PSI_NVBG_Location";
        private const string TopicToVHText = "PSI_VHT_Text";
        private const string TopicFromPython = "Python_PSI_Location";
        private const string TopicFromBazaar = "Bazaar_PSI_Text";
        private const string TopicToAgent = "PSI_Agent_Text";
        private const string TopicFromSensor = "Sensor_PSI_Text";
        private const string TopicFaceOrientation = "face-orientation";
        private const int SendingImageWidth = 360;
        private const int MaxSendingFrameRate = 15;
        private const string TcpIPResponder = "@tcp://*:40001";
        // private const string TcpIPPublisher = "tcp://*:8080";
        private const string TcpIPPublisher = "tcp://*:30002";
        // private const string TcpIPPublisher = "tcp://*:40002";
        // private const string TcpIPPublisher = "tcp://*:5500";
        private const double SocialDistance = 183;
        private const double DistanceWarningCooldown = 30.0;
        private const double NVBGCooldownLocation = 8.0;
        private const double NVBGCooldownAudio = 3.0;
        private static string AzureSubscriptionKey = "b6ba5313943f4393abaa37e28a45de51";
        private static string AzureRegion = "eastus";
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
        public static List<String> AudioSourceList;
        public static CameraInfo VhtInfo;
        // public static String sensorVideoText = "tcp://128.2.212.138:40000";     // Nano
        // public static String sensorVideoText = "tcp://128.2.220.118:40003";     // erebor
        public static String sensorVideoText; 
        public static String sensorAudio; 
        public static String sensorDOA; 
        public static String sensorVAD; 

        // public static String sensorAudio = "tcp://128.2.212.138:40001"; 
        // public static String sensorDOA = "tcp://128.2.212.138:40002"; 
        // public static String sensorVAD = "tcp://128.2.212.138:40003"; 

        public static void Main(string[] args)
        {
            if (Initialize())    // TEMPORARY
            // if (true)
            {
                bool exit = false;
                while (!exit)
                {
                    SetConsole();
                    Console.WriteLine("############################################################################");
                    Console.WriteLine("1) Respond to requests from remote device. Then press any key to quit.");
                    Console.WriteLine("Q) Quit.");
                    ConsoleKey key = Console.ReadKey().Key;
                    Console.WriteLine();
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            RunDemo();
                            break;
                        case ConsoleKey.Q:
                            exit = true;
                            break;
                    }
                    exit = true;   // TEMPORARY for one loop only
                }
            }
            // else
            // {
            //     Console.ReadLine();
            // }
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

            return true;
        }

        // ...
        public static void RunDemo()
        {
            sensorIPs IPs = new sensorIPs(); 

            String sensorVideoText;
            using (var responseSocket = new ResponseSocket("@tcp://*:40001"))
            {
                var message = responseSocket.ReceiveFrameString();
                Console.WriteLine("RunDemo, responseSocket received '{0}'", message);
                responseSocket.SendFrame(message);
                // sensorVideoText = message; 
                IPs = JsonConvert.DeserializeObject<sensorIPs>(message);
                sensorVideoText = IPs.sensorVideoText;
                sensorAudio = IPs.sensorAudio;
                sensorDOA = IPs.sensorDOA;
                sensorVAD = IPs.sensorVAD;
                Console.WriteLine("RunDemo: sensorVideoText = '{0}'", sensorVideoText);
                Console.WriteLine("RunDemo: sensorAudio = '{0}'", sensorAudio);
            }
            Thread.Sleep(1000);


            using (var p = Pipeline.Create())
            {
                // Subscribe to messages from remote sensor using NetMQ (ZeroMQ)
                // var nmqSubFromSensor = new NetMQSubscriber<string>(p, "", sensorVideoText, MessagePackFormat.Instance, useSourceOriginatingTimes = true, name="Sensor to PSI");
                // var nmqSubFromSensor = new NetMQSubscriber<string>(p, "", sensorVideoText, JsonFormat.Instance, true, "Sensor to PSI");
                // other messages
                var nmqSubFromSensor = new NetMQSubscriber<IDictionary<string, object>>(p, "", sensorVideoText, MessagePackFormat.Instance, true, "Sensor to PSI");

                // Create a publisher for messages from the sensor to Bazaar
                var amqPubSensorToBazaar = new AMQPublisher<IDictionary<string, object>>(p, TopicFromSensor, TopicToBazaar, "Sensor to Bazaar");

                // Subscribe to messages from Bazaar for the agent
                var amqSubBazaarToAgent = new AMQSubscriber<IDictionary<string, object>>(p, TopicFromBazaar, TopicToAgent, "Bazaar to Agent");

                // Create a publisher for messages to the agent using NetMQ (ZeroMQ)
                var nmqPubToAgent = new NetMQPublisher<IDictionary<string, object>>(p, TopicFaceOrientation, TcpIPPublisher, MessagePackFormat.Instance);

                // Route messages from the sensor to Bazaar
                nmqSubFromSensor.PipeTo(amqPubSensorToBazaar.IDictionaryIn);

                // Combine messages (1) direct from sensor, and (2) from Bazaar, and send to agent
                SmartlabMerge<IDictionary<string, object>> mergeToAgent = new SmartlabMerge<IDictionary<string, object>>(p, "Merge to Agent");
                var receiverSensor = mergeToAgent.AddInput("Sensor to PSI");
                var receiverBazaar = mergeToAgent.AddInput("Bazaar to Agent");
                nmqSubFromSensor.PipeTo(receiverSensor);
                amqSubBazaarToAgent.PipeTo(receiverBazaar);
                mergeToAgent.PipeTo(nmqPubToAgent); 

                // ======================================================================================
                // vvv AUDIO SETUP vvv
                var format = WaveFormat.Create16BitPcm(16000, 1);

                // binary data stream
                var audioFromNano = new NetMQSource<byte[]>(
                    p,
                    "temp",
                    // ips.sensorAudio,  // TEMPORARY
                    sensorAudio,          // TEMPORARY
                    MessagePackFormat.Instance);

                // sensorDOA - Direction of Arrival (of sound, int values range from 0 to 360)
                var sensorDOAFromNano = new NetMQSource<int>(
                    p,
                    "temp2",
                    // ips.sensorDOA,         // TEMPORARY
                    sensorDOA,             // TEMPORARY
                    MessagePackFormat.Instance);

                var vadFromNano = new NetMQSource<int>(
                    p,
                    "temp3",
                    // ips.vad,         // TEMPORARY
                    sensorVAD,                // TEMPORARY
                    MessagePackFormat.Instance);

                // processing audio and sensorDOA input, and saving to file
                // audioFromNano contains binary array data, needs to be converted to PSI compatible AudioBuffer format
                var audioInAudioBuffer = audioFromNano
                    .Select(t =>
                    {
                        var ab = new AudioBuffer(t, format);
                        return ab;
                    });

                // saving to audio file
                // var saveToWavFile = new WaveFileWriter(p, "./psi_direct_audio_05-14-a.wav");
                // audioInAudioBuffer.PipeTo(saveToWavFile);  

                // vvvvvvvvvvvv From psi-samples SimpleVoiceActivityDetector vvvvvvvvvvvvvv

                // To run from a stored audio file
                //    -- Comment out the 'audioInAudioBuffer' declaration above
                //    -- Uncomment the two lines below and customize the file name at the end of the first line
                // var inputStore = PsiStore.Open(p, "psi_direct_audio_0.wav", Path.Combine(Directory.GetCurrentDirectory(), "Stores"));
                // audioInAudioBuffer = inputStore.OpenStream<AudioBuffer>("Audio");  // replaced microphone with audioInAudioBuffer

                var acousticFeaturesExtractor = new AcousticFeaturesExtractor(p);
                audioInAudioBuffer.PipeTo(acousticFeaturesExtractor);  // replaced microphone with audioInAudioBuffer

                // Display the log energy
                // acousticFeaturesExtractor.LogEnergy
                //     .Sample(TimeSpan.FromSeconds(0.2))
                //     .Do(logEnergy => Console.Write($"LogEnergy = {logEnergy}"));
                    // .Do(logEnergy => Console.WriteLine($"LogEnergy = {logEnergy}"));

                // Create a voice-activity stream by thresholding the log energy
                var vad = acousticFeaturesExtractor.LogEnergy
                    .Select(l => l > 10);
                
                // Create filtered signal by aggregating over historical buffers
                var vadWithHistory = acousticFeaturesExtractor.LogEnergy
                    .Window(RelativeTimeInterval.Future(TimeSpan.FromMilliseconds(300)))
                    .Aggregate(false, (previous, buffer) => (!previous && buffer.All(v => v > 10)) || (previous && !buffer.All(v => v < 10)));

                // Write the microphone output, VAD streams, and some acoustic features to the store
                // Console.WriteLine($"Writing to store");
                // var store = PsiStore.Create(p, "SimpleVAD", Path.Combine(Directory.GetCurrentDirectory(), "Stores"));
                // audioInAudioBuffer.Write("Audio", store);
                // vad.Write("VAD", store);
                // vadWithHistory.Write("VADFiltered", store);
                // acousticFeaturesExtractor.LogEnergy.Write("LogEnergy", store);
                // acousticFeaturesExtractor.ZeroCrossingRate.Write("ZeroCrossingRate", store);
                // ^^^^^^^^^^^^ From psi-samples SimpleVoiceActivityDetector ^^^^^^^^^^^^^
                

                // AUDIO [10, 283, 3972, 74.0397, ........., 835.3, 493.8]
                // VAD [0, 0, 1, 1, 1, 1,................, 0, 0] (same length as above)
                var annotatedAudio = audioInAudioBuffer.Join(vadWithHistory, TimeSpan.FromMilliseconds(100)).Select(x =>
                {
                    return (x.Item1, x.Item2);
                });

                var recognizer = new AzureSpeechRecognizer(p, new AzureSpeechRecognizerConfiguration()
                {
                    SubscriptionKey = Program.AzureSubscriptionKey,
                    Region = Program.AzureRegion
                });

                // To CHECK: 
                // What is being sent to Azure? Answer: Only audio for which voice activity detection (vad) == true
                // What are we being charged for: the time the ASR system is running or the audio duration being sent?
                annotatedAudio.PipeTo(recognizer);

                // Text transcription from Azure
                var finalResults = recognizer.Out.Where(result => result.IsFinal);

                recognizer.Select(result => "audio:" + result.Text).PipeTo(amqPubSensorToBazaar.StringIn);

                finalResults.Do((IStreamingSpeechRecognitionResult result, Envelope envelope) =>
                {
                    string text = result.Text; 
                    if (!string.IsNullOrWhiteSpace(text)) {
                        Console.WriteLine($"Sending text to Bazaar -- audio:{text}");
                    }
                });
                // ^^^ AUDIO SETUP ^^^
                // ======================================================================================

                p.RunAsync();
                Console.ReadKey();
            }
        }


        // Works sending to itself locally
        public static void RunDemoWithLocal()
        {
            using (var responseSocket = new ResponseSocket("@tcp://*:40001"))
            using (var requestSocket = new RequestSocket(">tcp://localhost:40001"))
            using (var p = Pipeline.Create())
            for (;;) 
            {
				var mq = new NetMQSource<string>(p, "test-topic", "tcp://localhost:45678", JsonFormat.Instance); 
				Console.WriteLine("requestSocket : Sending 'Hello'");
				requestSocket.SendFrame(">>>>> Hello from afar! <<<<<<");
				var message = responseSocket.ReceiveFrameString();
				Console.WriteLine("responseSocket : Server Received '{0}'", message);
				Console.WriteLine("responseSocket Sending 'Hibackatcha!'");
				responseSocket.SendFrame("Hibackatcha!");
				message = requestSocket.ReceiveFrameString();
				Console.WriteLine("requestSocket : Received '{0}'", message);
				Console.ReadLine();
				Thread.Sleep(1000);
            }
        }


        // This method tests sending & receiving over the same socket. 
        public static void RunDemoPubSubLocal()
        {
            string address = "tcp://127.0.0.1:40001";
            var pubSocket = new PublisherSocket();
            pubSocket.Options.SendHighWatermark = 1000;
            pubSocket.Bind(address);
            var subSocket = new SubscriberSocket();
            subSocket.Connect(address);
            Thread.Sleep(100);
            subSocket.SubscribeToAnyTopic();
            String received = "";

            // Testing send & receive over same socket
            for (;;) {
                for (;;) {
                    pubSocket.SendFrame( "Howdy from NetMQ!", false );
                    Console.WriteLine( "About to try subSocket.ReceiveFrameString");
                    received = subSocket.ReceiveFrameString(); 
                    if  (received == "") {
                        Console.WriteLine( "Received nothing");
                        continue; 
                    }
                    Console.WriteLine("Received something");
                    break;
                }
                Console.WriteLine(received);
                Thread.Sleep(2000);
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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

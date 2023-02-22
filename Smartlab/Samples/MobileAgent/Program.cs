// This application entry point is based on ASP.NET Core new project templates and is included
// as a starting point for app host configuration.
// This file may need to be updated according to the specific scenario of the application being upgraded.
// For more information on ASP.NET Core hosting, see https://docs.microsoft.com/aspnet/core/fundamentals/host/web-host

using CMU.Smartlab.Communication;
using CMU.Smartlab.Identity;
// using CMU.Smartlab.Rtsp;
using System;
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
// using NetMQSource;
// using ZeroMQ; 
// using Operators; 


namespace SigdialDemo
{
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

        private const int SendingImageWidth = 360;
        private const int MaxSendingFrameRate = 15;
        private const string TcpIPResponder = "@tcp://*:40001";
        private const string TcpIPPublisher = "tcp://*:40002";

        private const double SocialDistance = 183;
        private const double DistanceWarningCooldown = 30.0;
        private const double NVBGCooldownLocation = 8.0;
        private const double NVBGCooldownAudio = 3.0;

        private static string AzureSubscriptionKey = "abee363f8d89444998c5f35b6365ca38";
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
        // private readonly Merger<Message<string>, int> merger;

        public static String remoteIP; 

        public static void Main(string[] args)
        {
            SetConsole();
            if (Initialize())    // TEMPORARY
            if (true)
            {
                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("############################################################################");
                    Console.WriteLine("1) Respond to requests from remote device.");
                    ConsoleKey key = Console.ReadKey().Key;
                    Console.WriteLine();
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            RunDemo();
                            break;
                        // case ConsoleKey.Q:
                        //     exit = true;
                        //     break;
                    }
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
            String remoteIP; 
            // String localIP = "tcp://127.0.0.1:40003";

            using (var responseSocket = new ResponseSocket("@tcp://*:40001")) {
                var message = responseSocket.ReceiveFrameString();
                Console.WriteLine("RunDemoWithRemoteMultipart, responseSocket received '{0}'", message);
                responseSocket.SendFrame(message);
                remoteIP = message; 
                Console.WriteLine("RunDemoWithRemoteMultipart: remoteIP = '{0}'", remoteIP);
            }
            Thread.Sleep(1000); 


            using (var p = Pipeline.Create())
            {
                // Subscribe to messages from remote sensor using NetMQ (ZeroMQ)
                // var nmqSubFromSensor = new NetMQSubscriber<string>(p, "", remoteIP, JsonFormat.Instance, useSourceOriginatingTimes = true, name="Sensor to PSI");
                var nmqSubFromSensor = new NetMQSubscriber<string>(p, "", remoteIP, JsonFormat.Instance, true, "Sensor to PSI");

                // Create a publisher for messages from the sensor to Bazaar
                var amqPubSensorToBazaar = new AMQPublisher<string>(p, TopicFromSensor, TopicToBazaar, "Sensor to Bazaar"); 

                // Subscribe to messages from Bazaar for the agent
                var amqSubBazaarToAgent = new AMQSubscriber<string>(p, TopicFromBazaar, TopicToAgent, "Bazaar to Agent"); 

                // Create a publisher for messages to the agent using NetMQ (ZeroMQ)
                var nmqPubToAgent = new NetMQPublisher<string>(p, TopicToAgent, TcpIPPublisher, JsonFormat.Instance);
                // nmqPubToAgent.Do(x => Console.WriteLine("RunDemoWithRemoteMultipart, nmqPubToAgent.Do: {0}", x));

                // Route messages from the sensor to Bazaar
                nmqSubFromSensor.PipeTo(amqPubSensorToBazaar.StringIn); 

                // ===============================================================================
                // Have to merge two emitters before connecting to a single receiver
                // ... see https://github.com/Microsoft/psi/issues/19

                // Two emitters to a single receive not allowed
                // // Route messages from the sensor directly to the agent
                // nmqSubFromSensor.PipeTo(nmqPubToAgent);
                // // Route messages from Bazaar to the agent
                // amqSubBazaarToAgent.PipeTo(nmqPubToAgent); 
                

                // Tried the syntactic sugar method at https://github.com/Microsoft/psi/issues/19
                // nmqSubFromSensor.Merge(amqSubBazaarToAgent).PipeTo(nmqPubToAgent);


                // Merge code from https://github.com/Microsoft/psi/issues/19
                // Merge two emitters to a single receiver
                // create Merger component

                // ======= API =======
                // Merger(Pipeline, Action<TKey, Message<TIn>>, String)
                // public Merger(Pipeline pipeline, Action<TKey, Message<TIn>> action, string name = "Merger")


                // Example (doesn't work) from https://github.com/Microsoft/psi/issues/19
                // var merger = new Merger<string, int>(pipeline);
                // merger.Select(m => m.Item2.Data).PipeTo(c);

                // var merger = new Merger<string, int>(p);
                // var merger = new Merger(p,Action<int,Message<string>>,"Merge to Agent");
                // var merger = new Merger(p,Select(m => m.Item2.Data),"Merge to Agent");
                // var merger = new Merger(p,Action<int,Message<string>> (m => m.Item2.Data),"Merge to Agent");
                // var merger = new Merger(p,Action<int,Message<string>> mySelect,"Merge to Agent");
                // var merger = new Merger(p, Action<int, Message<string>>);
                // var merger = new Merger(p, Select);

                // // create 2 receivers to merge 2 streams

                // Merger<Message<string>, int> merger = new Merger<Message<string>, int>(p);
                // var receiver0 = merger.Add(0);
                // var receiver1 = merger.Add(1);

                // pipe streams to merge to the Merger
                // nmqSubFromSensor.PipeTo(receiver0);
                // amqSubBazaarToAgent.PipeTo(receiver1);

                // // pipe merged output to ConsoleOutput
                // merger.Select(m => m.Item2.Data).PipeTo(nmqPubToAgent);
                // merger.PipeTo(nmqPubToAgent);


                // ===============================================================================


                // Merger<Message<string>, int> merger = new Merger<Message<string>, int>(p);

                // Merger<Message<string>, int> merger = new Merger<Message<string>, int>(p, (_, m) =>
                // {
                //     // Select(m => m.Item2.Data); 
                //     // return m.Item2.Data; 
                //     m.Item2.Data; 
                // });
                // var receiver0 = merger.Add(0);
                // var receiver1 = merger.Add(1);

                // nmqSubFromSensor.PipeTo(receiver0);
                // amqSubBazaarToAgent.PipeTo(receiver1);

                // Try Merge class at xhttps://microsoft.github.io/psi/api/Microsoft.Psi.Components.Merge-1.html
                // Merge<String> mergeToAgent = new Merge<String>(p); 
                // mergeToAgent.addInput("Sensor to PSI"); 
                // mergeToAgent.addInput("Bazaar to Agent"); 
                // mergeToAgent.PipeTo(nmqPubToAgent); 

                // pipe merged output to ConsoleOutput
                // merger.Select(m => m.Item2.Data).PipeTo(nmqPubToAgent);

                Merge<string> mergeToAgent = new Merge<string>(p,"Merge to Agent"); 
                mergeToAgent.AddInput("Sensor to PSI"); 
                mergeToAgent.AddInput("Bazaar to Agent"); 

                // nmqSubFromSensor.PipeTo(mergeToAgent);
                // amqSubBazaarToAgent.PipeTo(mergeToAgent); 

                mergeToAgent.Select(m => m.Data).PipeTo(nmqPubToAgent); 

                // Emitter<Message<string>> mergeEmitter = mergeToAgent.Out; 
                // mergeEmitter.PipeTo(nmqPubToAgent); 
    
                // SEE ******* https://github.com/microsoft/psi/wiki/Stream-Fusion-and-Merging *******

                p.Run();

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
                    Console.WriteLine( "Received something");
                    break; 
                }
                Console.WriteLine( received );
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

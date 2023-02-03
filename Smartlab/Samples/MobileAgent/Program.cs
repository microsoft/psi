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
        private const string TopicToRemote = "PSI_Remote_Text";
        // private const string TopicFromRemote = "Remote_PSI_Text";
        private const string TopicFromRemote = "Remote_PSI_Text";
        // private const string TopicFromPython_QueryKinect = "Python_PSI_QueryKinect";
        // private const string TopicToPython_AnswerKinect = "PSI_Python_AnswerKinect";

        private const int SendingImageWidth = 360;
        private const int MaxSendingFrameRate = 15;
        private const string TcpIPResponder = "@tcp://*:40001";
        private const string TcpIPPublisher = "tcp://*:40002";
        // private const string TcpIPPublisher = "tcp://0.0.0.0:40002";
        // private const string TcpPortSubscriber = "40003";

        // private const int KinectImageWidth = 1920;
        // private const int KinectImageHeight = 1080;

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
        // public static SortedList<DateTime, CameraSpacePoint[]> KinectMappingBuffer;
        public static List<String> AudioSourceList;

        // public static CameraInfo KinectInfo;
        public static CameraInfo VhtInfo;
        // public static void Main(string[] args)
        // {
        //     CreateHostBuilder(args).Build().Run();
        // }

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
                var nmqSub = new NetMQSubscriber<string>(p, "", remoteIP, JsonFormat.Instance);
                // var nmqSub = new NetMQSubscriber<string>(p, "", localIP, JsonFormat.Instance);

                var amqRemoteToBazaar = new AMQPublisher<string>(p, TopicFromRemote, TopicToBazaar, "Remote to Bazaar"); 
                var amqBazaarToRemote = new AMQSubscriber<string>(p, TopicFromBazaar, TopicToRemote, "Bazaar to Remote"); 

                var nmqPub = new NetMQPublisher<string>(p, TopicToRemote, TcpIPPublisher, JsonFormat.Instance);
                // nmqPub.Do(x => Console.WriteLine("RunDemoWithRemoteMultipart, nmqPub.Do: {0}", x));

                // nmqSub.PipeTo(nmqPub); 
                nmqSub.PipeTo(amqRemoteToBazaar.StringIn); 
                amqBazaarToRemote.PipeTo(nmqPub); 

                // manager = new CommunicationManager(); 
                // manager.subscribe(TopicFromBazaar, ProcessText);

                // amqBazaarToPSI.PipeTo(________________); 

                p.Run();

            }
        }

        private static void ProcessText(String s)
        {
            if (s != null)
            {
                Console.WriteLine($"Program.cs, ProcessText - send to topic: {TopicToRemote}");
                Console.WriteLine($"Program.cs, ProcessText - send message:  {s}");
                manager.SendText(TopicToRemote, s);
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

namespace Smartlab_Demo_v2_1_work
{
    using CMU.Smartlab.Communication;
    using CMU.Smartlab.Identity;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.CognitiveServices;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;
    using Microsoft.Psi.Speech;
    using System.Security.Cryptography;
    using Apache.NMS.ActiveMQ.Commands;

    class Program
    {
        private const string AppName = "SmartLab Project - Demo v2.1";

        private const string LogName = @"WebcamWithAudioSample";
        private const string LogPath = @"C:\\Users\thisiswys\Videos\WebcamWithAudioSample.0005";

        private const string TopicToBazaar = "PSI_Bazaar_Text";
        private const string TopicToPython = "PSI_Python_Image";
        private const string TopicToNVBG = "PSI_NVBG_Location";
        private const string TopicToVHText = "PSI_VHT_Text";
        private const string TopicFromPython = "Python_PSI_Location";
        private const string TopicFromTextLocation = "Python_PSI_TextLocation";
        private const string TopicFromBazaar = "Bazaar_PSI_Text";

        private const int SendingImageWidth = 360;

        private static string AzureSubscriptionKey = "abee363f8d89444998c5f35b6365ca38";
        private static string AzureRegion = "eastus";
        private static string endpoint = "tcp://127.0.0.1:5569";

        private static Dictionary<string, string[]> idInfo = new Dictionary<string, string[]>();
        private static Dictionary<string, string[]> idTemp = new Dictionary<string, string[]>();

        private static CommunicationManager manager;
        private static NetMqSubscriber netsubscriber;
        private static NetMqPublisher netpublisher;
        // private static IdentityInfoProcess idProcess;

        public static readonly object SendToBazaarLock = new object();
        public static readonly object SendToPythonLock = new object();

        static void Main(string[] args)
        {
            SetConsole();
            if (Initialize())
            {
                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("############################################################################");
                    Console.WriteLine("1) Multimodal streaming. Press any key to finish streaming.");
                    Console.WriteLine("2) Audio only. Press any key to finish streaming.");
                    Console.WriteLine("Q) Quit.");
                    ConsoleKey key = Console.ReadKey().Key;
                    Console.WriteLine();
                    switch (key)
                    {
                        case ConsoleKey.D1:
                            RunDemo();
                            break;
                        case ConsoleKey.D2:
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
            manager = new CommunicationManager();
            manager.subscribe(TopicFromPython, ProcessLocation);
            manager.subscribe(TopicFromBazaar, ProcessText);
            netsubscriber = new NetMqSubscriber(endpoint);
            netsubscriber.RegisterSbuscriberAll();
            netsubscriber.RegisterSubscriber(TopicFromBazaar);
            netpublisher = new NetMqPublisher(endpoint);
            return true;
        }

        private static void ProcessLocation(byte[] b)
        {
            string text = Encoding.ASCII.GetString(b);
            string[] infos = text.Split(';');
            int num = int.Parse(infos[0]);
            if (num >= 1)
            {
                // ProcessID(text);
                Console.WriteLine($"Send location message to NVBG: multimodal:true;%;identity:someone;%;location:{infos[1]}");
                manager.SendText(TopicToNVBG, $"multimodal:true;%;identity:someone;%;location:{infos[1]}");
            }
        }

        private static void ProcessText(String s)
        {
            if (s != null)
            {
                Console.WriteLine($"Send location message to VHT: multimodal:false;%;identity:someone;%;text:{s}");
                manager.SendText(TopicToVHText, s);
            }
        }
        
        /*
        private static void ProcessID(string s)
        {
            idTemp = idProcess.MsgParse(s);
            idProcess.IdCompare(idInfo, idTemp);
        }
        */


        public static void RunDemo(bool AudioOnly=false)
        {
            using (Pipeline pipeline = Pipeline.Create())
            {
                pipeline.PipelineExceptionNotHandled += Pipeline_PipelineException;
                pipeline.PipelineCompleted += Pipeline_PipelineCompleted;

                // var store = Store.Open(pipeline, Program.LogName, Program.LogPath);
                // Send video part to Python

                // var video = store.OpenStream<Shared<EncodedImage>>("Image");
                if (!AudioOnly)
                {
                    MediaCapture webcam = new MediaCapture(pipeline, 1280, 720, 30, true);

                    // var decoded = video.Out.Decode().Out;
                    ImageSendHelper helper = new ImageSendHelper(manager, "webcam", Program.TopicToPython, Program.SendingImageWidth, Program.SendToPythonLock);
                    webcam.Out.Do(helper.SendImage);
                    // var encoded = webcam.Out.EncodeJpeg(90, DeliveryPolicy.LatestMessage).Out;
                }

                // Send audio part to Bazaar

                // var audio = store.OpenStream<AudioBuffer>("Audio");
                IProducer<AudioBuffer> audio = new AudioCapture(pipeline, new AudioCaptureConfiguration() { OutputFormat = WaveFormat.Create16kHz1Channel16BitPcm() });

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
                    Console.WriteLine("Running Smart Lab Project Demo v2.1 - Audio Only.");
                }
                else
                {
                    Console.WriteLine("Running Smart Lab Project Demo v2.0");
                }
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
            }
        }

        private static void SendDialogToBazaar(IStreamingSpeechRecognitionResult result, Envelope envelope)
        {
            String speech = result.Text; 
            if (speech != "")
            {
                String name = getRandomName();
                String location = getRandomLocation(); 
                String messageToBazaar = "multimodal:true;%;speech:" + result.Text + ";%;identity:" + name + ";%;location:" + location;
                Console.WriteLine($"Send text message to Bazaar: {messageToBazaar}");
                netpublisher.Publish(TopicToBazaar, messageToBazaar);
                //manager.SendText(TopicToBazaar, messageToBazaar);
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

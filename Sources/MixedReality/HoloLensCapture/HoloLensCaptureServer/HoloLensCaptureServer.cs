// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace HoloLensCaptureServer
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Text;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;
    using Microsoft.Psi.MixedReality;
    using Microsoft.Psi.Spatial.Euclidean;
    using OpenXRHand = Microsoft.Psi.MixedReality.OpenXR.Hand;
    using StereoKitHand = Microsoft.Psi.MixedReality.StereoKit.Hand;
    using WinRTEyes = Microsoft.Psi.MixedReality.WinRT.Eyes;

    /// <summary>
    /// Capture server to persist streams from the accompanying HoloLencCaptureApp.
    /// </summary>
    public class HoloLensCaptureServer
    {
        // version number shared by capture app and server to ensure compatiblity.
        private const string Version = "v1";

        // capture actions to execute for expected stream types
        private static readonly Dictionary<string, Action<Rendezvous.TcpSourceEndpoint>> CaptureStreamAction = new ()
        {
            {
                // CoordinateSystem
                SimplifyTypeName(typeof(CoordinateSystem).FullName),
                (t) => CaptureTcpStream<CoordinateSystem>(t, Serializers.CoordinateSystemFormat())
            },
            {
                // Ray3D
                SimplifyTypeName(typeof(Ray3D).FullName),
                (t) => CaptureTcpStream<Ray3D>(t, Serializers.Ray3DFormat())
            },
            {
                // StereoKit Hand
                SimplifyTypeName(typeof(StereoKitHand).FullName),
                (t) => CaptureTcpStream<StereoKitHand>(t, Serializers.StereoKitHandFormat())
            },
            {
                // OpenXR Hand
                SimplifyTypeName(typeof(OpenXRHand).FullName),
                (t) => CaptureTcpStream<OpenXRHand>(t, Serializers.OpenXRHandFormat())
            },
            {
                // WinRT Eyes
                SimplifyTypeName(typeof(WinRTEyes).FullName),
                (t) => CaptureTcpStream<WinRTEyes>(t, Serializers.WinRTEyesFormat(), persistFrameRate: true)
            },
            {
                // AudioBuffer
                SimplifyTypeName(typeof(AudioBuffer).FullName),
                (t) => CaptureTcpStream<AudioBuffer>(t, Serializers.AudioBufferFormat())
            },
            {
                // Shared<Image>
                SimplifyTypeName(typeof(Shared<Image>).FullName),
                (t) => ViewImageStream(CaptureTcpStream<Shared<Image>>(t, Serializers.SharedImageFormat(), largeMessage: true), t.Stream.StreamName)
            },
            {
                // Shared<EncodedImage>
                SimplifyTypeName(typeof(Shared<EncodedImage>).FullName),
                (t) => ViewImageStream(CaptureTcpStream<Shared<EncodedImage>>(t, Serializers.SharedEncodedImageFormat(), largeMessage: true, persistFrameRate: true).Decode(new ImageFromStreamDecoder(), DeliveryPolicy.LatestMessage), t.Stream.StreamName)
            },
            {
                // Shared<DepthImage>
                SimplifyTypeName(typeof(Shared<DepthImage>).FullName),
                (t) => CaptureTcpStream<Shared<DepthImage>>(t, Serializers.SharedDepthImageFormat(), largeMessage: true, persistFrameRate: true)
            },
            {
                // CameraIntrinsics
                SimplifyTypeName(typeof(CameraIntrinsics).FullName),
                (t) => CaptureTcpStream<CameraIntrinsics>(t, Serializers.CameraIntrinsicsFormat())
            },
            {
                // CalibrationPointsMap
                SimplifyTypeName(typeof(CalibrationPointsMap).FullName),
                (t) => CaptureTcpStream<CalibrationPointsMap>(t, Serializers.CalibrationPointsMapFormat(), largeMessage: true)
            },
            {
                // SceneObjectCollection
                SimplifyTypeName(typeof(SceneObjectCollection).FullName),
                (t) => CaptureTcpStream<SceneObjectCollection>(t, Serializers.SceneObjectCollectionFormat(), largeMessage: true)
            },
            {
                // PipelineDiagnostics
                SimplifyTypeName(typeof(PipelineDiagnostics).FullName),
                (t) => CaptureTcpStream<PipelineDiagnostics>(t, Serializers.PipelineDiagnosticsFormat())
            },
            {
                // int
                SimplifyTypeName(typeof(int).FullName),
                (t) => CaptureTcpStream<int>(t, Serializers.Int32Format())
            },
            {
                // (Vector3D, DateTime)[]
                SimplifyTypeName(typeof((Vector3D, DateTime)[]).FullName),
                (t) =>
                {
                    // relay *frames* of IMU samples
                    CaptureTcpStream<(Vector3D, DateTime)[]>(t, Serializers.ImuFormat());

                    // relay *individual* IMU samples
                    // GetTcpStream<(Vector3D, DateTime)[]>(Serializers.ImuFormat()).SelectManyImuSamples().Write(stream.StreamName, store);
                }
            },
            {
                // (Hand Left, Hand Right)
                SimplifyTypeName(typeof((StereoKitHand Left, StereoKitHand Right)).FullName),
                (t) => CaptureTcpStream<(StereoKitHand Left, StereoKitHand Right)>(t, Serializers.StereoKitHandsFormat(), persistFrameRate: true)
            },
            {
                // (HandXR Left, HandXR Right)
                SimplifyTypeName(typeof((OpenXRHand Left, OpenXRHand Right)).FullName),
                (t) => CaptureTcpStream<(OpenXRHand Left, OpenXRHand Right)>(t, Serializers.OpenXRHandsFormat(), persistFrameRate: true)
            },
            {
                // EncodedImageCameraView
                SimplifyTypeName(typeof(EncodedImageCameraView).FullName),
                (t) => ViewImageStream(CaptureTcpStream<EncodedImageCameraView>(t, Serializers.EncodedImageCameraViewFormat(), true, t => t.Dispose(), true).Select(v => v.ViewedObject).Decode(new ImageFromStreamDecoder(), DeliveryPolicy.LatestMessage), t.Stream.StreamName)
            },
            {
                // ImageCameraView
                SimplifyTypeName(typeof(ImageCameraView).FullName),
                (t) => ViewImageStream(CaptureTcpStream<ImageCameraView>(t, Serializers.ImageCameraViewFormat(), true, t => t.Dispose(), true).Select(v => v.ViewedObject), t.Stream.StreamName)
            },
            {
                // DepthImageCameraView
                SimplifyTypeName(typeof(DepthImageCameraView).FullName),
                (t) => CaptureTcpStream<DepthImageCameraView>(t, Serializers.DepthImageCameraViewFormat(), true, t => t.Dispose(), true)
            },
        };

        private static readonly RendezvousServer RendezvousServer = new ();
        private static Pipeline captureServerPipeline = null;
        private static PsiExporter captureServerStore = null;
        private static string logFile = null;
        private static Dictionary<string, StreamStatistics> statistics = null;
        private static ImageViewer imageViewer = null;

        private static void Main(string[] args)
        {
            // Listen for the client app (HoloLensCaptureApp) to connect and create the pipeline
            RendezvousServer.Rendezvous.ProcessAdded += (_, process) =>
            {
                ReportProcessAdded(process);

                if (process.Name == "HoloLensCaptureApp")
                {
                    if (process.Version != Version)
                    {
                        throw new Exception($"Connection received from unexpected version of HoloLensCaptureApp (expected {Version}, actual {process.Version}).");
                    }

                    Console.WriteLine($"  Starting Capture Server Pipeline");
                    CreateAndRunComputeServerPipeline(process);
                    captureServerPipeline.PipelineExceptionNotHandled += (_, args) =>
                    {
                        StopComputeServerPipeline($"SERVER PIPELINE RUNTIME EXCEPTION: {args.Exception.Message}");
                    };
                }
                else if (process.Name != nameof(HoloLensCaptureServer))
                {
                    throw new Exception($"Connection received from unexpected process named {process.Name}.");
                }
            };

            // When the client app is stopped (removed), disposed of the capture server pipeline
            RendezvousServer.Rendezvous.ProcessRemoved += (_, process) =>
            {
                ReportProcessRemoved(process);
                if (process.Name == "HoloLensCaptureApp")
                {
                    StopComputeServerPipeline("Client stopped recording");
                }
            };

            RendezvousServer.Error += (_, ex) =>
            {
                Console.WriteLine();
                StopComputeServerPipeline($"RENDEZVOUS ERROR: {ex.Message}");
            };

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                StopComputeServerPipeline($"SERVER EXITED");
            };

            RendezvousServer.Start();
            Console.WriteLine($"Listening on TCP port {RendezvousServer.DefaultPort} for client HoloLensCaptureApp.");
            Console.WriteLine("Be sure to check firewall settings (may need to enable Public).");

            // Wait to press a key to exit
            Console.WriteLine("Press Q or ENTER key to exit.");
            while (true)
            {
                switch (Console.ReadKey().Key)
                {
                    case ConsoleKey.Q:
                    case ConsoleKey.Enter:
                        RendezvousServer.Stop();
                        StopComputeServerPipeline("Server manually stopped");
                        Environment.Exit(0);
                        break;
                    case ConsoleKey.V:
                        imageViewer?.ShowWindow();
                        break;
                }
            }
        }

        private static void CreateAndRunComputeServerPipeline(Rendezvous.Process inputRendezvousProcess)
        {
            var config = ConfigurationManager.AppSettings;
            var storeName = config["storeName"];
            var storePath = config["storePath"];
            var diagnosticsInterval = double.Parse(config["diagnosticsIntervalSeconds"]);

            // Create the pipeline, store and output diagnostics
            if (captureServerPipeline != null)
            {
                StopComputeServerPipeline("CLIENT STARTED NEW RECORDING WHILE PREVIOUS STILL RUNNING");
            }

            captureServerPipeline = Pipeline.Create(
                enableDiagnostics: diagnosticsInterval > 0,
                diagnosticsConfiguration: new DiagnosticsConfiguration()
                {
                    SamplingInterval = TimeSpan.FromSeconds(diagnosticsInterval),
                });
            captureServerStore = PsiStore.Create(captureServerPipeline, storeName, storePath);

            captureServerPipeline.Diagnostics.Write("ServerDiagnostics", captureServerStore);

            // Connect to remote clock on the client app to synchronize clocks
            foreach (var endpoint in inputRendezvousProcess.Endpoints)
            {
                if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockExporterEndpoint)
                {
                    var remoteClock = remoteClockExporterEndpoint.ToRemoteClockImporter(captureServerPipeline);
                    Console.Write("    Connecting to clock sync ...");
                    if (!remoteClock.Connected.WaitOne(10000))
                    {
                        Console.WriteLine("FAILED.");
                        throw new Exception("Failed to connect to remote clock exporter.");
                    }

                    Console.WriteLine("DONE.");
                }
            }

            statistics = new ();
            foreach (var endpoint in inputRendezvousProcess.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint tcpEndpoint && tcpEndpoint.Stream is not null)
                {
                    // Determine the correct action to execute for capturing the rendezvous stream,
                    // based on a simplified version of the stream's type name.
                    var simpleTypeName = SimplifyTypeName(tcpEndpoint.Stream.TypeName);

                    if (!CaptureStreamAction.ContainsKey(simpleTypeName))
                    {
                        throw new Exception($"Unknown stream type: {tcpEndpoint.Stream.StreamName} ({tcpEndpoint.Stream.TypeName})");
                    }

                    CaptureStreamAction[simpleTypeName](tcpEndpoint);
                }
                else if (endpoint is not Rendezvous.RemoteClockExporterEndpoint)
                {
                    throw new Exception("Unexpected endpoint type.");
                }
            }

            // Send a server heartbeat
            var serverHeartbeat = Generators.Sequence(
                captureServerPipeline,
                (0f, 0f),
                _ =>
                {
                    if (statistics.TryGetValue("VideoEncodedImageCameraView", out var videoStats))
                    {
                        if (statistics.TryGetValue("DepthImageCameraView", out var depthStats))
                        {
                            return ((float)videoStats.MessagesPerSecond,
                                    (float)depthStats.MessagesPerSecond);
                        }
                        else
                        {
                            return ((float)videoStats.MessagesPerSecond, 0.0f);
                        }
                    }
                    else
                    {
                        if (statistics.TryGetValue("DepthImageCameraView", out var depthStats))
                        {
                            return (0.0f, (float)depthStats.MessagesPerSecond);
                        }
                        else
                        {
                            return (0.0f, 0.0f);
                        }
                    }
                },
                TimeSpan.FromSeconds(0.2) /* 5Hz */);
            serverHeartbeat.Write("ServerHeartbeat", captureServerStore);
            var heartbeatTcpSource = new TcpWriter<(float, float)>(captureServerPipeline, 16000, Serializers.HeartbeatFormat());
            serverHeartbeat.PipeTo(heartbeatTcpSource);
            RendezvousServer.Rendezvous.TryAddProcess(
                new Rendezvous.Process(
                    nameof(HoloLensCaptureServer),
                    new[] { heartbeatTcpSource.ToRendezvousEndpoint("0.0.0.0", "ServerHeartbeat") }, // dummy host name, ignored by app
                    Version));

            // Report statistics to console
            logFile = Path.Combine(captureServerStore.Path, "CaptureLog.txt");
            Generators.Sequence(
                captureServerPipeline,
                string.Empty,
                _ =>
                {
                    var sb = new StringBuilder();
                    foreach (var kv in statistics)
                    {
                        sb.Append($"{kv.Key}: {kv.Value}\n");
                    }

                    return sb.ToString();
                },
                TimeSpan.FromSeconds(10))
                .Do(log =>
                {
                    Console.WriteLine();
                    Console.WriteLine(log);
                    File.WriteAllText(logFile, $"Capture Statistics\nVersion: {Version}\n\n{log}\n\nIn progress... ");
                });

            // Run the pipeline
            captureServerPipeline.RunAsync();
            Console.WriteLine("    Running...");
            Console.WriteLine();
            Console.WriteLine("Press V to view camera stream.");
        }

        private static IProducer<T> CaptureTcpStream<T>(
            Rendezvous.TcpSourceEndpoint tcpEndpoint,
            IFormatDeserializer deserializer,
            bool largeMessage = false,
            Action<T> deallocator = null,
            bool persistFrameRate = false)
        {
            var streamName = tcpEndpoint.Stream.StreamName;
            var tcpSource = tcpEndpoint.ToTcpSource<T>(captureServerPipeline, deserializer, deallocator: deallocator);
            var stats = new StreamStatistics();
            statistics.Add(streamName, stats);
            tcpSource
                .Do((_, e) => stats.ReportMessage(e.OriginatingTime))
                .Write(streamName, captureServerStore, largeMessage);

            if (persistFrameRate)
            {
                tcpSource
                    .Window(TimeSpan.FromSeconds(-3), TimeSpan.Zero, DeliveryPolicy.SynchronousOrThrottle)
                    .Select(b => b.Length / 3.0, DeliveryPolicy.SynchronousOrThrottle)
                    .Write($"{streamName}.AvgFrameRate", captureServerStore);
            }

            return tcpSource;
        }

        private static void StopComputeServerPipeline(string message)
        {
            if (captureServerPipeline != null)
            {
                RendezvousServer.Rendezvous.TryRemoveProcess(nameof(HoloLensCaptureServer));
                RendezvousServer.Rendezvous.TryRemoveProcess("HoloLensCaptureApp");
                captureServerPipeline?.Dispose();
                if (captureServerPipeline != null)
                {
                    Console.WriteLine($"  Stopped Capture Server Pipeline.");
                }

                captureServerPipeline = null;
            }

            if (logFile != null)
            {
                using var writer = File.AppendText(logFile);
                writer.Write($"Complete!\n\n({message})");
                logFile = null;
            }
        }

        private static void ReportProcessAdded(Rendezvous.Process process)
        {
            Console.WriteLine();
            Console.WriteLine($"PROCESS ADDED: {process.Name}");
            foreach (var endpoint in process.Endpoints)
            {
                if (endpoint is Rendezvous.TcpSourceEndpoint tcpEndpoint)
                {
                    Console.WriteLine($"  ENDPOINT: TCP {tcpEndpoint.Host} {tcpEndpoint.Port}");
                }
                else if (endpoint is Rendezvous.NetMQSourceEndpoint netMQEndpoint)
                {
                    Console.WriteLine($"  ENDPOINT: NetMQ {netMQEndpoint.Address}");
                }
                else if (endpoint is Rendezvous.RemoteExporterEndpoint remoteExporterEndpoint)
                {
                    Console.WriteLine($"  ENDPOINT: Remote {remoteExporterEndpoint.Host} {remoteExporterEndpoint.Port} {remoteExporterEndpoint.Transport}");
                }
                else if (endpoint is Rendezvous.RemoteClockExporterEndpoint remoteClockExporterEndpoint)
                {
                    Console.WriteLine($"  ENDPOINT: Remote Clock {remoteClockExporterEndpoint.Host} {remoteClockExporterEndpoint.Port}");
                }
                else
                {
                    throw new ArgumentException($"Unknown type of Endpoint ({endpoint.GetType().Name}).");
                }

                foreach (var stream in endpoint.Streams)
                {
                    Console.WriteLine($"    STREAM: {stream.StreamName} ({stream.TypeName.Split(',')[0]})");
                }
            }
        }

        private static void ReportProcessRemoved(Rendezvous.Process process)
        {
            Console.WriteLine();
            Console.WriteLine($"PROCESS REMOVED: {process.Name}");
        }

        private static void ViewImageStream(IProducer<Shared<Image>> stream, string title)
        {
            if (title.StartsWith("Video") /* not including preview, grey-cameras, etc. */)
            {
                imageViewer = new ImageViewer(captureServerPipeline, $"Image Viewer: {title}", false);
                stream.PipeTo(imageViewer, DeliveryPolicy.LatestMessage);
            }
        }

        /// <summary>
        /// Simplify the full type name into just the basic underlying type names,
        /// stripping away details like assembly, version, culture, token, etc.
        /// For example, for the type (Vector3D, DateTime)[]
        /// Input: "System.ValueTuple`2
        ///     [[MathNet.Spatial.Euclidean.Vector3D, MathNet.Spatial, Version=0.6.0.0, Culture=neutral, PublicKeyToken=000000000000],
        ///     [System.DateTime, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=000000000000]]
        ///     [], System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=000000000000"
        /// Output: "System.ValueTuple`2[[MathNet.Spatial.Euclidean.Vector3D],[System.DateTime]][]".
        /// </summary>
        private static string SimplifyTypeName(string typeName)
        {
            static string SubstringToComma(string s)
            {
                var commaIndex = s.IndexOf(',');
                if (commaIndex >= 0)
                {
                    return s.Substring(0, commaIndex);
                }
                else
                {
                    return s;
                }
            }

            // Split first on open bracket, then on closed bracket
            var allSplits = new List<string[]>();
            foreach (var openSplit in typeName.Split('['))
            {
                allSplits.Add(openSplit.Split(']'));
            }

            // Re-assemble into a simplified string (without assembly, version, culture, token, etc).
            var assembledString = string.Empty;
            for (int i = 0; i < allSplits.Count; i++)
            {
                // Add back an open bracket (except the first time)
                if (i != 0)
                {
                    assembledString += "[";
                }

                for (int j = 0; j < allSplits[i].Length; j++)
                {
                    // Remove everything after the comma (assembly, version, culture, token, etc).
                    assembledString += SubstringToComma(allSplits[i][j]);

                    // Add back a closed bracket (except the last time)
                    if (j != allSplits[i].Length - 1)
                    {
                        assembledString += "]";
                    }
                }
            }

            return assembledString;
        }

        private class StreamStatistics
        {
            private readonly Queue<DateTime> pastOriginatingTimes = new ();

            /// <summary>
            /// Gets First message time.
            /// </summary>
            public DateTime FirstMessage { get; private set; } = DateTime.MinValue;

            /// <summary>
            /// Gets last message time.
            /// </summary>
            public DateTime LastMessage { get; private set; } = DateTime.MaxValue;

            /// <summary>
            /// Gets message count.
            /// </summary>
            public long MessageCount { get; private set; } = 0;

            /// <summary>
            /// Gets messages per second.
            /// </summary>
            public double MessagesPerSecond { get; private set; } = 0;

            public void ReportMessage(DateTime originatingTime)
            {
                this.MessageCount++;
                this.LastMessage = originatingTime;
                if (this.FirstMessage == DateTime.MinValue)
                {
                    this.FirstMessage = originatingTime;
                }

                // compute messages per second over 5 sec sliding window
                var windowSeconds = TimeSpan.FromSeconds(5);
                this.pastOriginatingTimes.Enqueue(originatingTime);
                while (this.pastOriginatingTimes.Peek() <= originatingTime - windowSeconds)
                {
                    this.pastOriginatingTimes.Dequeue();
                }

                this.MessagesPerSecond = (double)this.pastOriginatingTimes.Count / windowSeconds.TotalSeconds;
            }

            public override string ToString()
            {
                if (this.FirstMessage == DateTime.MinValue)
                {
                    return "No messages";
                }

                var elapsed = this.LastMessage - this.FirstMessage;
                var mps = this.MessageCount / elapsed.TotalSeconds;
                return $"{mps:0.#}/s (frames={this.MessageCount} time={elapsed})";
            }
        }
    }
}

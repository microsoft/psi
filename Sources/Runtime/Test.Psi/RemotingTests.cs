// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// #define ShellExecute
namespace Test.Psi
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Remoting;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RemotingTests
    {
        private static readonly string ServerDataStream = "server_data";

        [TestMethod]
        [Timeout(60000)]
        public void OneWayUDP()
        {
            var server = this.StartServer(nameof(this.OneWayUDPServer));
            this.ReceiveAndValidate(server);
        }

        public void OneWayUDPServer()
        {
            this.GenerateData(TransportKind.Udp);
        }

        [TestMethod]
        [Timeout(60000)]
        public void OneWayPipes()
        {
            var server = this.StartServer(nameof(this.OneWayPipesServer));
            this.ReceiveAndValidate(server);
        }

        public void OneWayPipesServer()
        {
            this.GenerateData(TransportKind.NamedPipes);
        }

        [TestMethod]
        [Timeout(60000)]
        public void OneWayTCP()
        {
            var server = this.StartServer(nameof(this.OneWayTCPServer));
            this.ReceiveAndValidate(server);
        }

        public void OneWayTCPServer()
        {
            this.GenerateData(TransportKind.Tcp);
        }

        private void ReceiveAndValidate(Process server)
        {
            var data = new List<int>();
            int count = 0;
            var doneEvt = new ManualResetEvent(false);

            using (var p = Pipeline.Create())
            {
                using (var remote = new RemoteImporter(p, TimeInterval.Infinite, Environment.MachineName))
                {
                    Console.WriteLine("Connecting");
                    if (!remote.Connected.WaitOne(-1))
                    {
                        server.Kill();
                        throw new Exception("Could not connect to server.");
                    }

                    Console.WriteLine("Connected");

                    // wait for the stream to be created in the remote server
                    int retryIntervalMs = 10;
                    int maxRetries = 1000;
                    int retries = 0;
                    while (!remote.Importer.Contains(ServerDataStream) && retries++ < maxRetries)
                    {
                        Thread.Sleep(retryIntervalMs);
                        if (retries == maxRetries)
                        {
                            server.Kill();
                            throw new Exception($"Stream {ServerDataStream} was not created within the allotted time.");
                        }
                    }

                    var importer = remote.Importer;
                    var dataStream = importer.OpenStream<int>(ServerDataStream);
                    dataStream.Do(data.Add).Select(i => ++count).Where(c => c == 100).Do(_ => doneEvt.Set());
                    p.RunAsync();
                    doneEvt.WaitOne(20000);
#if !ShellExecute
                    server.StandardInput.WriteLine();
                    server.StandardInput.WriteLine(); // the test executon framework is also waiting for a line
#endif
                    server.WaitForExit();
                    Console.WriteLine("Server completed.");
                }
            }

            Console.WriteLine("Checking.");
            Assert.AreEqual(100, data.Count);
            Assert.AreEqual(50 * 99, data.Sum());
            Console.WriteLine("Data looks valid.");
        }

        /// <summary>
        /// Starts the Test.Psi.exe process with the specified entry point (needs to be a public method)
        /// </summary>
        /// <param name="entryPoint">The name of a public mehtod. Doesn't need to have the [TestMethod] annotation.</param>
        /// <returns>A process. Caller should ensure the process terminates (e.g via process.Kill)</returns>
        private Process StartServer(string entryPoint)
        {
            var fileName = Assembly.GetExecutingAssembly().Location;
            var procInfo = new ProcessStartInfo("dotnet", $"{fileName} !{entryPoint}");
#if !ShellExecute
            procInfo.UseShellExecute = false;
            procInfo.RedirectStandardInput = true;
            procInfo.RedirectStandardOutput = true;
            procInfo.RedirectStandardError = true;
#endif
            Environment.CurrentDirectory = Path.GetDirectoryName(fileName);
            var server = Process.Start(procInfo);

            // ensure that the stdout/stderr streams don't fill up (this causes the server to block)
            server.BeginOutputReadLine();
            server.BeginErrorReadLine();

            return server;
        }

        // entry point for the server. The process can be terminated with process.StandardInput.WriteLine().
        private void GenerateData(TransportKind transportKind)
        {
            using (var p = Pipeline.Create())
            {
                using (var remote = new RemoteExporter(p, transportKind))
                {
                    var exporter = remote.Exporter;
                    var generator = Generators.Range(p, 0, 100, TimeSpan.FromMilliseconds(1));
                    generator.Write(ServerDataStream, exporter);
                    p.RunAsync();
                    Console.ReadLine();
                }
            }
        }
    }
}

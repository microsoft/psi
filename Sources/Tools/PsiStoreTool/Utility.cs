// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PsiStoreTool
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Format;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.Interop.Transport;

    /// <summary>
    /// Psi store utility methods.
    /// </summary>
    internal static class Utility
    {
        /// <summary>
        /// List streams within store.
        /// </summary>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <returns>Success flag.</returns>
        internal static int ListStreams(string store, string path)
        {
            Console.WriteLine($"Available Streams (store={store}, path={path})");
            using (var pipeline = Pipeline.Create())
            {
                var data = Store.Open(pipeline, store, Path.GetFullPath(path));
                var count = 0;
                foreach (var stream in data.AvailableStreams)
                {
                    Console.WriteLine($"{stream.Name} ({stream.TypeName.Split(',')[0]})");
                    count++;
                }

                Console.WriteLine($"Count: {count}");
            }

            return 0;
        }

        /// <summary>
        /// Display stream metadata info.
        /// </summary>
        /// <param name="stream">Stream name.</param>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <returns>Success flag.</returns>
        internal static int DisplayStreamInfo(string stream, string store, string path)
        {
            Console.WriteLine($"Stream Metadata (stream={stream}, store={store}, path={path})");
            using (var pipeline = Pipeline.Create())
            {
                var data = Store.Open(pipeline, store, Path.GetFullPath(path));
                var meta = data.AvailableStreams.First(s => s.Name == stream);
                Console.WriteLine($"ID: {meta.Id}");
                Console.WriteLine($"Name: {meta.Name}");
                Console.WriteLine($"TypeName: {meta.TypeName}");
                Console.WriteLine($"MessageCount: {meta.MessageCount}");
                Console.WriteLine($"AverageFrequency: {meta.AverageFrequency}");
                Console.WriteLine($"AverageLatency: {meta.AverageLatency}");
                Console.WriteLine($"AverageMessageSize: {meta.AverageMessageSize}");
                Console.WriteLine($"FirstMessageOriginatingTime: {meta.FirstMessageOriginatingTime}");
                Console.WriteLine($"LastMessageOriginatingTime: {meta.LastMessageOriginatingTime}");
                Console.WriteLine($"IsClosed: {meta.IsClosed}");
                Console.WriteLine($"IsIndexed: {meta.IsIndexed}");
                Console.WriteLine($"IsPersisted: {meta.IsPersisted}");
                Console.WriteLine($"IsPolymorphic: {meta.IsPolymorphic}");
                if (meta.IsPolymorphic)
                {
                    Console.WriteLine("RuntimeTypes:");
                    foreach (var type in meta.RuntimeTypes.Values)
                    {
                        Console.WriteLine(type);
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// Print (first n) messages from stream.
        /// </summary>
        /// <param name="stream">Stream name.</param>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="number">Number of messages to display.</param>
        /// <returns>Success flag.</returns>
        internal static int DisplayStreamMessages(string stream, string store, string path, int number)
        {
            Console.WriteLine($"Stream Messages (stream={stream}, store={store}, path={path}, number={number})");
            using (var pipeline = Pipeline.Create())
            {
                var count = 0;
                var data = Store.Open(pipeline, store, Path.GetFullPath(path));
                data.OpenDynamicStream(stream).Do((m, e) =>
                {
                    if (count++ < number)
                    {
                        PrintMessage(m, e);
                    }
                });
                pipeline.RunAsync();

                while (count < number)
                {
                    Thread.Sleep(100);
                }
            }

            return 0;
        }

        /// <summary>
        /// Persist messages from stream to the file system.
        /// </summary>
        /// <param name="stream">Stream name.</param>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="file">Output file.</param>
        /// <param name="format">File format to which to serialize.</param>
        /// <returns>Success flag.</returns>
        internal static int SaveStreamMessages(string stream, string store, string path, string file, string format)
        {
            Console.WriteLine($"Saving Stream Messages (stream={stream}, store={store}, path={path}, file={file}, format={format})");
            return TransportStreamMessages(stream, store, path, p => new FileWriter<dynamic>(p, file, GetFormatSerializer(format)));
        }

        /// <summary>
        /// Persist messages from stream to the file system.
        /// </summary>
        /// <param name="stream">Stream name.</param>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="topic">Message queue topic name.</param>
        /// <param name="address">Connection address (e.g. "tcp://localhost:12345").</param>
        /// <param name="format">File format to which to serialize.</param>
        /// <returns>Success flag.</returns>
        internal static int SendStreamMessages(string stream, string store, string path, string topic, string address, string format)
        {
            Console.WriteLine($"Saving Stream Messages (stream={stream}, store={store}, path={path}, topic={topic}, address={address}, format={format})");
            return TransportStreamMessages(stream, store, path, p => new NetMQWriter<dynamic>(p, topic, address, GetFormatSerializer(format)));
        }

        /// <summary>
        /// Transport messages from stream to the file system, message queue, etc.
        /// </summary>
        /// <param name="stream">Stream name.</param>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="transport">Transport component (FileWriter, NetMQWriter, etc.)</param>
        /// <returns>Success flag.</returns>
        private static int TransportStreamMessages(string stream, string store, string path, Func<Pipeline, IConsumer<dynamic>> transport)
        {
            using (var pipeline = Pipeline.Create())
            {
                var data = Store.Open(pipeline, store, Path.GetFullPath(path));
                var messages = data.OpenDynamicStream(stream);
                messages.PipeTo(transport(pipeline));
                messages.Count().Do(i =>
                {
                    if (i % 100 == 0)
                    {
                        Console.Write('.');
                    }
                });
                pipeline.Run();
                Console.WriteLine();
            }

            return 0;
        }

        /// <summary>
        /// Map format name to implementation.
        /// </summary>
        /// <param name="format">Format name.</param>
        /// <returns>Persistent format serializer (dynamic, because may be used as IFormatSerializer or IPersistentFormatSerializer).</returns>
        private static dynamic GetFormatSerializer(string format)
        {
            switch (format)
            {
                case "msg": return MessagePackFormat.Instance;
                case "json": return JsonFormat.Instance;
                case "csv": return CsvFormat.Instance;
                default: throw new ArgumentException($"Unknown format specifier: {format}");
            }
        }

        /// <summary>
        /// Print individual dynamic node.
        /// </summary>
        /// <param name="node">Dynamic node.</param>
        /// <param name="indent">Indent level.</param>
        private static void PrintNode(dynamic node, int indent)
        {
            Func<int, string> indentStr = n => new string(' ', (indent + n) * 2);

            if (indent > 10)
            {
                // too deep - may be cyclic
                Console.WriteLine($"{indentStr(1)}...");
                return;
            }

            if (node == null)
            {
                Console.WriteLine($"{indentStr(0)}null");
                return;
            }

            if (node is ExpandoObject)
            {
                // structure
                foreach (var field in node as IDictionary<string, object>)
                {
                    if (field.Value is ExpandoObject || field.Value is dynamic[])
                    {
                        Console.WriteLine($"{indentStr(0)}{field.Key}:");
                        PrintNode(field.Value, indent + 1);
                    }
                    else
                    {
                        Console.WriteLine($"{indentStr(0)}{field.Key}: {field.Value}");
                    }
                }
            }
            else if (node is dynamic[])
            {
                // array
                var i = 0;
                foreach (var element in node)
                {
                    if (++i > 3)
                    {
                        Console.WriteLine($"{indentStr(1)}...");
                        break;
                    }

                    PrintNode(element, indent + 1);
                }
            }
            else
            {
                // simple primitive
                Console.WriteLine($"{indentStr(0)}{node}");
            }
        }

        /// <summary>
        /// Print individual dynamic message.
        /// </summary>
        /// <param name="message">Dynamic message.</param>
        /// <param name="envelope">Message envelope.</param>
        private static void PrintMessage(dynamic message, Envelope envelope)
        {
            Console.WriteLine($"Originating Time: {envelope.OriginatingTime}");
            Console.WriteLine("Message:");
            PrintNode(message, 1);
        }
    }
}
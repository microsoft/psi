// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace PsiStoreTool
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Dynamic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using Microsoft.Psi;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Interop.Format;
    using Microsoft.Psi.Interop.Transport;

    /// <summary>
    /// Psi store utility methods.
    /// </summary>
    internal static class Utility
    {
        // The name of this application.
        private const string ApplicationName = "PsiStoreTool";

        // The list of default assemblies which will be searched for task definitions.
        private static string[] defaultAssemblies = new[] { "PsiStoreTool.dll" };

        // The number of characters in each line in the console.
        private static int consoleLineWidth = 80;

        /// <summary>
        /// List streams within store.
        /// </summary>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="showSize">Indicates whether to show the stream size information.</param>
        /// <returns>Success flag.</returns>
        internal static int ListStreams(string store, string path, bool showSize = false)
        {
            var stringBuilder = new StringBuilder();

            using (var pipeline = Pipeline.Create())
            {
                var data = PsiStore.Open(pipeline, store, Path.GetFullPath(path));

                stringBuilder.AppendLine($"{data.AvailableStreams.Count()} Available Streams (store={store}, path={path})");
                if (showSize)
                {
                    stringBuilder.AppendLine("[Avg. Message Size / Total Size]; * marks indexed streams");
                    foreach (var stream in data.AvailableStreams.OrderByDescending(s => (double)s.MessageCount * s.AverageMessageSize))
                    {
                        var psiStream = stream as PsiStreamMetadata;
                        if (psiStream == null)
                        {
                            throw new NotSupportedException("Currently, only Psi Stores are supported.");
                        }

                        var isIndexed = psiStream.IsIndexed ? "* " : "  ";
                        stringBuilder.AppendLine($"{isIndexed}[{(double)stream.AverageMessageSize / 1024:0.00}Kb / {(stream.AverageMessageSize * (double)stream.MessageCount) / (1024 * 1024):0.00}Mb] {stream.Name} ({stream.TypeName.Split(',')[0]})");
                    }
                }
                else
                {
                    foreach (var stream in data.AvailableStreams)
                    {
                        stringBuilder.AppendLine($"{stream.Name} ({stream.TypeName.Split(',')[0]})");
                    }
                }
            }

            Console.WriteLine(stringBuilder.ToString());

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
                var data = PsiStore.Open(pipeline, store, Path.GetFullPath(path));
                var meta = data.AvailableStreams.First(s => s.Name == stream) as PsiStreamMetadata;
                if (meta == null)
                {
                    throw new NotSupportedException("Currently, only Psi Stores are supported.");
                }

                Console.WriteLine($"ID: {meta.Id}");
                Console.WriteLine($"Name: {meta.Name}");
                Console.WriteLine($"TypeName: {meta.TypeName}");
                Console.WriteLine($"SupplementalMetadataTypeName: {meta.SupplementalMetadataTypeName}");
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
        /// Removes a stream from the store.
        /// </summary>
        /// <param name="stream">Stream name.</param>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <returns>Success flag.</returns>
        internal static int RemoveStream(string stream, string store, string path)
        {
            string tempFolderPath = Path.Combine(path, $"Copy-{Guid.NewGuid()}");

            // copy all streams to the new path, excluding the specified stream by name
            PsiStore.Copy((store, path), (store, tempFolderPath), null, s => s.Name != stream, false);

            // create a SafeCopy folder in which to save the original store files
            var safeCopyPath = Path.Combine(path, $"Original-{Guid.NewGuid()}");
            Directory.CreateDirectory(safeCopyPath);

            // Move the original store files to the BeforeRepair folder. Do this even if the deleteOldStore
            // flag is true, as deleting the original store files immediately may occasionally fail. This can
            // happen because the InfiniteFileReader disposes of its MemoryMappedView in a background
            // thread, which may still be in progress. If deleteOldStore is true, we will delete the
            // BeforeRepair folder at the very end (by which time any open MemoryMappedViews will likely
            // have finished disposing).
            foreach (var file in Directory.EnumerateFiles(path))
            {
                var fileInfo = new FileInfo(file);
                File.Move(file, Path.Combine(safeCopyPath, fileInfo.Name));
            }

            // move the repaired store files to the original folder
            foreach (var file in Directory.EnumerateFiles(Path.Combine(tempFolderPath)))
            {
                var fileInfo = new FileInfo(file);
                File.Move(file, Path.Combine(path, fileInfo.Name));
            }

            // cleanup temporary folder
            Directory.Delete(tempFolderPath, true);

            // delete the old store files
            Directory.Delete(safeCopyPath, true);

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
                var data = PsiStore.Open(pipeline, store, Path.GetFullPath(path));
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
        /// Concatenate a set of stores, generating a new store.
        /// </summary>
        /// <param name="stores">Store names (semicolon separated).</param>
        /// <param name="path">Store path.</param>
        /// <param name="output">Output store name.</param>
        /// <returns>Success flag.</returns>
        internal static int ConcatenateStores(string stores, string path, string output)
        {
            Console.WriteLine($"Concatenating stores (stores={stores}, path={path}, output={output})");
            PsiStore.Concatenate(stores.Split(';').Select(s => (s, path)), (output, path), new Progress<double>(p => Console.WriteLine($"Progress: {p * 100.0:F2}%")), Console.WriteLine);
            return 0;
        }

        /// <summary>
        /// Crop a store, generating a new store.
        /// </summary>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="output">Output store name.</param>
        /// <param name="start">Start time relative to beginning.</param>
        /// <param name="length">Length relative to start.</param>
        /// <returns>Success flag.</returns>
        internal static int CropStore(string store, string path, string output, string start, string length)
        {
            Console.WriteLine($"Cropping store (store={store}, path={path}, output={output}, start={start}, length={length})");

            var startTime = TimeSpan.Zero; // start at beginning if unspecified
            if (!string.IsNullOrWhiteSpace(start) && !TimeSpan.TryParse(start, CultureInfo.InvariantCulture, out startTime))
            {
                throw new Exception($"Could not parse start time '{start}' (see https://docs.microsoft.com/en-us/dotnet/api/system.timespan.parse for valid format).");
            }

            var lengthRelativeInterval = RelativeTimeInterval.LeftBounded(TimeSpan.Zero); // continue to end if unspecified
            if (!string.IsNullOrWhiteSpace(length))
            {
                if (TimeSpan.TryParse(length, CultureInfo.InvariantCulture, out var lengthTimeSpan))
                {
                    lengthRelativeInterval = RelativeTimeInterval.Future(lengthTimeSpan);
                }
                else
                {
                    throw new Exception($"Could not parse length '{length}' (see https://docs.microsoft.com/en-us/dotnet/api/system.timespan.parse for valid format).");
                }
            }

            PsiStore.Crop((store, path), (output, path), startTime, lengthRelativeInterval, true, new Progress<double>(p => Console.WriteLine($"Progress: {p * 100.0:F2}%")), Console.WriteLine);
            return 0;
        }

        /// <summary>
        /// Encode image streams, generating a new store.
        /// </summary>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="output">Output store name.</param>
        /// <param name="quality">Start time relative to beginning.</param>
        /// <returns>Success flag.</returns>
        internal static int EncodeStore(string store, string path, string output, int quality)
        {
            Console.WriteLine($"Encoding store (store={store}, path={path}, output={output}, quality={quality}");

            bool IsImageStream(IStreamMetadata streamInfo)
            {
                return streamInfo.TypeName.StartsWith("Microsoft.Psi.Shared`1[[Microsoft.Psi.Imaging.Image,");
            }

            void EncodeImageStreams(IStreamMetadata streamInfo, PsiImporter importer, Exporter exporter)
            {
                importer
                    .OpenStream<Shared<Image>>(streamInfo.Name)
                    .ToPixelFormat(PixelFormat.BGRA_32bpp)
                    .EncodeJpeg(quality)
                    .Write(streamInfo.Name, exporter, true);
            }

            PsiStore.Process(IsImageStream, EncodeImageStreams, (store, path), (output, path), true, new Progress<double>(p => Console.WriteLine($"Progress: {p * 100.0:F2}%")), Console.WriteLine);

            return 0;
        }

        /// <summary>
        /// List tasks discovered in assemblies given in app.config.
        /// </summary>
        /// <param name="assemblies">Optional assemblies containing task.</param>
        /// <returns>Success flag.</returns>
        internal static int ListTasks(IEnumerable<string> assemblies)
        {
            Console.WriteLine($"Available Tasks");
            foreach (var task in LoadTasks(assemblies))
            {
                Console.WriteLine($"{task.Attribute.Name} - {task.Attribute.Description}");
            }

            return 0;
        }

        /// <summary>
        /// Execute task against each message in a stream.
        /// </summary>
        /// <param name="stream">Stream name.</param>
        /// <param name="store">Store name.</param>
        /// <param name="path">Store path.</param>
        /// <param name="name">Task name.</param>
        /// <param name="assemblies">Optional assemblies containing task.</param>
        /// <param name="args">Additional configuration arguments.</param>
        /// <returns>Success flag.</returns>
        internal static int ExecuteTask(string stream, string store, string path, string name, IEnumerable<string> assemblies, IEnumerable<string> args)
        {
            Console.WriteLine($"Execute Task (stream={stream}, store={store}, path={path}, name={name}, assemblies={assemblies}, args={args})");

            // find task
            var tasks = LoadTasks(assemblies).Where(t => t.Attribute.Name == name).ToArray();
            if (tasks.Length == 0)
            {
                throw new Exception($"Could not find task named '{name}'.");
            }
            else if (tasks.Length > 1)
            {
                throw new Exception($"Ambiguous task name ({tasks.Count()} tasks found named '{name}').");
            }

            var task = tasks[0];

            // process task
            using (var pipeline = Pipeline.Create())
            {
                var importer = store != null ? PsiStore.Open(pipeline, store, Path.GetFullPath(path)) : null;

                // prepare parameters
                var streamMode = stream?.Length > 0;
                var messageIndex = -1;
                var envelopeIndex = -1;
                var argList = args.ToArray();
                var argIndex = 0;
                var parameterInfo = task.Method.GetParameters();
                var parameters = new object[parameterInfo.Length];
                for (var i = 0; i < parameterInfo.Length; i++)
                {
                    var p = parameterInfo[i];
                    if (p.ParameterType.IsAssignableFrom(typeof(Importer)))
                    {
                        if (importer == null)
                        {
                            throw new ArgumentException("Error: Task requires a store, but no store argument supplied (-s).");
                        }

                        parameters[i] = importer;
                    }
                    else if (p.ParameterType.IsAssignableFrom(typeof(Pipeline)))
                    {
                        parameters[i] = pipeline;
                    }
                    else if (p.ParameterType.IsAssignableFrom(typeof(Envelope)))
                    {
                        envelopeIndex = i;
                    }
                    else if (streamMode && messageIndex == -1)
                    {
                        messageIndex = i; // assumed first arg
                    }
                    else
                    {
                        Action<string, Func<string, object>> processArgs = (friendlyName, parser) =>
                        {
                            if (argIndex < args.Count())
                            {
                                // take from command-line args
                                try
                                {
                                    parameters[i] = parser(argList[argIndex++]);
                                }
                                catch (Exception ex)
                                {
                                    throw new ArgumentException($"Error: Parameter '{p.Name}' ({i}) expected {friendlyName}.", ex);
                                }
                            }
                            else
                            {
                                // get value interactively
                                do
                                {
                                    try
                                    {
                                        Console.Write($"{p.Name} ({friendlyName})? ");
                                        parameters[i] = parser(Console.ReadLine());
                                    }
                                    catch
                                    {
                                        Console.WriteLine($"Error: Expected {friendlyName}.");
                                    }
                                }
                                while (parameters[i] == null);
                            }
                        };

                        if (p.ParameterType.IsAssignableFrom(typeof(double)))
                        {
                            processArgs("double", v => double.Parse(v));
                        }
                        else if (p.ParameterType.IsAssignableFrom(typeof(int)))
                        {
                            processArgs("integer", v => int.Parse(v));
                        }
                        else if (p.ParameterType.IsAssignableFrom(typeof(bool)))
                        {
                            processArgs("boolean", v => bool.Parse(v));
                        }
                        else if (p.ParameterType.IsAssignableFrom(typeof(DateTime)))
                        {
                            processArgs("datetime", v => DateTime.Parse(v));
                        }
                        else if (p.ParameterType.IsAssignableFrom(typeof(TimeSpan)))
                        {
                            processArgs("timespan", v => TimeSpan.Parse(v));
                        }
                        else if (p.ParameterType.IsAssignableFrom(typeof(string)))
                        {
                            processArgs("string", v => v);
                        }
                        else
                        {
                            throw new ArgumentException($"Unexpected parameter type ({p.ParameterType}).");
                        }
                    }
                }

                if (streamMode)
                {
                    if (importer == null)
                    {
                        throw new ArgumentException("Error: Task requires a stream within a store, but no store argument supplied (-s).");
                    }

                    importer.OpenDynamicStream(stream).Do((m, e) =>
                    {
                        if (messageIndex != -1)
                        {
                            parameters[messageIndex] = m;
                        }

                        if (envelopeIndex != -1)
                        {
                            parameters[envelopeIndex] = e;
                        }

                        task.Method.Invoke(null, parameters);
                    });
                }
                else
                {
                    task.Method.Invoke(null, parameters);
                }

                if (importer != null)
                {
                    pipeline.ProgressReportInterval = TimeSpan.FromSeconds(1);
                    pipeline.RunAsync(null, new Progress<double>(p => Console.WriteLine($"Progress: {p * 100.0:F2}%")));
                    pipeline.WaitAll();
                }
            }

            return 0;
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
                var data = PsiStore.Open(pipeline, store, Path.GetFullPath(path));
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

        private static IEnumerable<(MethodInfo Method, BatchProcessingTaskAttribute Attribute)> LoadTasks(IEnumerable<string> extraAssemblies)
        {
            var assemblies = (ConfigurationManager.AppSettings["taskAssemblies"]?.Split(';') ?? new string[0]).Concat(extraAssemblies).Where(s => s.Length > 0);
            if (assemblies == null || assemblies.Count() == 0)
            {
                throw new ConfigurationErrorsException("Task assemblies must be specified at the command line or in app.config.");
            }

            // third-party assembly warning
            Console.WriteLine();
            WriteHorizontalLine();
            WriteWarningLines(AdditionalAssembliesWarning.Title);
            WriteEmptyLine();
            WriteWarningLines(string.Format(AdditionalAssembliesWarning.Line1, ApplicationName));
            WriteEmptyLine();

            foreach (var file in assemblies)
            {
                Console.WriteLine($"| {file.PadRight(consoleLineWidth - 4)} |");
            }

            WriteEmptyLine();
            WriteWarningLines(AdditionalAssembliesWarning.Line2);
            WriteHorizontalLine();
            Console.WriteLine();
            Console.Write(string.Format(AdditionalAssembliesWarning.Question, ApplicationName));
            Console.WriteLine(" (Y/N)");

            if (Console.ReadLine().ToUpper() != "Y")
            {
                throw new Exception("Task aborted.");
            }

            // find task
            foreach (var file in assemblies)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(file);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    var message = new StringBuilder();
                    message.Append($"Could not load assembly {file}: {ex.Message}");
                    if (ex.LoaderExceptions != null)
                    {
                        foreach (Exception loaderException in ex.LoaderExceptions)
                        {
                            message.Append(loaderException.Message);
                        }
                    }

                    throw new Exception(message.ToString());
                }
                catch (Exception ex)
                {
                    throw new Exception($"Could not load assembly {file}: {ex.Message}");
                }

                foreach (var type in assembly.GetTypes())
                {
                    foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
                    {
                        foreach (var attr in method.GetCustomAttributes(typeof(BatchProcessingTaskAttribute)))
                        {
                            yield return (method, (BatchProcessingTaskAttribute)attr);
                        }
                    }
                }
            }
        }

        private static void WriteHorizontalLine()
        {
            Console.WriteLine($"+{new string('-', consoleLineWidth - 2)}+");
        }

        private static void WriteEmptyLine()
        {
            Console.WriteLine($"|{new string(' ', consoleLineWidth - 2)}|");
        }

        private static void WriteWarningLines(string warningString)
        {
            StringBuilder line = new StringBuilder();

            // Split the line into individual words
            string[] words = warningString.Split(' ');

            // Walk the list of words
            int index = 0;
            while (index < words.Length)
            {
                // If the next word, and a prepended space fit on the current line,
                // add it. Otherwise output the current line and begin a new one.
                if (line.Length + words[index].Length + 1 <= consoleLineWidth - 4)
                {
                    if (line.Length > 0)
                    {
                        line.Append(" ");
                    }

                    line.Append(words[index++]);
                }
                else
                {
                    OutputLine(line);
                }
            }

            // Output any partial line
            if (line.Length > 0)
            {
                OutputLine(line);
            }
        }

        private static void OutputLine(StringBuilder line)
        {
            double sidePadding = (consoleLineWidth - 4 - line.Length) / 2.0d;

            Console.Write("| ");
            Console.Write(new string(' ', (int)Math.Floor(sidePadding)));
            Console.Write(line);
            Console.Write(new string(' ', (int)Math.Ceiling(sidePadding)));
            Console.WriteLine(" |");
            line.Clear();
        }
    }
}
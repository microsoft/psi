// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Class containing debug extensions for the executive subsystem.
    /// </summary>
    public static class DebugExtensions
    {
        private static Exporter debugStore;
        private static Pipeline debugPipeline;
        private static string debugStoreName = "debug";
        private static object syncRoot = new object();

        /// <summary>
        /// Call this to enable DebugView calls. Usually wrapped in #ifdef DEBUG conditional statements.
        /// </summary>
        public static void EnableDebugViews()
        {
            if (debugStore == null)
            {
                lock (syncRoot)
                {
                    if (debugStore == null)
                    {
                        debugPipeline = Pipeline.Create(debugStoreName);
                        debugStore = PsiStore.Create(debugPipeline, debugStoreName, null);
                        debugPipeline.RunAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Call this to disable DebugView calls. Usually wrapped in #ifdef DEBUG conditional statements.
        /// </summary>
        public static void DisableDebugViews()
        {
            if (debugStore != null)
            {
                lock (syncRoot)
                {
                    if (debugStore != null)
                    {
                        debugPipeline.Dispose();
                        debugStore = null;
                        debugPipeline = null;
                    }
                }
            }
        }

        /// <summary>
        /// Publishes the specified stream to the debug partition, allowing debugging visualizers to display the data.
        /// </summary>
        /// <typeparam name="T">The type of data in the stream.</typeparam>
        /// <param name="source">The stream to visualize.</param>
        /// <param name="name">The name to use when visualizing the stream.</param>
        /// <param name="deliveryPolicy">An optional delivery policy.</param>
        /// <returns>The debug name of the stream, either as provided or the generated one if one was not specified.</returns>
        public static string DebugView<T>(this IProducer<T> source, string name = null, DeliveryPolicy<T> deliveryPolicy = null)
        {
            var debugName = name ?? source.Out.Name ?? source.Out.Id.ToString();

            if (debugStore != null)
            {
                lock (syncRoot)
                {
                    if (!PsiStore.TryGetStreamMetadata(debugPipeline, debugName, out IStreamMetadata meta))
                    {
                        source.Write(debugName, debugStore, deliveryPolicy: deliveryPolicy);
                    }
                }
            }

            return debugName;
        }

        /// <summary>
        /// Generates a .dgml file that can be opened in Visual Studio to visualize the pipeline structure.
        /// See https://msdn.microsoft.com/en-us/library/ee842619.aspx.
        /// </summary>
        /// <param name="pipeline">The pipeline to dump.</param>
        /// <param name="fileName">The name (and path) of the new file to generate.</param>
        public static void DumpStructure(this Pipeline pipeline, string fileName)
        {
            string catSource = "source";
            string catDefault = "default";

            using (var writer = File.CreateText(fileName))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf - 8\"?>");
                writer.WriteLine("<DirectedGraph GraphDirection=\"LeftToRight\" Layout=\"Sugiyama\" ZoomLevel=\" - 1\" xmlns=\"http://schemas.microsoft.com/vs/2009/dgml\">");
                writer.WriteLine("<Nodes>");

                foreach (var component in pipeline.Components)
                {
                    var componentCategory = component.IsSource ? catSource : catDefault;
                    foreach (var receiver in component.Inputs)
                    {
                        var receiverName = System.Security.SecurityElement.Escape(receiver.Key);
                        writer.WriteLine($"<Node Id=\"{component.Name}{receiverName}\" Label=\"{receiverName}\" />/>");
                    }

                    foreach (var emitter in component.Outputs)
                    {
                        var emitterName = System.Security.SecurityElement.Escape(emitter.Key);
                        writer.WriteLine($"<Node Id=\"{component.Name}{emitterName}\"  Label=\"{emitterName}\" />");
                    }

                    var componentName = System.Security.SecurityElement.Escape(component.Name);
                    writer.WriteLine($"<Node Id=\"{componentName}\" Category=\"{componentCategory}\" Group=\"Collapsed\" Label=\"{componentName}\"/>");
                }

                writer.WriteLine("</Nodes>");
                writer.WriteLine("<Links>");

                foreach (var component in pipeline.Components)
                {
                    var componentName = System.Security.SecurityElement.Escape(component.Name);
                    foreach (var receiver in component.Inputs)
                    {
                        var receiverName = System.Security.SecurityElement.Escape(receiver.Key);
                        writer.WriteLine($"<Link Source=\"{componentName}\" Target=\"{componentName}{receiverName}\" Category=\"Contains\"/>");
                    }

                    foreach (var emitter in component.Outputs)
                    {
                        var emitterName = System.Security.SecurityElement.Escape(emitter.Key);
                        writer.WriteLine($"<Link Source=\"{componentName}\" Target=\"{componentName}{emitterName}\" Category=\"Contains\"/>");
                    }
                }

                foreach (var receiverComp in pipeline.Components)
                {
                    var recComponentName = System.Security.SecurityElement.Escape(receiverComp.Name);
                    foreach (var receiver in receiverComp.Inputs)
                    {
                        if (receiver.Value.Source != null)
                        {
                            var emitterComp = pipeline.Components.First(c => c.Outputs.Values.Contains(receiver.Value.Source));
                            var emitComponentName = System.Security.SecurityElement.Escape(emitterComp.Name);
                            var emitterName = System.Security.SecurityElement.Escape(pipeline.Components.SelectMany(c => c.Outputs).First(e => e.Value == receiver.Value.Source).Key);
                            var receiverName = System.Security.SecurityElement.Escape(receiver.Key);
                            writer.WriteLine($"<Link Source=\"{emitComponentName}{emitterName}\" Target=\"{recComponentName}{receiverName}\"/>");
                        }
                    }
                }

                writer.WriteLine("</Links>");
                writer.WriteLine("<Categories>");
                writer.WriteLine($"<Category Id=\"{catSource}\"/>");
                writer.WriteLine($"<Category Id=\"{catDefault}\"/>");
                writer.WriteLine("<Category Id=\"Contains\" IsContainment=\"True\"/>");
                writer.WriteLine("</Categories>");
                writer.WriteLine("<Styles>");
                writer.WriteLine($"<Style TargetType=\"Node\"  GroupLabel=\"{catSource}\" ValueLabel =\"True\">");
                writer.WriteLine($"<Condition Expression=\"HasCategory('{catSource}')\" />");
                writer.WriteLine("<Setter Property=\"Background\" Value=\"#FF339933\" />");
                writer.WriteLine("</Style>");
                writer.WriteLine("</Styles>");
                writer.WriteLine("</DirectedGraph>");
            }
        }
    }
}

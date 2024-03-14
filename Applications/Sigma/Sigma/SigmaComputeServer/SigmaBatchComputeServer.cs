// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using Microsoft.Psi;
    using Microsoft.Psi.MixedReality.Applications;

    /// <summary>
    /// Implements the batch compute server pipeline.
    /// </summary>
    public static class SigmaBatchComputeServer
    {
        /// <summary>
        /// Replays a store and runs the compute server pipeline.
        /// </summary>
        /// <param name="storePath">The path to the store to replay.</param>
        /// <param name="configuration">The configuration for the compute server pipeline.</param>
        /// <param name="outputStoreName">The output store name.</param>
        public static void ReRun(string storePath, SigmaComputeServerPipelineConfiguration configuration, string outputStoreName)
        {
            Console.WriteLine($"Running: {storePath}");

            // Create the pipeline and input and output stores
            using var pipeline = Pipeline.Create(enableDiagnostics: true, diagnosticsConfiguration: new () { SamplingInterval = TimeSpan.FromSeconds(5) });
            var exporter = PsiStore.Create(pipeline, outputStoreName, storePath, createSubdirectory: false);

            // Write the diagnostics
            pipeline.Diagnostics.Write("Diagnostics", exporter, largeMessages: true);

            // Open the importer
            var importer = PsiStore.Open(pipeline, "Sigma", storePath);

            // Get the hololens streams from the specified store
            var hololensStreams = new HoloLensStreams(importer, nameof(HoloLensStreams));

            // Write the hololens streams to the store under the HoloLens name, but not the images
            // (those will be persisted separately as image views in this app)
            hololensStreams.Write("HoloLensStreams", exporter);

            // Create the batch compute server pipeline
            var sigmaBatchComputeServerPipeline = configuration.CreateBatchComputeServerPipeline(
                pipeline, hololensStreams, importer, exporter);
            sigmaBatchComputeServerPipeline.Initialize();
            sigmaBatchComputeServerPipeline.Write("Sigma", exporter);

            pipeline.PipelineCompleted += (_, _) => Console.WriteLine("\nDONE.");
            pipeline.RunAsync(ReplayDescriptor.ReplayAllRealTime, progress: new Progress<double>(p => Console.Write($"Progress: {p}\r")));
            pipeline.WaitAll();
        }
    }
}
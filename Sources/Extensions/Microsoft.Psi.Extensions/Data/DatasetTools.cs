// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Defines tools that are helpful in working with datasets.
    /// </summary>
    public static class DatasetTools
    {
        /// <summary>
        /// Compute derived results for each session in the dataset.
        /// </summary>
        /// <typeparam name="TResult">The type of data of the derived result.</typeparam>
        /// <param name="dataset">The dataset over which to derive results.</param>
        /// <param name="computeDerived">The action to be invoked to derive results.</param>
        /// <returns>List of results.</returns>
        public static IReadOnlyList<TResult> ComputeDerived<TResult>(
            this Dataset dataset,
            Action<Pipeline, SessionImporter, TResult> computeDerived)
            where TResult : class, new()
        {
            var results = new List<TResult>();
            foreach (var session in dataset.Sessions)
            {
                // the first partition is where we put the data if output is not specified
                var inputPartition = session.Partitions.FirstOrDefault();

                // create and run the pipeline
                using (var pipeline = Pipeline.Create())
                {
                    var importer = SessionImporter.Open(pipeline, session);

                    var result = new TResult();
                    computeDerived(pipeline, importer, result);

                    var startTime = DateTime.Now;
                    Console.WriteLine($"Computing derived features on {inputPartition.StorePath} ...");
                    pipeline.Run(ReplayDescriptor.ReplayAll);

                    var finishTime = DateTime.Now;
                    Console.WriteLine($" - Time elapsed: {(finishTime - startTime).TotalMinutes:0.00} min.");

                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Compute derived partion for each session in the dataset.
        /// </summary>
        /// <param name="dataset">The dataset over which to derive partitions.</param>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="outputPartitionName">The output partition name to be created.</param>
        /// <param name="overwrite">Flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">The name of the output data store. Default is null.</param>
        /// <param name="outputStorePath">The path of the output data store. Default is null.</param>
        /// <param name="replayDescriptor">The replay descriptor to us</param>
        /// <returns>A dataset with the newly derived partitions.</returns>
        public static Dataset CreateDerivedPartition(
            this Dataset dataset,
            Action<Pipeline, SessionImporter, Exporter> computeDerived,
            string outputPartitionName,
            bool overwrite = false,
            string outputStoreName = null,
            string outputStorePath = null,
            ReplayDescriptor replayDescriptor = null)
        {
            return CreateDerivedPartition<long>(
                dataset,
                (p, si, e, l) => computeDerived(p, si, e),
                0,
                outputPartitionName,
                overwrite,
                outputStoreName,
                outputStorePath,
                replayDescriptor);
        }

        /// <summary>
        /// Compute derived partion for each session in the dataset.
        /// </summary>
        /// <typeparam name="TParameter">The type of paramater passed to the action.</typeparam>
        /// <param name="dataset">The dataset over which to derive partitions.</param>
        /// <param name="computeDerived">The action to be invoked to derive partitions.</param>
        /// <param name="parameter">The parameter to be passed to the action.</param>
        /// <param name="outputPartitionName">The output partition name to be created.</param>
        /// <param name="overwrite">Flag indicating whether the partition should be overwritten. Default is false.</param>
        /// <param name="outputStoreName">The name of the output data store. Default is null.</param>
        /// <param name="outputStorePath">The path of the output data store. Default is null.</param>
        /// <param name="replayDescriptor">The replay descriptor to us</param>
        /// <returns>A dataset with the newly derived partitions.</returns>
        public static Dataset CreateDerivedPartition<TParameter>(
            this Dataset dataset,
            Action<Pipeline, SessionImporter, Exporter, TParameter> computeDerived,
            TParameter parameter,
            string outputPartitionName,
            bool overwrite = false,
            string outputStoreName = null,
            string outputStorePath = null,
            ReplayDescriptor replayDescriptor = null)
        {
            int sessionIndex = 0;
            foreach (var session in dataset.Sessions)
            {
                if (session.Partitions.Any(p => p.Name == outputPartitionName))
                {
                    if (overwrite)
                    {
                        // remove the partition first
                        session.RemovePartition(session.Partitions.First(p => p.Name == outputPartitionName));
                    }
                    else
                    {
                        // if the overwrite flag is not on, throw
                        throw new Exception($"Session already contains partition with name {outputPartitionName}");
                    }
                }

                // the first partition is where we put the data if output is not specified
                var inputPartition = session.Partitions.FirstOrDefault();

                // figure out the output partition path
                var outputPartitionPath = (outputStorePath == null) ? inputPartition.StorePath : Path.Combine(outputStorePath, $"{sessionIndex}");

                // create and run the pipeline
                using (var pipeline = Pipeline.Create())
                {
                    var importer = SessionImporter.Open(pipeline, session);
                    var exporter = Store.Create(pipeline, outputStoreName ?? outputPartitionName, outputPartitionPath);

                    computeDerived(pipeline, importer, exporter, parameter);

                    var startTime = DateTime.Now;
                    Console.WriteLine($"Computing derived features on {inputPartition.StorePath} ...");

                    // Add a default replay strategy
                    if (replayDescriptor == null)
                    {
                        replayDescriptor = ReplayDescriptor.ReplayAll;
                    }

                    pipeline.Run(replayDescriptor);

                    var finishTime = DateTime.Now;
                    Console.WriteLine($" - Time elapsed: {(finishTime - startTime).TotalMinutes:0.00} min.");
                }

                // add the partition
                session.AddStorePartition(outputPartitionName, outputPartitionPath, outputPartitionName);

                // increment session index
                sessionIndex++;
            }

            return dataset;
        }

        /// <summary>
        /// Adds sessions from data stores located in the specified path.
        /// </summary>
        /// <param name="dataset">The dataset to add sessions to.</param>
        /// <param name="path">The path that contains the data stores.</param>
        /// <param name="partitionName">The name of the partion to be added when adding a new session. Default is null.</param>
        public static void AddSessionsFromExistingStores(this Dataset dataset, string path, string partitionName = null)
        {
            dataset.AddSessionsFromExistingStores(path, path, partitionName);
        }

        private static void AddSessionsFromExistingStores(this Dataset dataset, string rootPath, string currentPath, string partitionName)
        {
            // scan for any psi catalog files
            foreach (var filename in Directory.EnumerateFiles(currentPath, "*.Catalog_000000.psi"))
            {
                var fi = new FileInfo(filename);
                var storeName = fi.Name.Substring(0, fi.Name.Length - ".Catalog_000000.psi".Length);
                var sessionName = (currentPath == rootPath) ? filename : Path.Combine(currentPath, filename).Substring(rootPath.Length);
                sessionName = sessionName.Substring(0, sessionName.Length - fi.Name.Length);
                sessionName = sessionName.Trim('\\');
                dataset.AddSessionFromExistingStore(sessionName, storeName, currentPath, partitionName);
            }

            // now go through subfolders
            foreach (var directory in Directory.EnumerateDirectories(currentPath))
            {
                dataset.AddSessionsFromExistingStores(rootPath, Path.Combine(currentPath, directory), partitionName);
            }
        }
    }
}
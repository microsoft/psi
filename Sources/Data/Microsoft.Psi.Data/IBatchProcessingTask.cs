// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    /// <summary>
    /// Defines a batch processing task.
    /// </summary>
    internal interface IBatchProcessingTask
    {
        /// <summary>
        /// Gets the default configuration.
        /// </summary>
        /// <returns>The default configuration.</returns>
        public BatchProcessingTaskConfiguration GetDefaultConfiguration();

        /// <summary>
        /// Runs the batch processing task with a specified configuration.
        /// </summary>
        /// <param name="pipeline">The pipeline to run the task.</param>
        /// <param name="sessionImporter">The session importer.</param>
        /// <param name="exporter">The exporter to use.</param>
        /// <param name="configuration">The configuration for the task.</param>
        public void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, BatchProcessingTaskConfiguration configuration);
    }
}

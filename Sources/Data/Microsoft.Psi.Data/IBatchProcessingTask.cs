// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    /// <summary>
    /// Defines a batch processing task.
    /// </summary>
    public interface IBatchProcessingTask
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

        /// <summary>
        /// Performs any initialization operations when starting to process a dataset.
        /// </summary>
        public void OnStartProcessingDataset();

        /// <summary>
        /// Performs any closing operations after completing processing a dataset.
        /// </summary>
        public void OnEndProcessingDataset();

        /// <summary>
        /// Performs any closing operations after the dataset batch processing operation has been cancelled.
        /// </summary>
        public void OnCanceledProcessingDataset();

        /// <summary>
        /// Performs any closing operations after the dataset batch processing operation has encountered an exception.
        /// </summary>
        public void OnExceptionProcessingDataset();

        /// <summary>
        /// Performs any initialization operations when starting to process a session.
        /// </summary>
        public void OnStartProcessingSession();

        /// <summary>
        /// Performs any closing operations after completing processing a session.
        /// </summary>
        public void OnEndProcessingSession();

        /// <summary>
        /// Performs any closing operations after the session batch processing operation has been cancelled.
        /// </summary>
        public void OnCanceledProcessingSession();

        /// <summary>
        /// Performs any closing operations after the session batch processing operation has encountered an exception.
        /// </summary>
        public void OnExceptionProcessingSession();
    }
}

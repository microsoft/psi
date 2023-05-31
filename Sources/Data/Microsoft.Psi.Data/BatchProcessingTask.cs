// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    /// <summary>
    /// Defines an abstract base class for batch processing tasks.
    /// </summary>
    /// <typeparam name="TBatchProcessingTaskConfiguration">The type of the batch processing task configuration object.</typeparam>
    /// <remarks>
    /// To implement a batch processing task, implement a derived class from this base abstract.
    /// </remarks>
    public abstract class BatchProcessingTask<TBatchProcessingTaskConfiguration> : IBatchProcessingTask
        where TBatchProcessingTaskConfiguration : BatchProcessingTaskConfiguration, new()
    {
        /// <summary>
        /// Gets the default configuration for the batch procesing task.
        /// </summary>
        /// <returns>The default configuration.</returns>
        public virtual TBatchProcessingTaskConfiguration GetDefaultConfiguration() => new ();

        /// <summary>
        /// Runs a batch processing task.
        /// </summary>
        /// <param name="pipeline">The pipeline used to run the task.</param>
        /// <param name="sessionImporter">The session importer.</param>
        /// <param name="exporter">The exporter to write resulting streams to.</param>
        /// <param name="configuration">The configuration for the batch processing task.</param>
        public abstract void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, TBatchProcessingTaskConfiguration configuration);

        /// <inheritdoc/>
        BatchProcessingTaskConfiguration IBatchProcessingTask.GetDefaultConfiguration() => this.GetDefaultConfiguration();

        /// <inheritdoc/>
        void IBatchProcessingTask.Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, BatchProcessingTaskConfiguration configuration) =>
            this.Run(pipeline, sessionImporter, exporter, configuration as TBatchProcessingTaskConfiguration);
    }
}

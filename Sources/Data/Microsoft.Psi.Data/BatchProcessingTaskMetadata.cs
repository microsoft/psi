// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Represents metadata about a dynamically loaded batch processing task
    /// and provides functionality for configuring and executing the task.
    /// </summary>
    public class BatchProcessingTaskMetadata
    {
        private readonly BatchProcessingTaskAttribute batchProcessingTaskAttribute = null;
        private readonly Type batchProcessingTaskType;
        private readonly MethodInfo batchProcessingTaskMethodInfo;
        private readonly string batchProcessingTaskConfigurationsPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchProcessingTaskMetadata"/> class.
        /// </summary>
        /// <param name="batchProcessingTaskType">The batch processing task type.</param>
        /// <param name="batchProcessingTaskAttribute">The batch processing task attribute.</param>
        /// <param name="batchProcessingTaskConfigurationsPath">The folder in which batch processing task configurations are saved.</param>
        public BatchProcessingTaskMetadata(Type batchProcessingTaskType, BatchProcessingTaskAttribute batchProcessingTaskAttribute, string batchProcessingTaskConfigurationsPath)
        {
            this.batchProcessingTaskType = batchProcessingTaskType;
            this.batchProcessingTaskAttribute = batchProcessingTaskAttribute;
            this.batchProcessingTaskConfigurationsPath = Path.Combine(batchProcessingTaskConfigurationsPath, batchProcessingTaskAttribute.Name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchProcessingTaskMetadata"/> class.
        /// </summary>
        /// <param name="batchProcessingTaskMethodInfo">The batch processing method info.</param>
        /// <param name="batchProcessingTaskAttribute">The batch processing task attribute.</param>
        /// <param name="batchProcessingTaskConfigurationsPath">The folder in which batch processing task configurations are saved.</param>
        public BatchProcessingTaskMetadata(MethodInfo batchProcessingTaskMethodInfo, BatchProcessingTaskAttribute batchProcessingTaskAttribute, string batchProcessingTaskConfigurationsPath)
        {
            this.batchProcessingTaskMethodInfo = batchProcessingTaskMethodInfo;
            this.batchProcessingTaskAttribute = batchProcessingTaskAttribute;
            this.batchProcessingTaskConfigurationsPath = Path.Combine(batchProcessingTaskConfigurationsPath, batchProcessingTaskAttribute.Name);
        }

        /// <summary>
        /// Gets the batch processing task name.
        /// </summary>
        public string Name => this.batchProcessingTaskAttribute.Name;

        /// <summary>
        /// Gets the batch processing task description.
        /// </summary>
        public string Description => this.batchProcessingTaskAttribute.Description;

        /// <summary>
        /// Gets the batch processing task icon source path.
        /// </summary>
        public string IconSourcePath => this.batchProcessingTaskAttribute.IconSourcePath;

        /// <summary>
        /// Gets the folder under which configurations for this batch processing task should be stored.
        /// </summary>
        public string ConfigurationsPath => this.batchProcessingTaskConfigurationsPath;

        /// <summary>
        /// Gets or sets the name of the most recently used configuration.
        /// </summary>
        public string MostRecentlyUsedConfiguration { get; set; }

        /// <summary>
        /// Gets a value indicating whether this batch processing task is method based.
        /// </summary>
        private bool IsMethodBased => this.batchProcessingTaskMethodInfo != null;

        /// <summary>
        /// Gets the default configuration for the batch processing task.
        /// </summary>
        /// <returns>The default configuration.</returns>
        public BatchProcessingTaskConfiguration GetDefaultConfiguration()
        {
            if (this.IsMethodBased)
            {
                return new BatchProcessingTaskConfiguration()
                {
                    ReplayAllRealTime = this.batchProcessingTaskAttribute.ReplayAllRealTime,
                    DeliveryPolicyLatestMessage = this.batchProcessingTaskAttribute.DeliveryPolicyLatestMessage,
                    OutputStoreName = this.batchProcessingTaskAttribute.OutputStoreName,
                    OutputStorePath = this.batchProcessingTaskAttribute.OutputStorePath,
                    OutputPartitionName = this.batchProcessingTaskAttribute.OutputPartitionName ?? "Derived",
                };
            }
            else
            {
                var batchProcessingTask = Activator.CreateInstance(this.batchProcessingTaskType) as IBatchProcessingTask;
                return batchProcessingTask.GetDefaultConfiguration();
            }
        }

        /// <summary>
        /// Runs the batch processing task.
        /// </summary>
        /// <param name="pipeline">The pipeline to run the task on.</param>
        /// <param name="sessionImporter">The session importer.</param>
        /// <param name="exporter">The exporter.</param>
        /// <param name="configuration">The task configuration.</param>
        public void Run(Pipeline pipeline, SessionImporter sessionImporter, Exporter exporter, BatchProcessingTaskConfiguration configuration)
        {
            if (this.IsMethodBased)
            {
                this.batchProcessingTaskMethodInfo.Invoke(null, new object[] { pipeline, sessionImporter, exporter });
            }
            else
            {
                var batchProcessingTask = Activator.CreateInstance(this.batchProcessingTaskType) as IBatchProcessingTask;
                batchProcessingTask.Run(pipeline, sessionImporter, exporter, configuration);
            }
        }
    }
}

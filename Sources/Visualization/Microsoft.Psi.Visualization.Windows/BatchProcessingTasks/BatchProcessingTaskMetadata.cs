// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Tasks
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Represents information about a batch processing task.
    /// </summary>
    public class BatchProcessingTaskMetadata
    {
        private readonly BatchProcessingTaskAttribute taskAttribute = null;

        private BatchProcessingTaskMetadata(Type batchProcessingTaskType, MethodInfo methodInfo, BatchProcessingTaskAttribute taskAttribute)
        {
            this.Type = batchProcessingTaskType;
            this.MethodInfo = methodInfo;
            this.taskAttribute = taskAttribute;
        }

        /// <summary>
        /// Gets the type containing the batch processing task.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets the method info for the batch processing task.
        /// </summary>
        public MethodInfo MethodInfo { get; private set; }

        /// <summary>
        /// Gets the batch processing task name.
        /// </summary>
        public string Name => this.taskAttribute.Name;

        /// <summary>
        /// Gets the batch processing task description.
        /// </summary>
        public string Description => this.taskAttribute.Description;

        /// <summary>
        /// Gets the batch processing task icon source path.
        /// </summary>
        public string IconSourcePath => this.taskAttribute.IconSourcePath;

        /// <summary>
        /// Gets a value indicating whether to use the <see cref="ReplayDescriptor.ReplayAllRealTime"/> descriptor when executing this batch task.
        /// </summary>
        public bool ReplayAllRealTime => this.taskAttribute.ReplayAllRealTime;

        /// <summary>
        /// Gets a value indicating whether to use the <see cref="DeliveryPolicy.LatestMessage"/> pipeline-level delivery policy when executing this batch task.
        /// </summary>
        public bool DeliveryPolicyLatestMessage => this.taskAttribute.DeliveryPolicyLatestMessage;

        /// <summary>
        /// Gets a value indicating whether we should enable pipeline diagnostics when running this batch task.
        /// </summary>
        public bool EnableDiagnostics => this.taskAttribute.EnableDiagnostics;

        /// <summary>
        /// Gets the output store name for this batch task.
        /// </summary>
        public string OutputStoreName => this.taskAttribute.OutputStoreName;

        /// <summary>
        /// Gets the output store path for this batch task.
        /// </summary>
        public string OutputStorePath => this.taskAttribute.OutputStorePath;

        /// <summary>
        /// Gets the output partition name for this batch task.
        /// </summary>
        public string OutputPartitionName => this.taskAttribute.OutputPartitionName;

        /// <summary>
        /// Creates a new batch processing task metadata.
        /// </summary>
        /// <param name="batchProcessingTaskType">The type of the task.</param>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="taskAttribute">The task attribute.</param>
        /// <returns>A task metadata.</returns>
        public static BatchProcessingTaskMetadata Create(Type batchProcessingTaskType, MethodInfo methodInfo, BatchProcessingTaskAttribute taskAttribute)
        {
            // Create the task metadata
            return new BatchProcessingTaskMetadata(batchProcessingTaskType, methodInfo, taskAttribute);
        }
    }
}

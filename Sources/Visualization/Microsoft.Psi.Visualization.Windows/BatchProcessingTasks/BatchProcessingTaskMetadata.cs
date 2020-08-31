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
        private BatchProcessingTaskMetadata(Type batchProcessingTaskType, MethodInfo methodInfo, string name, string description, string iconSourcePath)
        {
            this.Type = batchProcessingTaskType;
            this.MethodInfo = methodInfo;
            this.Name = name;
            this.Description = description;
            this.IconSourcePath = iconSourcePath;
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
        public string Name { get; private set; }

        /// <summary>
        /// Gets the batch processing task description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the batch processing task icon source path.
        /// </summary>
        public string IconSourcePath { get; private set; }

        /// <summary>
        /// Creates a new batch processing task metadata.
        /// </summary>
        /// <param name="batchProcessingTaskType">The type of the task.</param>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="taskAttribute">The task attribute.</param>
        /// <param name="logWriter">The log writer.</param>
        /// <returns>A task metadata.</returns>
        public static BatchProcessingTaskMetadata Create(Type batchProcessingTaskType, MethodInfo methodInfo, BatchProcessingTaskAttribute taskAttribute, VisualizationLogWriter logWriter)
        {
            // Create the task metadata
            return new BatchProcessingTaskMetadata(batchProcessingTaskType, methodInfo, taskAttribute.Name, taskAttribute.Description, taskAttribute.IconSourcePath);
        }

        private static BatchProcessingTaskAttribute GetBatchProcessingTaskAttribute(Type taskType, VisualizationLogWriter logWriter)
        {
            var taskAttribute = taskType.GetCustomAttribute<BatchProcessingTaskAttribute>();

            if (taskAttribute == null)
            {
                logWriter.WriteError($"Task {0} could not be loaded because it is not decorated with a {nameof(BatchProcessingTaskAttribute)}", taskType.Name);
                return null;
            }

            if (string.IsNullOrWhiteSpace(taskAttribute.Name))
            {
                logWriter.WriteError($"Task {0} could not be loaded because its {nameof(BatchProcessingTaskAttribute)} does not specify a Name property", taskType.Name);
                return null;
            }

            return taskAttribute;
        }
    }
}

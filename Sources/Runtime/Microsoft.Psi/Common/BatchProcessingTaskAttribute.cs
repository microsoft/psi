// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Represents a batch processing task attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class BatchProcessingTaskAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BatchProcessingTaskAttribute"/> class.
        /// </summary>
        /// <param name="name">Name of this task.</param>
        /// <param name="description">Description of this task.</param>
        /// <param name="iconSourcePath">An optional path to the icon to be used for this task.</param>
        public BatchProcessingTaskAttribute(string name, string description, string iconSourcePath = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentNullException(nameof(description));
            }

            this.Name = name;
            this.Description = description;
            this.IconSourcePath = iconSourcePath;
        }

        /// <summary>
        /// Gets the name of this task.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of this task.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the path to the icon associated with the batch task.
        /// </summary>
        public string IconSourcePath { get; private set; }
    }
}

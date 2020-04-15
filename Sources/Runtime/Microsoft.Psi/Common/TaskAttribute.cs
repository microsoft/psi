// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Represents a task attribute.
    /// </summary>
    /// <remarks>Should be applied to static methods taking a message (of any type) and an Envelope.</remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TaskAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAttribute"/> class.
        /// </summary>
        /// <param name="name">Name of this task.</param>
        /// <param name="description">Description of this task.</param>
        public TaskAttribute(string name, string description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentNullException("description");
            }

            this.Name = name;
            this.Description = description;
        }

        /// <summary>
        /// Gets the name of this task.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of this task.
        /// </summary>
        public string Description { get; }
    }
}

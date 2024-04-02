// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents a procedural task.
    /// </summary>
    public class Task : IInteropSerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Task"/> class.
        /// </summary>
        public Task()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Task"/> class.
        /// </summary>
        /// <param name="name">The name of the task.</param>
        /// <param name="steps">The set of steps.</param>
        public Task(string name, List<Step> steps = null)
        {
            this.Name = name;
            this.Steps = steps ?? new ();
        }

        /// <summary>
        /// Gets or sets the name of the procedural task.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the list of steps.
        /// </summary>
        public List<Step> Steps { get; set; }

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteString(this.Name, writer);
            InteropSerialization.WriteCollection(this.Steps, writer);
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
            this.Name = InteropSerialization.ReadString(reader);
            this.Steps = InteropSerialization.ReadCollection<Step>(reader)?.ToList();
        }

        /// <summary>
        /// Gets the list of steps of a specified type.
        /// </summary>
        /// <typeparam name="T">The step type.</typeparam>
        /// <returns>The enumeration of steps of a specified type.</returns>
        public IEnumerable<T> GetStepsOfType<T>()
            where T : Step
            => this.Steps.Where(s => s is T).Select(s => s as T);
    }
}

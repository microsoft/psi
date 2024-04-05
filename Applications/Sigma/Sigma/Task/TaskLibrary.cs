// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Data.Helpers;
    using Microsoft.Psi.Interop.Serialization;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a task library.
    /// </summary>
    /// <typeparam name="TTask">The type of tasks in the library.</typeparam>
    public class TaskLibrary<TTask> : IInteropSerializable
        where TTask : Task, IInteropSerializable, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLibrary{TTask}"/> class.
        /// </summary>
        public TaskLibrary()
        {
        }

        /// <summary>
        /// Gets or sets the tasks.
        /// </summary>
        public TTask[] Tasks { get; set; }

        /// <summary>
        /// Loads a task library from a json file.
        /// </summary>
        /// <param name="jsonFilename">The json file to load the task library from.</param>
        /// <returns>The loaded task library.</returns>
        public static TaskLibrary<TTask> FromJsonFile(string jsonFilename)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    SerializationBinder = new SafeSerializationBinder(),
                    ContractResolver = new TaskLibraryContractResolver(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                });

            StreamReader jsonFile = null;
            try
            {
                jsonFile = File.OpenText(jsonFilename);
                using var jsonReader = new JsonTextReader(jsonFile);
                jsonFile = null;

                // Deserialize the visualization container
                return serializer.Deserialize<TaskLibrary<TTask>>(jsonReader);
            }
            catch
            {
                return null;
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        /// <summary>
        /// Gets a task by name.
        /// </summary>
        /// <param name="taskName">The task name.</param>
        /// <returns>A task with the specified name, or if no such task exists in the library.</returns>
        public TTask GetTaskOrDefault(string taskName)
            => this.Tasks.FirstOrDefault(t => t.Name.ToLower() == taskName.ToLower());

        /// <summary>
        /// Saves the task library to a json file.
        /// </summary>
        /// <param name="jsonFilename">The file to save the task to.</param>
        public void SaveAsJson(string jsonFilename)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    SerializationBinder = new SafeSerializationBinder(),
                    ContractResolver = new TaskLibraryContractResolver(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                });

            StreamWriter jsonFile = null;
            try
            {
                jsonFile = File.CreateText(jsonFilename);
                using var jsonWriter = new JsonTextWriter(jsonFile);
                jsonFile = null;
                serializer.Serialize(jsonWriter, this);
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Write(BinaryWriter writer)
        {
            InteropSerialization.WriteCollection<TTask>(this.Tasks, writer);
        }

        /// <inheritdoc/>
        public void ReadFrom(BinaryReader reader)
        {
            this.Tasks = InteropSerialization.ReadCollection<TTask>(reader)?.ToArray();
        }
    }
}

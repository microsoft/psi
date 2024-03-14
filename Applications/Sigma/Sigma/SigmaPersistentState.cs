// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using HoloLensCaptureInterop;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// Represents the persistent state for the Sigma application, storing information available
    /// across sessions.
    /// </summary>
    /// <typeparam name="TTask">The type of the task.</typeparam>
    public class SigmaPersistentState<TTask> : IInteropSerializable
        where TTask : Task, IInteropSerializable, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaPersistentState{TTask}"/> class.
        /// </summary>
        public SigmaPersistentState()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaPersistentState{TTask}"/> class.
        /// </summary>
        /// <param name="filename">The name of the file from which to read the state.</param>
        public SigmaPersistentState(string filename)
        {
            this.TaskLibrary = TaskLibrary<TTask>.FromJsonFile(filename + ".tasklibrary.json");

            if (File.Exists(filename + ".knownlocations.dat"))
            {
                using var fileStream = File.OpenRead(filename + ".knownlocations.dat");
                using var reader = new BinaryReader(fileStream);
                this.KnownSpatialLocations = InteropSerialization.ReadDictionary(reader, InteropSerialization.ReadString, Serialization.ReadCoordinateSystem);
                fileStream.Close();
            }
            else
            {
                this.KnownSpatialLocations = new Dictionary<string, CoordinateSystem>();
            }
        }

        /// <summary>
        /// Gets or sets the task library.
        /// </summary>
        public TaskLibrary<TTask> TaskLibrary { get; set; } = new TaskLibrary<TTask>();

        /// <summary>
        /// Gets or sets the known locations.
        /// </summary>
        public Dictionary<string, CoordinateSystem> KnownSpatialLocations { get; set; } = new ();

        /// <summary>
        /// Writes the persistent state to a file.
        /// </summary>
        /// <param name="persistentStateFilename">The file to write the persistent state to.</param>
        /// <param name="originatingTime">The originating time for the persistent state.</param>
        public virtual void WriteToFile(string persistentStateFilename, DateTime originatingTime)
        {
            // Save the task library in json format
            var taskLibraryFileName = persistentStateFilename + ".tasklibrary.json";
            this.TaskLibrary.SaveAsJson(taskLibraryFileName);

            using var fileStream = File.Open(persistentStateFilename + ".knownlocations.dat", FileMode.Create);
            using var writer = new BinaryWriter(fileStream);
            InteropSerialization.WriteDictionary(this.KnownSpatialLocations, writer, InteropSerialization.WriteString, Serialization.WriteCoordinateSystem);
            fileStream.Close();
        }

        /// <inheritdoc/>
        public virtual void Write(BinaryWriter writer)
        {
            InteropSerialization.Write(this.TaskLibrary, writer);
            InteropSerialization.WriteDictionary(this.KnownSpatialLocations, writer, InteropSerialization.WriteString, Serialization.WriteCoordinateSystem);
        }

        /// <inheritdoc/>
        public virtual void ReadFrom(BinaryReader reader)
        {
            this.TaskLibrary = InteropSerialization.Read<TaskLibrary<TTask>>(reader);
            this.KnownSpatialLocations = InteropSerialization.ReadDictionary(reader, InteropSerialization.ReadString, Serialization.ReadCoordinateSystem);
        }
    }
}

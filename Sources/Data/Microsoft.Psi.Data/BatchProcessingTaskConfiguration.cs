// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data.Helpers;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a configuration for a batch processing task.
    /// </summary>
    public class BatchProcessingTaskConfiguration : ObservableObject
    {
        private bool replayAllRealTime = false;
        private bool deliveryPolicyLatestMessage = false;
        private bool enableDiagnostics = false;
        private string outputStoreName = null;
        private string outputStorePath = null;
        private string outputPartitionName = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchProcessingTaskConfiguration"/> class.
        /// </summary>
        public BatchProcessingTaskConfiguration()
        {
        }

        /// <summary>
        /// Gets the name of the configuration.
        /// </summary>
        [Browsable(false)]
        public string Name => "Configuration";

        /// <summary>
        /// Gets or sets a value indicating whether to use the <see cref="ReplayDescriptor.ReplayAllRealTime"/> descriptor when executing this batch task.
        /// </summary>
        [DataMember]
        [DisplayName("Replay in Real Time")]
        [Description("Indicates whether the task will execute by performing replay in real time.")]
        public bool ReplayAllRealTime
        {
            get => this.replayAllRealTime;
            set { this.Set(nameof(this.ReplayAllRealTime), ref this.replayAllRealTime, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use the <see cref="DeliveryPolicy.LatestMessage"/> pipeline-level delivery policy when executing this batch task.
        /// </summary>
        [DataMember]
        [DisplayName("Use Latest Message Delivery Policy")]
        [Description("Indicates whether the task will execute with a latest message global delivery policy.")]
        public bool DeliveryPolicyLatestMessage
        {
            get => this.deliveryPolicyLatestMessage;
            set { this.Set(nameof(this.DeliveryPolicyLatestMessage), ref this.deliveryPolicyLatestMessage, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to enable pipeline diagnostics when running this batch task.
        /// </summary>
        [DataMember]
        [DisplayName("Enable diagnostics")]
        [Description("Indicates whether diagnostics will be enabled on the pipeline while executing the task.")]
        public bool EnableDiagnostics
        {
            get => this.enableDiagnostics;
            set { this.Set(nameof(this.EnableDiagnostics), ref this.enableDiagnostics, value); }
        }

        /// <summary>
        /// Gets or sets the output store name.
        /// </summary>
        [DataMember]
        [DisplayName("Output Store Name")]
        [Description("The output store name.")]
        public string OutputStoreName
        {
            get => this.outputStoreName;
            set { this.Set(nameof(this.OutputStoreName), ref this.outputStoreName, value); }
        }

        /// <summary>
        /// Gets or sets the output store path.
        /// </summary>
        [DataMember]
        [DisplayName("Output Store Path")]
        [Description("The output store path.")]
        public string OutputStorePath
        {
            get => this.outputStorePath;
            set { this.Set(nameof(this.OutputStorePath), ref this.outputStorePath, value); }
        }

        /// <summary>
        /// Gets or sets the output partition name.
        /// </summary>
        [DataMember]
        [DisplayName("Output Partition Name")]
        [Description("The output partition name.")]
        public string OutputPartitionName
        {
            get => this.outputPartitionName;
            set { this.Set(nameof(this.OutputPartitionName), ref this.outputPartitionName, value); }
        }

        /// <summary>
        /// Loads a configuration from the specified file.
        /// </summary>
        /// <param name="fileName">The full path name of the configuration file.</param>
        /// <returns>The loaded configuration.</returns>
        public static BatchProcessingTaskConfiguration Load(string fileName)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    SerializationBinder = new SafeSerializationBinder(),
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                });

            using var jsonFile = File.OpenText(fileName);
            using var jsonReader = new JsonTextReader(jsonFile);
            return serializer.Deserialize<BatchProcessingTaskConfiguration>(jsonReader);
        }

        /// <summary>
        /// Saves the configuration to a file.
        /// </summary>
        /// <param name="fileName">The full path name of the configuration file.</param>
        public void Save(string fileName)
        {
            var serializer = JsonSerializer.Create(
                new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    SerializationBinder = new SafeSerializationBinder(),
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                });

            using var jsonFile = File.CreateText(fileName);
            using var jsonWriter = new JsonTextWriter(jsonFile);
            serializer.Serialize(jsonWriter, this, typeof(BatchProcessingTaskConfiguration));
        }

        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <param name="error">A message describing the issue if the configuration is invalid.</param>
        /// <returns>True if the configuration is valid, false otherwise.</returns>
        public virtual bool Validate(out string error)
        {
            error = null;
            return true;
        }
    }
}

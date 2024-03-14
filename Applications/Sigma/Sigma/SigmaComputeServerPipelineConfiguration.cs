// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.MixedReality.Applications;

    /// <summary>
    /// Represents the configuration for the <see cref="SigmaComputeServerPipeline{TTask, TConfiguration, TInteractionModel, TInteractionStateManager, TPersistentState, TInteractionState, TUserInterfaceState, TUserInterfaceCommands}"/>.
    /// </summary>
    public abstract class SigmaComputeServerPipelineConfiguration : ComputeServerPipelineConfiguration
    {
        private string resourcesPath = null;
        private bool useLiveSpeechRecoInBatchMode = false;
        private ObjectTrackingPipelineConfiguration objectTrackingPipelineConfiguration = null;
        private double maxInteractionStateOutputFrequency = 10;
        private string llmQueryLibraryFilename = null;
        private string persistentStateFilename = null;
        private TimeSpan delayedStart = TimeSpan.Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="SigmaComputeServerPipelineConfiguration"/> class.
        /// </summary>
        /// <param name="configurationType">The configuration type.</param>
        public SigmaComputeServerPipelineConfiguration(ConfigurationType configurationType)
            : base(configurationType)
        {
            this.Name = "Sigma";
            if (configurationType == ConfigurationType.Template)
            {
                this.ResourcesPath = "please fill in the path to the resources";
                this.UseLiveSpeechRecoInBatchMode = false;
                this.ObjectTrackingPipelineConfiguration = new ();
                this.LLMQueryLibraryFilename = "please fill in the LLM query library filename (optional)";
                this.PersistentStateFilename = "please fill in the persistent state filename";
            }
        }

        /// <summary>
        /// Gets or sets the resources path.
        /// </summary>
        [DataMember]
        [DisplayName("Resources Path")]
        [Description("The path to the resources.")]
        public string ResourcesPath
        {
            get => this.resourcesPath;
            set { this.Set(nameof(this.ResourcesPath), ref this.resourcesPath, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use live speech reco when reprocessing data in batch mode.
        /// </summary>
        [DataMember]
        [DisplayName("Use Live Speech Reco in Batch Mode")]
        [Description("Indicates whether to use live speech reco when reprocessing data in batch mode.")]
        public bool UseLiveSpeechRecoInBatchMode
        {
            get => this.useLiveSpeechRecoInBatchMode;
            set { this.Set(nameof(this.UseLiveSpeechRecoInBatchMode), ref this.useLiveSpeechRecoInBatchMode, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use object tracking.
        /// </summary>
        [DataMember]
        [DisplayName("Object Tracking Pipeline")]
        [Description("The configuration for the object tracking pipeline.")]
        public ObjectTrackingPipelineConfiguration ObjectTrackingPipelineConfiguration
        {
            get => this.objectTrackingPipelineConfiguration;
            set { this.Set(nameof(this.ObjectTrackingPipelineConfiguration), ref this.objectTrackingPipelineConfiguration, value); }
        }

        /// <summary>
        /// Gets or sets the maximum frequency with which to output the interaction state.
        /// </summary>
        [DataMember]
        [DisplayName("Max Interaction State Output Freq.")]
        [Description("The maximum frequency with which to output the interaction state.")]
        public double MaxInteractionStateOutputFrequency
        {
            get => this.maxInteractionStateOutputFrequency;
            set { this.Set(nameof(this.MaxInteractionStateOutputFrequency), ref this.maxInteractionStateOutputFrequency, value); }
        }

        /// <summary>
        /// Gets or sets the filename for the LLM query library.
        /// </summary>
        [DataMember]
        [DisplayName("LLM Query Library Filename")]
        [Description("The filename for the LLM query library.")]
        public string LLMQueryLibraryFilename
        {
            get => this.llmQueryLibraryFilename;
            set { this.Set(nameof(this.LLMQueryLibraryFilename), ref this.llmQueryLibraryFilename, value); }
        }

        /// <summary>
        /// Gets or sets the persistent state filename.
        /// </summary>
        [DataMember]
        [DisplayName("Persistent State Filename")]
        [Description("The filename for the persistent state.")]
        public string PersistentStateFilename
        {
            get => this.persistentStateFilename;
            set { this.Set(nameof(this.PersistentStateFilename), ref this.persistentStateFilename, value); }
        }

        /// <summary>
        /// Gets or sets the delayed start time interval.
        /// </summary>
        [DataMember]
        [DisplayName("Delayed Start")]
        [Description("Indicate whether to delay the start of the interaction.")]
        public TimeSpan DelayedStart
        {
            get => this.delayedStart;
            set { this.Set(nameof(this.DelayedStart), ref this.delayedStart, value); }
        }

        /// <summary>
        /// Gets a value indicating whether to use LLM queries.
        /// </summary>
        [XmlIgnore]
        public bool UsesLLMQueryLibrary => !string.IsNullOrEmpty(this.LLMQueryLibraryFilename);

        /// <summary>
        /// Creates a live compute server pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the components to.</param>
        /// <param name="hololensStreams">The set of hololens streams.</param>
        /// <param name="inputRendezvousProcess">The input rendezvous process.</param>
        /// <param name="exporter">The exporter to write streams to.</param>
        /// <returns>The Sigma compute server pipeline.</returns>
        public abstract ISigmaComputeServerPipeline CreateLiveComputeServerPipeline(
            Pipeline pipeline,
            HoloLensStreams hololensStreams,
            Rendezvous.Process inputRendezvousProcess,
            Exporter exporter);

        /// <summary>
        /// Creates a batch compute server pipeline.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the components to.</param>
        /// <param name="hololensStreams">The set of hololens streams.</param>
        /// <param name="importer">The importer to read streams from.</param>
        /// <param name="exporter">The exporter to write streams to.</param>
        /// <returns>The Sigma compute server pipeline.</returns>
        public abstract ISigmaComputeServerPipeline CreateBatchComputeServerPipeline(
            Pipeline pipeline,
            HoloLensStreams hololensStreams,
            Importer importer,
            Exporter exporter);

        /// <summary>
        /// Checks that the configuration is valid, and throws an exception otherwise.
        /// </summary>
        public virtual void Validate()
        {
            if (!string.IsNullOrEmpty(this.LLMQueryLibraryFilename) && !File.Exists(this.LLMQueryLibraryFilename))
            {
                throw new Exception($"The specified {nameof(this.LLMQueryLibraryFilename)} cannot be found.");
            }
        }
    }
}

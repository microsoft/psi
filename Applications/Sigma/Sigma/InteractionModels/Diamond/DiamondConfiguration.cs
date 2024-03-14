// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Diamond
{
    using System.Runtime.Serialization;
    using Microsoft.Psi;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Interop.Rendezvous;
    using Microsoft.Psi.MixedReality.Applications;

    /// <summary>
    /// Task generation policies.
    /// </summary>
    public enum TaskGenerationPolicy
    {
        /// <summary>
        /// Only use tasks predefined in the library.
        /// </summary>
        FromLibraryOnly,

        /// <summary>
        /// Use predefined tasks when available, and use LLM task generation otherwise.
        /// </summary>
        FromLibraryOrLLMGenerate,

        /// <summary>
        /// Always use LLM task generation.
        /// </summary>
        AlwaysLLMGenerate,
    }

    /// <summary>
    /// Represents the configuration for the <see cref="DiamondComputeServerPipeline"/>.
    /// </summary>
    public class DiamondConfiguration : SigmaComputeServerPipelineConfiguration
    {
        private TaskGenerationPolicy taskGenerationPolicy = TaskGenerationPolicy.FromLibraryOrLLMGenerate;
        private bool askContextQuestionsBeforeGeneratingTask = true;
        private bool moveGemToShowObjectLocations = true;
        private string autoStartTaskName = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiamondConfiguration"/> class.
        /// </summary>
        public DiamondConfiguration()
            : this(ConfigurationType.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiamondConfiguration"/> class.
        /// </summary>
        /// <param name="configurationType">The configuration type.</param>
        public DiamondConfiguration(ConfigurationType configurationType = ConfigurationType.Default)
            : base(configurationType)
        {
            this.Name = "Diamond";

            if (configurationType == ConfigurationType.Template)
            {
                this.TaskGenerationPolicy = TaskGenerationPolicy.FromLibraryOrLLMGenerate;
                this.AskContextQuestionsBeforeGeneratingTask = false;
                this.MoveGemToShowObjectLocations = true;
                this.AutoStartTaskName = "please fill in with a task name if autostart is desired, or leave empty otherwise";
            }
        }

        /// <summary>
        /// Gets or sets the task generation policy.
        /// </summary>
        [DataMember]
        public TaskGenerationPolicy TaskGenerationPolicy
        {
            get => this.taskGenerationPolicy;
            set { this.Set(nameof(this.TaskGenerationPolicy), ref this.taskGenerationPolicy, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the system should ask context questions before generating a task.
        /// </summary>
        [DataMember]
        public bool AskContextQuestionsBeforeGeneratingTask
        {
            get => this.askContextQuestionsBeforeGeneratingTask;
            set { this.Set(nameof(this.AskContextQuestionsBeforeGeneratingTask), ref this.askContextQuestionsBeforeGeneratingTask, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the gem should move to show object locations.
        /// </summary>
        [DataMember]
        public bool MoveGemToShowObjectLocations
        {
            get => this.moveGemToShowObjectLocations;
            set { this.Set(nameof(this.MoveGemToShowObjectLocations), ref this.moveGemToShowObjectLocations, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the system should auto-start a specific task.
        /// </summary>
        [DataMember]
        public string AutoStartTaskName
        {
            get => this.autoStartTaskName;
            set { this.Set(nameof(this.AutoStartTaskName), ref this.autoStartTaskName, value); }
        }

        /// <inheritdoc/>
        public override void Validate()
        {
            base.Validate();

            if ((this.TaskGenerationPolicy == TaskGenerationPolicy.FromLibraryOrLLMGenerate ||
                this.TaskGenerationPolicy == TaskGenerationPolicy.AlwaysLLMGenerate) &&
                !this.UsesLLMQueryLibrary)
            {
                throw new InvalidDataContractException("Cannot user LLM generated tasks without specifying an LLM query library.");
            }
        }

        /// <inheritdoc/>
        public override ISigmaComputeServerPipeline CreateLiveComputeServerPipeline(
            Pipeline pipeline,
            HoloLensStreams hololensStreams,
            Rendezvous.Process inputRendezvousProcess,
            Exporter exporter)
            => new DiamondComputeServerPipeline(
                pipeline,
                this,
                hololensStreams,
                new UserInterfaceStreams<DiamondUserInterfaceState>(pipeline, inputRendezvousProcess));

        /// <inheritdoc/>
        public override ISigmaComputeServerPipeline CreateBatchComputeServerPipeline(
            Pipeline pipeline,
            HoloLensStreams hololensStreams,
            Importer importer,
            Exporter exporter)
            => new DiamondComputeServerPipeline(
                pipeline,
                this,
                hololensStreams,
                new UserInterfaceStreams<DiamondUserInterfaceState>(importer, "UserInterfaceStreams"),
                new PrecomputedStreams(importer, "Sigma"));
    }
}

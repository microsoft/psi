// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a view model for the <see cref="RunBatchProcessingTaskWindow"/>.
    /// </summary>
    public class RunBatchProcessingTaskWindowViewModel : ObservableObject
    {
        private readonly VisualizationContainer visualizationContainer;
        private readonly DatasetViewModel datasetViewModel;
        private readonly SessionViewModel sessionViewModel;
        private readonly BatchProcessingTaskMetadata batchProcessingTaskMetadata;
        private Visibility configVisibility = Visibility.Visible;
        private Visibility runningVisibility = Visibility.Collapsed;
        private string name = null;
        private string description = null;
        private string target = null;
        private string dataSize = null;
        private double progress = 0;
        private string percentCompleteAsString = null;
        private string elapsedTime = null;
        private string estimatedRemainingTime = null;
        private BatchProcessingTaskConfiguration configuration = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunBatchProcessingTaskWindowViewModel"/> class.
        /// </summary>
        /// <param name="visualizationContainer">The visualization container.</param>
        /// <param name="datasetViewModel">The dataset view model.</param>
        /// <param name="batchProcessingTaskMetadata">The batch processing task metadata.</param>
        public RunBatchProcessingTaskWindowViewModel(VisualizationContainer visualizationContainer, DatasetViewModel datasetViewModel, BatchProcessingTaskMetadata batchProcessingTaskMetadata)
        {
            this.visualizationContainer = visualizationContainer;
            this.datasetViewModel = datasetViewModel;
            this.batchProcessingTaskMetadata = batchProcessingTaskMetadata;
            this.Name = batchProcessingTaskMetadata.Name;
            this.Description = batchProcessingTaskMetadata.Description;
            this.Target = datasetViewModel.Name;
            this.DataSize = TimeSpanFormatHelper.FormatTimeSpanApproximate(
                new TimeSpan(datasetViewModel.SessionViewModels.Sum(svm => svm.OriginatingTimeInterval.Span.Ticks)));
            this.Configuration = batchProcessingTaskMetadata.GetDefaultConfiguration();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RunBatchProcessingTaskWindowViewModel"/> class.
        /// </summary>
        /// <param name="visualizationContainer">The visualization container.</param>
        /// <param name="sessionViewModel">The dataset view model.</param>
        /// <param name="batchProcessingTaskMetadata">The batch processing task metadata.</param>
        public RunBatchProcessingTaskWindowViewModel(VisualizationContainer visualizationContainer, SessionViewModel sessionViewModel, BatchProcessingTaskMetadata batchProcessingTaskMetadata)
        {
            this.visualizationContainer = visualizationContainer;
            this.sessionViewModel = sessionViewModel;
            this.batchProcessingTaskMetadata = batchProcessingTaskMetadata;
            this.Name = batchProcessingTaskMetadata.Name;
            this.Description = batchProcessingTaskMetadata.Description;
            this.Target = sessionViewModel.Name;
            this.DataSize = TimeSpanFormatHelper.FormatTimeSpanApproximate(sessionViewModel.OriginatingTimeInterval.Span);
            this.Configuration = batchProcessingTaskMetadata.GetDefaultConfiguration();
        }

        /// <summary>
        /// Gets or sets the task name.
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.Set(nameof(this.Name), ref this.name, value);
        }

        /// <summary>
        /// Gets or sets the current target of the task.
        /// </summary>
        public string Description
        {
            get => this.description;
            set => this.Set(nameof(this.description), ref this.description, value);
        }

        /// <summary>
        /// Gets or sets the current target of the task.
        /// </summary>
        public string Target
        {
            get => this.target;
            set => this.Set(nameof(this.Target), ref this.target, value);
        }

        /// <summary>
        /// Gets or sets the data size.
        /// </summary>
        public string DataSize
        {
            get => this.dataSize;
            set => this.Set(nameof(this.DataSize), ref this.dataSize, value);
        }

        /// <summary>
        /// Gets or sets the progress of the task.
        /// </summary>
        public double Progress
        {
            get => this.progress;
            set => this.Set(nameof(this.Progress), ref this.progress, value);
        }

        /// <summary>
        /// Gets or sets the percentage complete as string.
        /// </summary>
        public string PercentageCompleteAsString
        {
            get => this.percentCompleteAsString;
            set => this.Set(nameof(this.PercentageCompleteAsString), ref this.percentCompleteAsString, value);
        }

        /// <summary>
        /// Gets or sets the elapsed time.
        /// </summary>
        public string ElapsedTime
        {
            get => this.elapsedTime;
            set => this.Set(nameof(this.ElapsedTime), ref this.elapsedTime, value);
        }

        /// <summary>
        /// Gets or sets the estimated remaining time.
        /// </summary>
        public string EstimatedRemainingTime
        {
            get => this.estimatedRemainingTime;
            set => this.Set(nameof(this.EstimatedRemainingTime), ref this.estimatedRemainingTime, value);
        }

        /// <summary>
        /// Gets or sets the batch processing task configuration.
        /// </summary>
        public BatchProcessingTaskConfiguration Configuration
        {
            get => this.configuration;
            set => this.Set(nameof(this.Configuration), ref this.configuration, value);
        }

        /// <summary>
        /// Gets or sets the configuration-time visibility.
        /// </summary>
        public Visibility ConfigVisibility
        {
            get => this.configVisibility;
            set => this.Set(nameof(this.ConfigVisibility), ref this.configVisibility, value);
        }

        /// <summary>
        /// Gets or sets the running-time visibility.
        /// </summary>
        public Visibility RunningVisibility
        {
            get => this.runningVisibility;
            set => this.Set(nameof(this.RunningVisibility), ref this.runningVisibility, value);
        }

        /// <summary>
        /// Run the batch processing task.
        /// </summary>
        /// <returns>The async threading task that runs the batch processing task.</returns>
        public async Task RunAsync()
        {
            this.ConfigVisibility = Visibility.Collapsed;
            this.RunningVisibility = Visibility.Visible;

            var startTime = DateTime.MinValue;

            // Unbind any visualizers currently bound to the output store
            this.visualizationContainer.UnbindVisualizationObjectsFromStore(this.configuration.OutputStoreName, this.configuration.OutputStorePath, null);

            // Initialize progress reporter for the status window
            if (this.datasetViewModel != null)
            {
                var progress = new Progress<(string, double)>(tuple =>
                {
                    if (startTime == DateTime.MinValue)
                    {
                        startTime = DateTime.UtcNow;
                    }

                    this.Target = $"{this.datasetViewModel.Name} : {tuple.Item1}";
                    this.Progress = tuple.Item2 * 100;
                    this.PercentageCompleteAsString = $"{this.Progress:0.0}%";
                    var elapsedTime = DateTime.UtcNow - startTime;
                    var estimatedRemainingTime = TimeSpan.FromTicks((long)(elapsedTime.Ticks * ((1 - tuple.Item2) / tuple.Item2)));
                    this.ElapsedTime = TimeSpanFormatHelper.FormatTimeSpanApproximate(elapsedTime);
                    this.EstimatedRemainingTime = "about " + TimeSpanFormatHelper.FormatTimeSpanApproximate(estimatedRemainingTime);
                });

                await this.datasetViewModel.Dataset.CreateDerivedPartitionAsync(
                    (pipeline, sessionImporter, exporter) => this.batchProcessingTaskMetadata.Run(pipeline, sessionImporter, exporter, this.Configuration),
                    this.Configuration.OutputPartitionName,
                    overwrite: true,
                    outputStoreName: this.Configuration.OutputStoreName,
                    outputStorePath: this.Configuration.OutputStorePath,
                    replayDescriptor: this.Configuration.ReplayAllRealTime ? ReplayDescriptor.ReplayAllRealTime : ReplayDescriptor.ReplayAll,
                    deliveryPolicy: this.Configuration.DeliveryPolicyLatestMessage ? DeliveryPolicy.LatestMessage : null,
                    enableDiagnostics: this.Configuration.EnableDiagnostics,
                    progress: progress);
            }
            else
            {
                var progress = new Progress<(string, double)>(tuple =>
                {
                    if (startTime == DateTime.MinValue)
                    {
                        startTime = DateTime.UtcNow;
                    }

                    this.Target = tuple.Item1;
                    this.Progress = tuple.Item2 * 100;
                    this.PercentageCompleteAsString = $"{this.Progress:0.0}%";
                    var elapsedTime = DateTime.UtcNow - startTime;
                    var estimatedRemainingTime = TimeSpan.FromTicks((long)(elapsedTime.Ticks * ((1 - tuple.Item2) / tuple.Item2)));
                    this.ElapsedTime = TimeSpanFormatHelper.FormatTimeSpanApproximate(elapsedTime);
                    this.EstimatedRemainingTime = "about " + TimeSpanFormatHelper.FormatTimeSpanApproximate(estimatedRemainingTime);
                });

                await this.sessionViewModel.Session.CreateDerivedPartitionAsync(
                    (pipeline, sessionImporter, exporter) => this.batchProcessingTaskMetadata.Run(pipeline, sessionImporter, exporter, this.Configuration),
                    this.Configuration.OutputPartitionName,
                    overwrite: true,
                    outputStoreName: this.Configuration.OutputStoreName,
                    outputStorePath: this.Configuration.OutputStorePath,
                    replayDescriptor: this.Configuration.ReplayAllRealTime ? ReplayDescriptor.ReplayAllRealTime : ReplayDescriptor.ReplayAll,
                    deliveryPolicy: this.Configuration.DeliveryPolicyLatestMessage ? DeliveryPolicy.LatestMessage : null,
                    enableDiagnostics: this.Configuration.EnableDiagnostics,
                    progress: progress);
            }
        }
    }
}

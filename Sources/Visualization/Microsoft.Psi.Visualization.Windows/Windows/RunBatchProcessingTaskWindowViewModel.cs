// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Implements a view model for the <see cref="RunBatchProcessingTaskWindow"/>.
    /// </summary>
    public class RunBatchProcessingTaskWindowViewModel : ObservableObject
    {
        private const string DefaultConfiguration = "<Default>";
        private const string ConfigurationExtension = ".btconfig";

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
        private string currentConfiguration;
        private List<string> availableConfigurations = new ();
        private RelayCommand saveConfigurationCommand;
        private RelayCommand saveConfigurationAsCommand;
        private RelayCommand resetConfigurationCommand;
        private RelayCommand deleteConfigurationCommand;

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
            this.DataSize = TimeSpanHelper.FormatTimeSpanApproximate(
                new TimeSpan(datasetViewModel.SessionViewModels.Sum(svm => svm.OriginatingTimeInterval.Span.Ticks)));
            this.LoadAvailableConfigurations();
            this.CurrentConfiguration = batchProcessingTaskMetadata.MostRecentlyUsedConfiguration ?? DefaultConfiguration;
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
            this.DataSize = TimeSpanHelper.FormatTimeSpanApproximate(sessionViewModel.OriginatingTimeInterval.Span);
            this.LoadAvailableConfigurations();
            this.CurrentConfiguration = batchProcessingTaskMetadata.MostRecentlyUsedConfiguration ?? DefaultConfiguration;
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
        /// Gets or sets the name of the current configuration.
        /// </summary>
        public string CurrentConfiguration
        {
            get => this.currentConfiguration;
            set
            {
                this.RaisePropertyChanging(nameof(this.CurrentConfiguration));

                this.currentConfiguration = value ?? DefaultConfiguration;
                this.batchProcessingTaskMetadata.MostRecentlyUsedConfiguration = this.currentConfiguration;

                this.LoadCurrentConfiguration();

                this.RaisePropertyChanged(nameof(this.CurrentConfiguration));
            }
        }

        /// <summary>
        /// Gets or sets the collection of available configurations.
        /// </summary>
        public List<string> AvailableConfigurations
        {
            get => this.availableConfigurations;
            set => this.Set(nameof(this.AvailableConfigurations), ref this.availableConfigurations, value);
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
        /// Gets the save configuration command.
        /// </summary>
        public RelayCommand SaveConfigurationCommand
            => this.saveConfigurationCommand ??= new RelayCommand(
                () =>
                {
                    if (this.CurrentConfiguration == DefaultConfiguration)
                    {
                        this.SaveConfigurationAs();
                    }
                    else
                    {
                        this.SaveConfiguration();
                    }
                });

        /// <summary>
        /// Gets the save configuration as command.
        /// </summary>
        public RelayCommand SaveConfigurationAsCommand
            => this.saveConfigurationAsCommand ??= new RelayCommand(() => this.SaveConfigurationAs());

        /// <summary>
        /// Gets the reset configuration command.
        /// </summary>
        public RelayCommand ResetConfigurationCommand
            => this.resetConfigurationCommand ??= new RelayCommand(() => this.ResetConfiguration());

        /// <summary>
        /// Gets the delete configuration command.
        /// </summary>
        public RelayCommand DeleteConfigurationCommand
            => this.deleteConfigurationCommand ??= new RelayCommand(() => this.DeleteConfiguration(), () => this.CurrentConfiguration != DefaultConfiguration);

        /// <summary>
        /// Run the batch processing task.
        /// </summary>
        /// <param name="cancellationToken">An optional token for canceling the asynchronous task.</param>
        /// <returns>The async threading task that runs the batch processing task.</returns>
        public async Task RunAsync(CancellationToken cancellationToken = default)
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
                    this.ElapsedTime = TimeSpanHelper.FormatTimeSpanApproximate(elapsedTime);
                    this.EstimatedRemainingTime = "about " + TimeSpanHelper.FormatTimeSpanApproximate(estimatedRemainingTime);
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
                    progress: progress,
                    cancellationToken: cancellationToken);
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
                    this.ElapsedTime = TimeSpanHelper.FormatTimeSpanApproximate(elapsedTime);
                    this.EstimatedRemainingTime = "about " + TimeSpanHelper.FormatTimeSpanApproximate(estimatedRemainingTime);
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
                    progress: progress,
                    cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Loads the list of available configurations.
        /// </summary>
        private void LoadAvailableConfigurations()
        {
            // Create a new collection of configurations
            var configurations = new List<string> { DefaultConfiguration };

            // Find all the configuration files and add them to the list of available configurations
            var directoryInfo = this.EnsureDirectoryExists(this.batchProcessingTaskMetadata.ConfigurationsPath);
            var configurationFiles = directoryInfo.GetFiles($"*{ConfigurationExtension}");
            foreach (FileInfo fileInfo in configurationFiles)
            {
                string configurationName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                configurations.Add(configurationName);
            }

            // Set the list of available configurations
            this.AvailableConfigurations = configurations;
        }

        /// <summary>
        /// Loads the currently selected configuration.
        /// </summary>
        private void LoadCurrentConfiguration()
        {
            if (this.CurrentConfiguration == DefaultConfiguration)
            {
                // Reset the configuration to the default values
                this.Configuration = this.batchProcessingTaskMetadata.GetDefaultConfiguration();
            }
            else
            {
                // Attempt to load the configuration from file
                string savedConfigurationFile = Path.Combine(
                    this.batchProcessingTaskMetadata.ConfigurationsPath,
                    this.CurrentConfiguration + ConfigurationExtension);

                try
                {
                    this.Configuration = BatchProcessingTaskConfiguration.Load(savedConfigurationFile);
                }
                catch (Exception e)
                {
                    _ = new MessageBoxWindow(
                       Application.Current.MainWindow,
                       "Error loading batch task configuration",
                       "An error occurred while attempting to load the batch task configuration. The default configuration will be used instead.\r\n\r\n" + e.Message,
                       cancelButtonText: null).ShowDialog();

                    // If the load failed, revert to using the default configuration. This method
                    // may have been called by the CurrentConfiguration property setter to load
                    // the selected configuration, so we need to asynchronously dispatch a message
                    // to change its value back to the default rather than set it directly here.
                    Application.Current?.Dispatcher.InvokeAsync(() => this.CurrentConfiguration = DefaultConfiguration);
                }
            }
        }

        /// <summary>
        /// Saves the current configuration.
        /// </summary>
        private void SaveConfiguration()
        {
            if (this.CurrentConfiguration == DefaultConfiguration)
            {
                this.SaveConfigurationAs();
            }
            else
            {
                try
                {
                    this.EnsureDirectoryExists(this.batchProcessingTaskMetadata.ConfigurationsPath);

                    string fileName = Path.Combine(
                        this.batchProcessingTaskMetadata.ConfigurationsPath,
                        this.CurrentConfiguration + ConfigurationExtension);

                    this.Configuration.Save(fileName);
                }
                catch (Exception e)
                {
                    _ = new MessageBoxWindow(
                       Application.Current.MainWindow,
                       "Error saving batch task configuration",
                       "An error occurred while saving the batch task configuration:\r\n\r\n" + e.Message,
                       cancelButtonText: null).ShowDialog();
                }
            }
        }

        /// <summary>
        /// Saves the current configuration as a new named configuration.
        /// </summary>
        private void SaveConfigurationAs()
        {
            var configurationNameWindow = new GetParameterWindow(
                Application.Current.MainWindow,
                "Save Configuration As...",
                "Configuration Name",
                string.Empty);

            bool? result = configurationNameWindow.ShowDialog();
            if (result == true)
            {
                string configurationName = configurationNameWindow.ParameterValue;

                try
                {
                    this.EnsureDirectoryExists(this.batchProcessingTaskMetadata.ConfigurationsPath);

                    string fileName = Path.Combine(
                        this.batchProcessingTaskMetadata.ConfigurationsPath,
                        configurationName + ConfigurationExtension);

                    // Save the configuration
                    this.Configuration.Save(fileName);

                    // Recreate the configuration list
                    this.LoadAvailableConfigurations();

                    // Set the current configuration
                    this.CurrentConfiguration = this.AvailableConfigurations.First(c => c == configurationName);
                }
                catch (Exception e)
                {
                    _ = new MessageBoxWindow(
                       Application.Current.MainWindow,
                       "Error saving batch task configuration",
                       "An error occurred while saving the batch task configuration:\r\n\r\n" + e.Message,
                       cancelButtonText: null).ShowDialog();
                }
            }
        }

        /// <summary>
        /// Resets the current configuration to the default values.
        /// </summary>
        private void ResetConfiguration()
        {
            this.Configuration = this.batchProcessingTaskMetadata.GetDefaultConfiguration();
        }

        /// <summary>
        /// Deletes the current configuration.
        /// </summary>
        private void DeleteConfiguration()
        {
            var result = new MessageBoxWindow(
               Application.Current.MainWindow,
               "Are you sure?",
               $"Are you sure you want to delete the batch task configuration named \"{this.CurrentConfiguration}\"? This will permanently delete it from disk.",
               "Yes",
               "Cancel").ShowDialog();

            if (result == true)
            {
                string configurationName = this.CurrentConfiguration;
                this.CurrentConfiguration = DefaultConfiguration;

                string fileName = Path.Combine(
                    this.batchProcessingTaskMetadata.ConfigurationsPath,
                    configurationName + ConfigurationExtension);

                File.Delete(fileName);
                this.LoadAvailableConfigurations();
                this.CurrentConfiguration = DefaultConfiguration;
            }
        }

        /// <summary>
        /// Ensures that the directory specified by the path exists, creating it if necessary.
        /// </summary>
        /// <param name="directoryPath">The path to the directory.</param>
        /// <returns>A <see cref="DirectoryInfo"/> object representing the directory.</returns>
        private DirectoryInfo EnsureDirectoryExists(string directoryPath)
        {
            var directoryInfo = new DirectoryInfo(this.batchProcessingTaskMetadata.ConfigurationsPath);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            return directoryInfo;
        }
    }
}

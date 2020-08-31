// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for RunBatchProcessingTaskWindow.xaml.
    /// </summary>
    public partial class RunBatchProcessingTaskWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunBatchProcessingTaskWindow"/> class.
        /// </summary>
        /// <param name="owner">The owner of this window.</param>
        /// <param name="runningTask">The task running.</param>
        /// <param name="target">The target object the task is running on.</param>
        public RunBatchProcessingTaskWindow(Window owner, string runningTask, string target = null)
        {
            this.InitializeComponent();

            this.Owner = owner;
            this.TaskName = runningTask;
            this.Target = target;
        }

        /// <summary>
        /// Gets or sets the task name.
        /// </summary>
        public string TaskName
        {
            get { return (string)this.TaskNameLabel.Content; }
            set { this.TaskNameLabel.Content = value; }
        }

        /// <summary>
        /// Gets or sets the current target of the task.
        /// </summary>
        public string Target
        {
            get { return (string)this.TargetLabel.Content; }
            set { this.TargetLabel.Content = value; }
        }

        /// <summary>
        /// Gets or sets the progress of the task.
        /// </summary>
        public double Progress
        {
            get { return this.ProgressBar.Value; }

            set
            {
                this.PercentCompleteLabel.Content = $"{value:0.0}%";
                this.ProgressBar.Value = value;
            }
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Windows;

    /// <summary>
    /// Interaction logic for RunBatchProcessingTaskWindow.xaml.
    /// </summary>
    public partial class RunBatchProcessingTaskWindow : Window
    {
        private DateTime startTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunBatchProcessingTaskWindow"/> class.
        /// </summary>
        /// <param name="owner">The owner of this window.</param>
        /// <param name="runningTask">The task running.</param>
        /// <param name="target">The target object the task is currently running on.</param>
        /// <param name="dataSize">A parameter specifying the data size. </param>
        public RunBatchProcessingTaskWindow(Window owner, string runningTask, string target, TimeSpan dataSize)
        {
            this.InitializeComponent();

            this.Owner = owner;
            this.TaskName = runningTask;
            this.Target = target;
            this.DataSizeLabel.Content = dataSize.ToString();
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

                if (this.startTime == DateTime.MinValue)
                {
                    this.startTime = DateTime.Now;
                }
                else if (value > 0)
                {
                    var progress = value * 0.01;
                    var elapsedTime = DateTime.Now - this.startTime;
                    var estimatedRemainingTime = TimeSpan.FromTicks((long)(elapsedTime.Ticks * ((1 - progress) / progress)));
                    this.ElapsedTimeLabel.Content = this.GetTimeSpanAsFriendlyString(elapsedTime);
                    this.EstimatedRemainingTimeLabel.Content = "about " + this.GetTimeSpanAsFriendlyString(estimatedRemainingTime);
                }
            }
        }

        private string GetTimeSpanAsFriendlyString(TimeSpan timeSpan)
        {
            var result = string.Empty;

            if (timeSpan.Days > 1)
            {
                result += $"{timeSpan.Days} days, ";
            }
            else if (timeSpan.Days == 1)
            {
                result += $"{timeSpan.Days} day, ";
            }

            if (timeSpan.Hours > 1)
            {
                result += $"{timeSpan.Hours} hours, ";
            }
            else if (timeSpan.Hours == 1)
            {
                result += $"{timeSpan.Hours} hour, ";
            }

            if (timeSpan.Days < 1)
            {
                if (timeSpan.Minutes > 1)
                {
                    result += $"{timeSpan.Minutes} minutes, ";
                }
                else if (timeSpan.Minutes == 1)
                {
                    result += $"{timeSpan.Minutes} minute, ";
                }

                if (timeSpan.Hours < 1)
                {
                    if (timeSpan.Seconds > 1 || timeSpan.Seconds == 0)
                    {
                        result += $"{timeSpan.Seconds} seconds, ";
                    }
                    else if (timeSpan.Seconds == 1)
                    {
                        result += $"{timeSpan.Seconds} second, ";
                    }
                }
            }

            return result.EndsWith(", ") ? result.TrimEnd(new char[] { ',', ' ' }) + "." : result;
        }
    }
}
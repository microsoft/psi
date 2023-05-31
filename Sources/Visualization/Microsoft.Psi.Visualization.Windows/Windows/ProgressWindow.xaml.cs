// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.ComponentModel;
    using System.Windows;

    /// <summary>
    /// Interaction logic for ProgressWindow.xaml.
    /// </summary>
    public partial class ProgressWindow : Window, INotifyPropertyChanged
    {
        private double progress;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressWindow"/> class.
        /// </summary>
        /// <param name="owner">The owner of this window.</param>
        /// <param name="progressText">The text in the progress window.</param>
        /// <param name="showCancelButton">Whether the progress window should display the Cancel button.</param>
        public ProgressWindow(Window owner, string progressText, bool showCancelButton = false)
        {
            this.InitializeComponent();

            this.DataContext = this;
            this.Owner = owner;
            this.ShowCancelButton = showCancelButton;
            this.ProgressText = progressText;
            this.Progress = 0d;
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the text of the progress window.
        /// </summary>
        public string ProgressText { get; private set; }

        /// <summary>
        /// Gets or sets the progress of the task.
        /// </summary>
        public double Progress
        {
            get => this.progress;

            set
            {
                this.progress = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Progress)));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Cancel button should be displayed.
        /// </summary>
        public bool ShowCancelButton { get; }
    }
}
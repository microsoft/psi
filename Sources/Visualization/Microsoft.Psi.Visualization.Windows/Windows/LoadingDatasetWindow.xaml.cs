// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for LoadingDatasetWindow.xaml.
    /// </summary>
    public partial class LoadingDatasetWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingDatasetWindow"/> class.
        /// </summary>
        /// <param name="owner">The owner of this window.</param>
        /// <param name="filename">The filename of the dataset.</param>
        /// <param name="status">The initial status message to display.</param>
        public LoadingDatasetWindow(Window owner, string filename, string status = null)
        {
            this.InitializeComponent();

            this.Owner = owner;
            this.Filename = filename;
            this.Status = status;
        }

        /// <summary>
        /// Gets or sets the filename of the dataset.
        /// </summary>
        public string Filename
        {
            get { return (string)this.FilenameLabel.Content; }
            set { this.FilenameLabel.Content = value; }
        }

        /// <summary>
        /// Gets or sets the current status message to display.
        /// </summary>
        public string Status
        {
            get { return (string)this.FilenameLabel.Content; }
            set { this.StatusLabel.Content = value; }
        }
    }
}
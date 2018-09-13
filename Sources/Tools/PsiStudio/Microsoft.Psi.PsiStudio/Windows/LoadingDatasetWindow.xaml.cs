// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for LoadingDatasetWindow.xaml
    /// </summary>
    public partial class LoadingDatasetWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingDatasetWindow"/> class.
        /// </summary>
        /// <param name="filename">The filename of the dataset being loaded.</param>
        /// <param name="owner">The owner of this window.</param>
        public LoadingDatasetWindow(string filename, Window owner)
        {
            this.InitializeComponent();

            this.Owner = owner;
            this.DataContext = this;
            this.Filename = filename;
        }

        /// <summary>
        /// Gets the filename of the dataset being loaded.
        /// </summary>
        public string Filename { get; }
    }
}
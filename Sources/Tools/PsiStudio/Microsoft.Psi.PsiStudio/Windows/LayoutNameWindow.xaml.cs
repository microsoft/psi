// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Windows
{
    using System;
    using System.IO;
    using System.Windows;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Interaction logic for LayoutNameWindow.xaml.
    /// </summary>
    public partial class LayoutNameWindow : Window
    {
        private string layoutsPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutNameWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        /// <param name="layoutsPath">The path to the layouts directory.</param>
        public LayoutNameWindow(Window owner, string layoutsPath)
        {
            if (string.IsNullOrWhiteSpace(layoutsPath))
            {
                throw new ArgumentNullException(nameof(layoutsPath));
            }

            this.InitializeComponent();
            this.Owner = owner;
            this.layoutsPath = layoutsPath;
        }

        /// <summary>
        /// Gets the layout name.
        /// </summary>
        public string LayoutName => this.LayoutNameTextBox.Text + ".plo";

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // Check that a layout with the same name does not already exist
            if (File.Exists(Path.Combine(this.layoutsPath, this.LayoutName)))
            {
                new MessageBoxWindow(this.Owner, "Layout already exists", $"A layout named {this.LayoutNameTextBox.Text} cannot be created because a layout with that name already exists", "Close", null).ShowDialog();
                return;
            }

            this.DialogResult = true;
            e.Handled = true;
        }
    }
}

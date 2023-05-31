// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Windows;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Interaction logic for MessageBoxWindow.xaml.
    /// </summary>
    public partial class SaveStoreWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SaveStoreWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        /// <param name="partitionViewModel">The partition that has unsaved changes.</param>
        public SaveStoreWindow(Window owner, PartitionViewModel partitionViewModel)
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Owner = owner;
            this.Text = $"The partition {partitionViewModel.Name} has unsaved changes.{Environment.NewLine}{Environment.NewLine}Do you wish to save these changes to disk before continuing?";
        }

        /// <summary>
        /// An enumeration representing the user's selection.
        /// </summary>
        public enum SaveStoreWindowResult
        {
            /// <summary>
            /// Save uncommitted changes.
            /// </summary>
            SaveChanges,

            /// <summary>
            /// Undo uncommitted changes.
            /// </summary>
            UndoChanges,

            /// <summary>
            /// Cancel the operation that launched this dialog window.
            /// </summary>
            Cancel,
        }

        /// <summary>
        /// Gets the option that the user selected.
        /// </summary>
        public SaveStoreWindowResult UserSelection { get; private set; }

        /// <summary>
        /// Gets or sets the text of the messagebox.
        /// </summary>
        public string Text { get; set; }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            this.UserSelection = SaveStoreWindowResult.SaveChanges;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            this.UserSelection = SaveStoreWindowResult.UndoChanges;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.UserSelection = SaveStoreWindowResult.Cancel;
            this.Close();
        }
    }
}

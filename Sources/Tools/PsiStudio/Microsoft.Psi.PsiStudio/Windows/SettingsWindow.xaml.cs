// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;
    using System.Windows.Forms;

    /// <summary>
    /// Interaction logic for SettingsWindow.xaml.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
        /// </summary>
        public SettingsWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the directory to search for layout files.
        /// </summary>
        public string LayoutsDirectory
        {
            get { return this.LayoutsDirectoryTextBox.Text; }
            set { this.LayoutsDirectoryTextBox.Text = value; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            e.Handled = true;
        }

        private void SelectDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "Select the folder where PsiStudio will search for layout files (*.plo files)";
            dlg.SelectedPath = this.LayoutsDirectory;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.LayoutsDirectory = dlg.SelectedPath;
            }
        }
    }
}

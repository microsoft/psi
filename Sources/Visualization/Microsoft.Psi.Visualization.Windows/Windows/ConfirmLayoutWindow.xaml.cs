// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for ConfirmLayoutWindow.xaml.
    /// </summary>
    public partial class ConfirmLayoutWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmLayoutWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        /// <param name="layoutName">The nane of the layout to confirm.</param>
        public ConfirmLayoutWindow(Window owner, string layoutName)
        {
            this.InitializeComponent();
            this.Title = "Layout Script Security Warning";
            this.Warning.Text = $"The layout {layoutName} contains one or more embedded scripts which will be executed on the data when applied. These scripts execute code on your machine to generate derived streams for visualization. This code was not written by Microsoft and has not been verified to be free from bugs, security vulnerabilities or malware. Before continuing you should verify that this layout has come from a trusted source.";
            this.WarningQuestion.Text = "Are you sure you want to apply this layout?";
            this.DataContext = this;
            this.Owner = owner;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            e.Handled = true;
        }
    }
}

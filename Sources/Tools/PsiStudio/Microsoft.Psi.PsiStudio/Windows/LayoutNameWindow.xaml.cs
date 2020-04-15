// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for LayoutNameWindow.xaml.
    /// </summary>
    public partial class LayoutNameWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutNameWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        public LayoutNameWindow(Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
        }

        /// <summary>
        /// Gets the layout name.
        /// </summary>
        public string LayoutName => this.LayoutNameTextBox.Text + ".plo";

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            e.Handled = true;
        }
    }
}

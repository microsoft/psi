// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for SetAnnotationValueWindow.xaml.
    /// </summary>
    public partial class SetAnnotationValueWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetAnnotationValueWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        public SetAnnotationValueWindow(Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
        }

        /// <summary>
        /// Gets or sets the annotation value.
        /// </summary>
        public string AnnotationValue
        {
            get { return this.AnnotationValueTextBox.Text; }
            set { this.AnnotationValueTextBox.Text = value; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            e.Handled = true;
        }
    }
}

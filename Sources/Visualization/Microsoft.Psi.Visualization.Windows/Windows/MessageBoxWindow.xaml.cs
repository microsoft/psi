// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for MessageBoxWindow.xaml.
    /// </summary>
    public partial class MessageBoxWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBoxWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        /// <param name="title">The text to display in the titlebar of the messagebox.</param>
        /// <param name="text">The text to display in the body of the messagebox.</param>
        /// <param name="okButtonText">The text to display on the OK button.</param>
        /// <param name="cancelButtonText">The text to display on the Cancel button.</param>
        public MessageBoxWindow(Window owner, string title, string text, string okButtonText = "OK", string cancelButtonText = "Cancel")
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Owner = owner;
            this.Title = title;
            this.Text = text;
            this.OKButton.Content = okButtonText;
            this.OKButton.Visibility = string.IsNullOrWhiteSpace(okButtonText) ? Visibility.Collapsed : Visibility.Visible;
            this.CancelButton.Content = cancelButtonText;
            this.CancelButton.Visibility = string.IsNullOrWhiteSpace(cancelButtonText) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Gets or sets the text of the messagebox.
        /// </summary>
        public string Text { get; set; }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            e.Handled = true;
        }
    }
}

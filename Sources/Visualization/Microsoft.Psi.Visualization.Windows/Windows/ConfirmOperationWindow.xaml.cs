// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for ConfirmOperationWindow.xaml.
    /// </summary>
    public partial class ConfirmOperationWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmOperationWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        /// <param name="titleText">The text to display in the title bar of the window.</param>
        /// <param name="text">The text to display in the body of the window.</param>
        public ConfirmOperationWindow(Window owner, string titleText, string text)
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Owner = owner;
            this.TitleText = titleText;
            this.Text = text;
        }

        /// <summary>
        /// An enumeration representing the user's selection.
        /// </summary>
        public enum ConfirmOperationResult
        {
            /// <summary>
            /// User confirmed the operation.
            /// </summary>
            Yes,

            /// <summary>
            /// User rejected the operation.
            /// </summary>
            No,

            /// <summary>
            /// User canceled the operation that launched this dialog window.
            /// </summary>
            Cancel,
        }

        /// <summary>
        /// Gets the option that the user selected.
        /// </summary>
        public ConfirmOperationResult UserSelection { get; private set; }

        /// <summary>
        /// Gets or sets the title text of the messagebox.
        /// </summary>
        public string TitleText { get; set; }

        /// <summary>
        /// Gets or sets the text of the messagebox.
        /// </summary>
        public string Text { get; set; }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            this.UserSelection = ConfirmOperationResult.Yes;
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            this.UserSelection = ConfirmOperationResult.No;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.UserSelection = ConfirmOperationResult.Cancel;
            this.Close();
        }
    }
}

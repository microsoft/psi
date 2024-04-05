// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for HelpWindow.xaml.
    /// </summary>
    public partial class HelpWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HelpWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        public HelpWindow(Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Collections.Generic;
    using System.Windows;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.PsiStudio;

    /// <summary>
    /// Interaction logic for AdditionalAssembliesWindow.xaml.
    /// </summary>
    public partial class AdditionalAssembliesWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdditionalAssembliesWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        /// <param name="additionalAssemblies">The list of additional assemblies the user wishes to add to PsiStudio.</param>
        public AdditionalAssembliesWindow(Window owner, List<string> additionalAssemblies)
        {
            this.InitializeComponent();
            this.Title = AdditionalAssembliesWarning.Title;
            this.WarningLine1.Text = string.Format(AdditionalAssembliesWarning.Line1, MainWindowViewModel.ApplicationName);
            this.WarningLine2.Text = AdditionalAssembliesWarning.Line2;
            this.WarningQuestion.Text = string.Format(AdditionalAssembliesWarning.Question, MainWindowViewModel.ApplicationName);
            this.AdditionalAssemblies = additionalAssemblies;
            this.DataContext = this;
            this.Owner = owner;
        }

        /// <summary>
        /// Gets the list of additional assemblies the user wishes to add to PsiStudio.
        /// </summary>
        public List<string> AdditionalAssemblies { get; private set; }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            e.Handled = true;
        }
    }
}

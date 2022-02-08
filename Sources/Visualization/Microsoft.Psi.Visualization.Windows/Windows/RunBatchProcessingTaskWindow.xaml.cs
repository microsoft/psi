// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for RunBatchProcessingTaskWindow.xaml.
    /// </summary>
    public partial class RunBatchProcessingTaskWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunBatchProcessingTaskWindow"/> class.
        /// </summary>
        /// <param name="owner">The owner of this window.</param>
        /// <param name="viewModel">The view model for this window.</param>
        public RunBatchProcessingTaskWindow(Window owner, RunBatchProcessingTaskWindowViewModel viewModel)
        {
            this.InitializeComponent();

            this.Owner = owner;
            this.DataContext = viewModel;
        }

        private RunBatchProcessingTaskWindowViewModel ViewModel => this.DataContext as RunBatchProcessingTaskWindowViewModel;

        private void RunButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.ViewModel.Configuration.Validate(out string error))
            {
                this.ViewModel
                    .RunAsync()
                    .ContinueWith(_ => Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.DialogResult = true;
                        this.Close();
                    }));
            }
            else
            {
                new MessageBoxWindow(this, "Invalid Configuration", error, cancelButtonText: null).ShowDialog();
            }
        }
    }
}
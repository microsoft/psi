// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Threading;
    using System.Windows;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Interaction logic for RunBatchProcessingTaskWindow.xaml.
    /// </summary>
    public partial class RunBatchProcessingTaskWindow : Window, IDisposable
    {
        private CancellationTokenSource cancellationTokenSource = null;

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

        /// <inheritdoc />
        public void Dispose()
        {
            this.cancellationTokenSource?.Dispose();
        }

        private async void RunButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.ViewModel.Configuration.Validate(out string error))
            {
                this.cancellationTokenSource = new ();

                await this.ViewModel
                    .RunAsync(cancellationToken: this.cancellationTokenSource.Token)
                    .ContinueWith(
                        task => Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.DialogResult = !task.IsCanceled;
                            this.Close();
                        }))
                    .ContinueWith(_ => this.cancellationTokenSource.Dispose());
            }
            else
            {
                new MessageBoxWindow(this, "Invalid Configuration", error, cancelButtonText: null).ShowDialog();
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.cancellationTokenSource?.Cancel();
        }

        private void OnPreparePropertyItem(object sender, Xceed.Wpf.Toolkit.PropertyGrid.PropertyItemEventArgs e)
        {
            // Using pattern from: https://stackoverflow.com/questions/33517266/propertygrid-specify-expandableobject-if-i-dont-have-control-of-the-class
            if (e.Item is not Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem item)
            {
                return;
            }

            if (item.PropertyType.IsSubclassOf(typeof(ObservableObject)))
            {
                e.PropertyItem.IsExpandable = true;
            }
        }
    }
}
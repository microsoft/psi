// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for ProgressWindow.xaml.
    /// </summary>
    public partial class ProgressWindow : Window, INotifyPropertyChanged, IDisposable
    {
        private double progress;
        private CancellationTokenSource cancelTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressWindow"/> class.
        /// </summary>
        /// <param name="owner">The owner of this window.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="initialStatus">The initial progress status.</param>
        /// <param name="isCancellable">Whether the progress window is cancellable.</param>
        private ProgressWindow(Window owner, string operationName, string initialStatus, bool isCancellable)
        {
            this.InitializeComponent();

            this.DataContext = this;
            this.Owner = owner;
            this.IsCancellable = isCancellable;
            this.OperationName = operationName;
            this.ProgressStatus = initialStatus;
            this.ProgressValue = 0d;

            this.Progress = new Progress<(string Status, double Progress)>(t =>
            {
                this.ProgressStatus = t.Status;
                this.ProgressValue = t.Progress * 100;
                if (t.Progress == 1.0 && !this.DialogResult.HasValue)
                {
                    this.CloseProgressWindow();
                }
            });

            if (isCancellable)
            {
                // Create a CancellationTokenSource which may be used to request cancellation
                this.cancelTokenSource = new CancellationTokenSource();
            }
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the name of the operation.
        /// </summary>
        public string OperationName { get; private set; }

        /// <summary>
        /// Gets the progress status.
        /// </summary>
        public string ProgressStatus { get; private set; }

        /// <summary>
        /// Gets or sets the progress value for the task (range is 0 to 1.0).
        /// </summary>
        public double ProgressValue
        {
            get => this.progress;

            set
            {
                this.progress = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ProgressValue)));
            }
        }

        /// <summary>
        /// Gets a token that notifies of cancellation if the window was created with the isCancellable flag set to true.
        /// </summary>
        public CancellationToken CancellationToken => this.cancelTokenSource?.Token ?? CancellationToken.None;

        /// <summary>
        /// Gets a value indicating whether the window is cancellable.
        /// </summary>
        public bool IsCancellable { get; }

        /// <summary>
        /// Gets the progress report interface.
        /// </summary>
        public IProgress<(string, double)> Progress { get; }

        /// <summary>
        /// Runs an action within a modal progress window.
        /// </summary>
        /// <param name="name">The name of the operation.</param>
        /// <param name="action">The action to perform.</param>
        /// <param name="initialStatus">The initial progress status.</param>
        public static void RunWithProgress(string name, Action<IProgress<(string, double)>> action, string initialStatus = "")
        {
            // Run the action (with progress) in a non-cancellable window
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, name, initialStatus, isCancellable: false);
            progressWindow.RunInProgressWindow(() => action(progressWindow.Progress));
        }

        /// <summary>
        /// Runs a cancellable action within a modal progress window.
        /// </summary>
        /// <param name="name">The name of the operation.</param>
        /// <param name="action">The cancellable action to perform.</param>
        /// <param name="initialStatus">The initial progress status.</param>
        public static void RunWithProgress(string name, Action<IProgress<(string, double)>, CancellationToken> action, string initialStatus = "")
        {
            // Run the action (with progress and cancellation) in a cancellable window
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, name, initialStatus, isCancellable: true);
            progressWindow.RunInProgressWindow(() => action(progressWindow.Progress, progressWindow.CancellationToken));
        }

        /// <summary>
        /// Runs a function which returns a value within a modal progress window.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="name">The name of the operation.</param>
        /// <param name="function">The function to run.</param>
        /// <param name="initialStatus">The initial progress status.</param>
        /// <returns>The return value of the function.</returns>
        public static T RunWithProgress<T>(string name, Func<IProgress<(string, double)>, T> function, string initialStatus = "")
        {
            // Run the function (with progress) in a non-cancellable window
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, name, initialStatus, isCancellable: false);
            return progressWindow.RunInProgressWindow(() => function(progressWindow.Progress));
        }

        /// <summary>
        /// Runs a cancellable function which returns a value within a modal progress window.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="name">The name of the operation.</param>
        /// <param name="function">The function to run.</param>
        /// <param name="initialStatus">The initial progress status.</param>
        /// <returns>The return value of the function.</returns>
        public static T RunWithProgress<T>(string name, Func<IProgress<(string, double)>, CancellationToken, T> function, string initialStatus = "")
        {
            // Run the function (with progress and cancellation) in a cancellable window
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, name, initialStatus, isCancellable: true);
            return progressWindow.RunInProgressWindow(() => function(progressWindow.Progress, progressWindow.CancellationToken));
        }

        /// <summary>
        /// Runs a task within a modal progress window.
        /// </summary>
        /// <param name="name">The name of the operation.</param>
        /// <param name="task">The task to run.</param>
        /// <param name="initialStatus">The initial progress status.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task RunWithProgressAsync(string name, Func<IProgress<(string, double)>, Task> task, string initialStatus = "")
        {
            // Run the task (with progress) in a non-cancellable window
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, name, initialStatus, isCancellable: false);
            await progressWindow.RunInProgressWindowAsync(task(progressWindow.Progress));
        }

        /// <summary>
        /// Runs a cancellable task within a modal progress window.
        /// </summary>
        /// <param name="name">The name of the operation.</param>
        /// <param name="task">The task to run.</param>
        /// <param name="initialStatus">The initial progress status.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task RunWithProgressAsync(string name, Func<IProgress<(string, double)>, CancellationToken, Task> task, string initialStatus = "")
        {
            // Run the task (with progress and cancellation) in a cancellable window
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, name, initialStatus, isCancellable: true);
            await progressWindow.RunInProgressWindowAsync(task(progressWindow.Progress, progressWindow.CancellationToken));
        }

        /// <summary>
        /// Runs a task which returns a value within a modal progress window.
        /// </summary>
        /// <typeparam name="T">The return type of the task.</typeparam>
        /// <param name="name">The name of the operation.</param>
        /// <param name="task">The task to run.</param>
        /// <param name="initialStatus">The initial progress status.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<T> RunWithProgressAsync<T>(string name, Func<IProgress<(string, double)>, Task<T>> task, string initialStatus = "")
        {
            // Run the task (with progress) in a non-cancellable window
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, name, initialStatus, isCancellable: false);
            return await progressWindow.RunInProgressWindowAsync(task(progressWindow.Progress));
        }

        /// <summary>
        /// Runs a cancellable task which returns a value within a modal progress window.
        /// </summary>
        /// <typeparam name="T">The return type of the task.</typeparam>
        /// <param name="name">The name of the operation.</param>
        /// <param name="task">The task to run.</param>
        /// <param name="initialStatus">The initial progress status.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<T> RunWithProgressAsync<T>(string name, Func<IProgress<(string, double)>, CancellationToken, Task<T>> task, string initialStatus = "")
        {
            // Run the task (with progress and cancellation) in a cancellable window
            var progressWindow = new ProgressWindow(Application.Current.MainWindow, name, initialStatus, isCancellable: true);
            return await progressWindow.RunInProgressWindowAsync(task(progressWindow.Progress, progressWindow.CancellationToken));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.cancelTokenSource?.Dispose();
        }

        /// <inheritdoc/>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // Since the window doesn't have a title bar, this provides a way to move it by dragging
            this.DragMove();
        }

        private void RunInProgressWindow(Action action)
        {
            // Start the operation in a task and show the progress window
            var task = Task.Run(action);
            this.ShowProgressWindow();

            try
            {
                task.Wait();
            }
            catch (AggregateException ae)
            {
                // Re-throw the inner exception thrown by the Action
                throw ae.InnerException;
            }
        }

        private T RunInProgressWindow<T>(Func<T> func)
        {
            // Start the operation in a task and show the progress window
            var task = Task.Run(func);
            this.ShowProgressWindow();

            try
            {
                return task.Result;
            }
            catch (AggregateException ae)
            {
                // Re-throw the inner exception thrown by the Func
                throw ae.InnerException;
            }
        }

        private async Task RunInProgressWindowAsync(Task task)
        {
            this.ShowProgressWindow();
            await task;
        }

        private async Task<T> RunInProgressWindowAsync<T>(Task<T> task)
        {
            this.ShowProgressWindow();
            return await task;
        }

        private bool? ShowProgressWindow()
        {
            try
            {
                // Show the progress window, which will be automatically closed once the reported progress reaches 1.0
                return this.ShowDialog();
            }
            catch (InvalidOperationException)
            {
                // Window has already been closed so just return the result
                return this.DialogResult;
            }
        }

        private void CloseProgressWindow()
        {
            // close the status window when the task reports completion
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
            {
                this.DialogResult = true;
            }
            else
            {
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // If the cancel button was clicked, request cancellation via the cancellation token
            this.cancelTokenSource.Cancel();
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.ComponentModel;
    using System.Windows;

    /// <summary>
    /// Interaction logic for GetParameterWindow.xaml.
    /// </summary>
    public partial class GetParameterWindow : Window, INotifyPropertyChanged
    {
        private readonly Func<string, (bool IsValid, string Error)> validator;
        private bool isValid = false;
        private string errorMessage = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetParameterWindow"/> class.
        /// </summary>
        /// <param name="owner">The window that owns this window.</param>
        /// <param name="windowTitle">The title of the window.</param>
        /// <param name="parameterName">The initial store name to display.</param>
        /// <param name="initialParameterValue">The initial store path to display.</param>
        /// <param name="validator">An optional validator function.</param>
        public GetParameterWindow(Window owner, string windowTitle, string parameterName, string initialParameterValue, Func<string, (bool IsValid, string Error)> validator = null)
        {
            this.InitializeComponent();

            this.Title = windowTitle;
            this.Owner = owner;
            this.DataContext = this;

            this.ParameterLabel.Content = parameterName + ":";
            this.ParameterValue = initialParameterValue;
            this.validator = validator ?? (value => (true, null));

            this.Validate();
        }

        /// <summary>
        /// Occurs when a property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the parameter value.
        /// </summary>
        public string ParameterValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog values are valid.
        /// </summary>
        public bool IsValid
        {
            get => this.isValid;
            set
            {
                if (this.isValid != value)
                {
                    this.isValid = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsValid)));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string ErrorMessage
        {
            get => this.errorMessage;
            set
            {
                if (this.errorMessage != value)
                {
                    this.errorMessage = value;
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(this.ErrorMessage)));
                    }
                }
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            e.Handled = true;
        }

        private void ValidateFormValues(object sender, RoutedEventArgs e)
        {
            // This method is called whenever the text in one of the textboxes changes
            this.Validate();
            e.Handled = true;
        }

        private void Validate()
        {
            (var isValid, var errorMessage) = this.validator(this.ParameterValue);
            this.IsValid = isValid;
            this.ErrorMessage = string.IsNullOrEmpty(this.ParameterValue) ? string.Empty : errorMessage;
        }
    }
}
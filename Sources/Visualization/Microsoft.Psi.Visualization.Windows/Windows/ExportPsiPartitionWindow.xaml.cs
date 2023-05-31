// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Windows;
    using System.Windows.Forms;
    using Microsoft.Psi.Visualization.Helpers;

    /// <summary>
    /// Interaction logic for ExportPsiPartitionWindow.xaml.
    /// </summary>
    public partial class ExportPsiPartitionWindow : Window, INotifyPropertyChanged
    {
        private bool isValid = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportPsiPartitionWindow"/> class.
        /// </summary>
        /// <param name="initialCropInterval">The initial setting for the interval to crop to.</param>
        /// <param name="initialStoreName">The initial store name to display.</param>
        /// <param name="initialStorePath">The initial store path to display.</param>
        /// <param name="owner">The window that owns this window.</param>
        public ExportPsiPartitionWindow(string initialStoreName, string initialStorePath, TimeInterval initialCropInterval, Window owner)
        {
            this.InitializeComponent();

            this.Owner = owner;
            this.DataContext = this;

            this.StoreName = initialStoreName;
            this.StorePath = initialStorePath;
            this.StartTimeText = DateTimeHelper.FormatDateTime(initialCropInterval.Left);
            this.EndTimeText = DateTimeHelper.FormatDateTime(initialCropInterval.Right);

            this.Validate();
        }

        /// <summary>
        /// Occurs when a property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the store name.
        /// </summary>
        public string StoreName { get; set; }

        /// <summary>
        /// Gets or sets the store path.
        /// </summary>
        public string StorePath { get; set; }

        /// <summary>
        /// Gets or sets a text version of the crop start time.
        /// </summary>
        public string StartTimeText { get; set; }

        /// <summary>
        /// Gets or sets a text version of the crop end time.
        /// </summary>
        public string EndTimeText { get; set; }

        /// <summary>
        /// Gets the selected crop interval.
        /// </summary>
        public TimeInterval CropInterval => new TimeInterval(DateTime.Parse(this.StartTimeText), DateTime.Parse(this.EndTimeText));

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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (PsiStore.Exists(this.StoreName, this.StorePath))
            {
                if (new MessageBoxWindow(
                    this.Owner,
                    "Store Exists",
                    $"A store named {this.StoreName} already exists at the specified output path. Are you sure you want to overwrite it?",
                    "Yes",
                    "No").ShowDialog() != true)
                {
                    e.Handled = true;
                    return;
                }
            }

            this.DialogResult = true;
            e.Handled = true;
        }

        private void StorePathButton_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Please select a folder.";
                dlg.RootFolder = Environment.SpecialFolder.MyComputer;
                dlg.SelectedPath = string.IsNullOrWhiteSpace(this.StorePath) ? string.Empty : this.StorePath;
                dlg.ShowNewFolderButton = true;

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    this.StorePath = dlg.SelectedPath;
                    this.Validate();
                }
            }
        }

        private void ValidateFormValues(object sender, RoutedEventArgs e)
        {
            // This method is called whenever the text in one of the textboxes changes
            this.Validate();
            e.Handled = true;
        }

        private void Validate()
        {
            this.IsValid =
                !string.IsNullOrWhiteSpace(this.StoreName) &&
                !string.IsNullOrWhiteSpace(this.StorePath) &&
                DateTime.TryParse(this.StartTimeText, out DateTime temp) &&
                DateTime.TryParse(this.EndTimeText, out temp) &&
                Directory.Exists(this.StorePath);
        }
    }
}
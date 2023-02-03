// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Forms;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Interaction logic for CreateAnnotationStreamWindow.xaml.
    /// </summary>
    public partial class CreateAnnotationStreamWindow : Window, INotifyPropertyChanged
    {
        private bool isValid = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAnnotationStreamWindow"/> class.
        /// </summary>
        /// <param name="session">The session that the annotation stream should be added to.</param>
        /// <param name="availableAnnotationSchemas">The list of available annotation schemas the user may choose from.</param>
        /// <param name="owner">The window that wons this window.</param>
        public CreateAnnotationStreamWindow(SessionViewModel session, List<AnnotationSchema> availableAnnotationSchemas, Window owner)
        {
            this.InitializeComponent();

            this.AvailablePartitions = session.PartitionViewModels.Where(p => p.IsPsiPartition && !p.IsLivePartition).ToArray();
            this.AvailableAnnotationSchemas = availableAnnotationSchemas;

            // By default, set the store path to the location of the first available partition
            if (this.AvailablePartitions.Any())
            {
                this.StorePath = this.AvailablePartitions.First().StorePath;
            }

            this.Owner = owner;
            this.DataContext = this;

            this.ShowPartitionWarningMessage = this.AvailablePartitions.Count() != session.PartitionViewModels.Count();
            if (this.ShowPartitionWarningMessage)
            {
                if (this.AvailablePartitions.Any())
                {
                    this.PartitionWarningMessage = @"Some partitions are not shown because they are live or are not \psi stores.";
                }
                else
                {
                    // no annotatable existing partitions available
                    this.PartitionWarningMessage = @"No partitions are available because they are live or are not \psi stores.";
                    this.NewPartitionCheckBox.IsChecked = true;
                    this.NewPartitionCheckBox.IsEnabled = false;
                    this.ExistingPartitionCheckBox.IsEnabled = false;
                    this.PartitionNameComboBox.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Occurs when a property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the list of available partitions.
        /// </summary>
        public IEnumerable<PartitionViewModel> AvailablePartitions { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to show a partition warning message.
        /// </summary>
        public bool ShowPartitionWarningMessage { get; private set; }

        /// <summary>
        /// Gets partition warning message.
        /// </summary>
        public string PartitionWarningMessage { get; private set; }

        /// <summary>
        /// Gets the list of available annotation schemas.
        /// </summary>
        public List<AnnotationSchema> AvailableAnnotationSchemas { get; private set; }

        /// <summary>
        /// Gets the selected annotation schema.
        /// </summary>
        public AnnotationSchema SelectedAnnotationSchema => this.AnnotationSchemaComboBox.SelectedItem as AnnotationSchema;

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        public string StreamName => this.StreamNameTextBox.Text;

        /// <summary>
        /// Gets the selected existing partition to create the annotation stream in.
        /// </summary>
        public string ExistingPartitionName => this.PartitionNameComboBox.SelectedIndex > -1 ? (this.PartitionNameComboBox.SelectedItem as PartitionViewModel).Name : string.Empty;

        /// <summary>
        /// Gets the store name.
        /// </summary>
        public string StoreName => this.StoreNameTextBox.Text;

        /// <summary>
        /// Gets or sets the store path.
        /// </summary>
        public string StorePath
        {
            get => this.StorePathTextBox.Text;
            set => this.StorePathTextBox.Text = value;
        }

        /// <summary>
        /// Gets a value indicating whether the annotations stream should be created in an existing partition.
        /// </summary>
        public bool UseExistingPartition => this.ExistingPartitionCheckBox.IsChecked == true;

        /// <summary>
        /// Gets or sets a value indicating whether the annotation is valid.
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
            this.DialogResult = true;
            e.Handled = true;
        }

        private void StorePathButton_Click(object sender, RoutedEventArgs e)
        {
            using var dlg = new FolderBrowserDialog()
            {
                Description = "Please select a folder.",
                RootFolder = Environment.SpecialFolder.MyComputer,
                SelectedPath = string.IsNullOrWhiteSpace(this.StorePathTextBox.Text) ? string.Empty : this.StorePathTextBox.Text,
                ShowNewFolderButton = true,
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.StorePathTextBox.Text = dlg.SelectedPath;
                this.Validate();
            }
        }

        private void ValidateFormValues(object sender, RoutedEventArgs e)
        {
            // This method is called whenever one of the radio buttons
            // is set or the text in one of the textboxes changes
            this.Validate();
            e.Handled = true;
        }

        private void Validate()
        {
            if (this.ExistingPartitionCheckBox.IsChecked.Value == true)
            {
                this.IsValid =
                    !string.IsNullOrWhiteSpace(this.StreamName) &&
                    !string.IsNullOrWhiteSpace(this.ExistingPartitionName) &&
                    this.SelectedAnnotationSchema != default;
            }
            else
            {
                this.IsValid =
                    !string.IsNullOrWhiteSpace(this.StreamName) &&
                    !string.IsNullOrWhiteSpace(this.StoreName) &&
                    Directory.Exists(this.StorePath);
            }
        }
    }
}
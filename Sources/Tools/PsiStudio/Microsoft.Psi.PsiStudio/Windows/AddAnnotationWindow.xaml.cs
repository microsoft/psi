// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using Microsoft.Psi.Extensions.Annotations;

    /// <summary>
    /// Interaction logic for AddAnnotationWindow.xaml
    /// </summary>
    public partial class AddAnnotationWindow : Window, INotifyPropertyChanged
    {
        private bool showStorageProperties;
        private bool isValid;
        private bool annotationNameEditing;
        private bool streamNameUserEdited;
        private bool partitionNameUserEdited;
        private bool storeNameUserEdited;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddAnnotationWindow"/> class.
        /// </summary>
        /// <param name="schemas">The current collection of annotation schemas.</param>
        /// <param name="showStorageProperties">Flag indicating whether to show the storage properties. Default is true.</param>
        public AddAnnotationWindow(ReadOnlyObservableCollection<AnnotationSchema> schemas, bool showStorageProperties = true)
        {
            this.InitializeComponent();

            this.DataContext = schemas;
            this.showStorageProperties = showStorageProperties;
            if (!this.showStorageProperties)
            {
                this.StorageProperties.Visibility = Visibility.Collapsed;
            }

            this.isValid = false;
            this.OKButton.DataContext = this;
        }

        /// <summary>
        /// Occurs when a property has changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the annotation name.
        /// </summary>
        public string AnnotationName => this.AnnotationNameTextBox.Text;

        /// <summary>
        /// Gets the annotation schema.
        /// </summary>
        public AnnotationSchema AnnotationSchema => this.AnnotationSchemaComboBox.SelectedItem as AnnotationSchema;

        /// <summary>
        /// Gets the stream name.
        /// </summary>
        public string StreamName => this.StreamNameTextBox.Text;

        /// <summary>
        /// Gets the partition name.
        /// </summary>
        public string PartitionName => this.PartitionNameTextBox.Text;

        /// <summary>
        /// Gets the store name.
        /// </summary>
        public string StoreName => this.StoreNameTextBox.Text;

        /// <summary>
        /// Gets or sets the store path.
        /// </summary>
        public string StorePath
        {
            get { return this.StorePathTextBox.Text; }
            set { this.StorePathTextBox.Text = value; }
        }

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

        private void StorePathButton_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Please select a folder.";
                dlg.RootFolder = Environment.SpecialFolder.MyComputer;
                dlg.SelectedPath = this.StorePathTextBox.Text;
                dlg.ShowNewFolderButton = true;
                var result = dlg.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    this.StorePathTextBox.Text = dlg.SelectedPath;
                }
            }

            e.Handled = true;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            e.Handled = true;
        }

        private void AnnotationNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.annotationNameEditing = true;

            if (!this.streamNameUserEdited)
            {
                this.StreamNameTextBox.Text = this.AnnotationNameTextBox.Text;
            }

            if (!this.partitionNameUserEdited)
            {
                this.PartitionNameTextBox.Text = this.AnnotationNameTextBox.Text;
            }

            if (!this.storeNameUserEdited)
            {
                this.StoreNameTextBox.Text = this.AnnotationNameTextBox.Text;
            }

            this.annotationNameEditing = false;
            this.Validate();
            e.Handled = true;
        }

        private void StreamNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.annotationNameEditing)
            {
                this.streamNameUserEdited = true;
            }

            this.Validate();
            e.Handled = true;
        }

        private void PartitionNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.annotationNameEditing)
            {
                this.partitionNameUserEdited = true;
            }

            this.Validate();
            e.Handled = true;
        }

        private void StoreNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!this.annotationNameEditing)
            {
                this.storeNameUserEdited = true;
            }

            this.Validate();
            e.Handled = true;
        }

        private void StorePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Validate();
            e.Handled = true;
        }

        private void Validate()
        {
            this.IsValid = !string.IsNullOrWhiteSpace(this.AnnotationName);
            this.IsValid = this.IsValid && (!this.showStorageProperties || !string.IsNullOrWhiteSpace(this.StreamName));
            this.IsValid = this.IsValid && (!this.showStorageProperties || !string.IsNullOrWhiteSpace(this.StoreName));
            this.IsValid = this.IsValid && (!this.showStorageProperties || Directory.Exists(this.StorePath));
        }
    }
}
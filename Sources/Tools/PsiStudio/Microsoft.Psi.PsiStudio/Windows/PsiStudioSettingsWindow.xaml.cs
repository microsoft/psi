// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System.Collections;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.PsiStudio;
    using Xceed.Wpf.Toolkit.PropertyGrid;

    /// <summary>
    /// Interaction logic for PsiStudioSettingsWindow.xaml.
    /// </summary>
    public partial class PsiStudioSettingsWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStudioSettingsWindow"/> class.
        /// </summary>
        /// <param name="owner">The owner of this window.</param>
        public PsiStudioSettingsWindow(Window owner)
        {
            this.InitializeComponent();

            this.Owner = owner;
            this.DataContext = this;

            // There is an issue with the PropertyGrid where validation errors may not be immediately reflected
            // for collection properties (see: https://github.com/xceedsoftware/wpftoolkit/issues/1463). The
            // following is a workaround to force evaluation of the validation state for collection properties.
            this.PropertyGrid.SelectedObjectChanged += (s, e) =>
            {
                foreach (PropertyItem property in this.PropertyGrid.Properties)
                {
                    // Add a handler for collection type properties to ensure that the property source value is
                    // updated so that it may be validated, then set the validation status to reflect the result
                    if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
                    {
                        // Triggered when the property editor loses focus
                        property.LostFocus += (s, e) =>
                        {
                            var binding = property.GetBindingExpression(PropertyItem.ValueProperty);
                            if (binding != null && binding.DataItem is DependencyObject source)
                            {
                                // Update the bound value to give it an opportunity to validate
                                binding.UpdateSource();

                                // Update the validation state of the binding
                                if (Validation.GetHasError(source))
                                {
                                    var errors = Validation.GetErrors(source);
                                    Validation.MarkInvalid(binding, errors[0]);
                                }
                                else
                                {
                                    Validation.ClearInvalid(binding);
                                }
                            }
                        };
                    }
                }
            };
        }

        /// <summary>
        /// Gets or sets the directory to search for layout files.
        /// </summary>
        public PsiStudioSettingsViewModel SettingsViewModel { get; set; }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {
            if (!this.SettingsViewModel.HasErrors)
            {
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                // Display validation errors
                new MessageBoxWindow(
                    this.Owner,
                    "Settings Error",
                    this.SettingsViewModel.Error,
                    cancelButtonText: null).ShowDialog();

                e.Handled = false;
            }
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using Microsoft.Psi.Data.Annotations;
    using Xceed.Wpf.Toolkit.PropertyGrid;
    using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

    /// <summary>
    /// Represents an editor for annotation values.
    /// </summary>
    public class AnnotationValueEditor : ITypeEditor
    {
        /// <inheritdoc/>
        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            FrameworkElement editorControl;
            DependencyProperty bindingProperty;

            if (propertyItem.Instance is TimeIntervalAnnotationDisplayData objectData)
            {
                // Get the schema definition for the property item.
                AnnotationSchemaDefinition schemaDefinition = objectData.Definition.SchemaDefinitions.FirstOrDefault(s => s.Name == propertyItem.DisplayName);

                // If the schema is finite and not readonly, then display the possible values in a combobox, otherwise
                // just display the current value in a readonly textbox.  Non-finite schemas are always displayed
                // in a textbox since there are infinite vallues possible.
                if (!propertyItem.IsReadOnly && schemaDefinition.Schema.IsFiniteAnnotationSchema)
                {
                    // Create the combobox and load it with the schema values
                    ComboBox comboBox = new ComboBox();
                    Type schemaType = schemaDefinition.Schema.GetType();
                    MethodInfo valuesProperty = schemaType.GetProperty("Values").GetGetMethod();
                    comboBox.ItemsSource = (IEnumerable)valuesProperty.Invoke(schemaDefinition.Schema, new object[] { });
                    comboBox.SelectedItem = propertyItem.Value;

                    editorControl = comboBox;
                    bindingProperty = ComboBox.SelectedItemProperty;
                }
                else
                {
                    // create the textbox and optionally make it readonly
                    TextBox textBox = new TextBox();
                    textBox.IsReadOnly = propertyItem.IsReadOnly;

                    editorControl = textBox;
                    bindingProperty = TextBox.TextProperty;
                }

                // Bind the editor control to the property item's value property.
                Binding binding = new Binding(nameof(PropertyItem.Value));
                binding.Source = propertyItem;
                binding.ValidatesOnExceptions = true;
                binding.ValidatesOnDataErrors = true;
                binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                BindingOperations.SetBinding(editorControl, bindingProperty, binding);

                return editorControl;
            }
            else
            {
                throw new ArgumentException($"{nameof(propertyItem)} argument must be a {nameof(TimeIntervalAnnotationDisplayData)}");
            }
        }
    }
}

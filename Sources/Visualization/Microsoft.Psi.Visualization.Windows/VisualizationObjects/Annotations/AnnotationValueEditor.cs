// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Linq;
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
                // Get the attribute schema
                var attributeSchema = objectData.AnnotationSchema.AttributeSchemas.FirstOrDefault(s => s.Name == propertyItem.DisplayName);

                // If the schema is finite and not readonly, then display the possible values in a combobox, otherwise
                // just display the current value in a readonly textbox.  Non-finite schemas are always displayed
                // in a textbox since there are infinite vallues possible.
                if (!propertyItem.IsReadOnly && attributeSchema.ValueSchema is IEnumerableAnnotationValueSchema finiteAnnotationValueSchema)
                {
                    // Create the combobox and load it with the schema values
                    var comboBox = new ComboBox
                    {
                        ItemsSource = finiteAnnotationValueSchema.GetPossibleAnnotationValues(),
                        SelectedItem = propertyItem.Value,
                    };

                    editorControl = comboBox;
                    bindingProperty = ComboBox.SelectedItemProperty;
                }
                else
                {
                    // create the textbox and optionally make it readonly
                    var textBox = new TextBox
                    {
                        IsReadOnly = propertyItem.IsReadOnly,
                    };

                    editorControl = textBox;
                    bindingProperty = TextBox.TextProperty;
                }

                // Bind the editor control to the property item's value property.
                var binding = new Binding(nameof(PropertyItem.Value))
                {
                    Source = propertyItem,
                    ValidatesOnExceptions = true,
                    ValidatesOnDataErrors = true,
                    Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                };

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

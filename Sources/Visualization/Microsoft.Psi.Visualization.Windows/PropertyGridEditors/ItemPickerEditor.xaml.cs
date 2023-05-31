// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.PropertyGridEditors
{
    using System.Windows;
    using System.Windows.Controls;
    using Xceed.Wpf.Toolkit.PropertyGrid;
    using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

    /// <summary>
    /// Interaction logic for ItemPickerEditor.xaml.
    /// </summary>
    public partial class ItemPickerEditor : UserControl, ITypeEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemPickerEditor"/> class.
        /// </summary>
        public ItemPickerEditor()
        {
            this.InitializeComponent();
        }

        /// <inheritdoc/>
        public FrameworkElement ResolveEditor(PropertyItem propertyItem)
        {
            // Set the editor's data context to the ItemSelectorViewModel property
            this.DataContext = propertyItem.Value;
            return this;
        }
    }
}
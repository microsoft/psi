// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.PropertyGridEditors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Provides a view model for selecting a value from an enumeration of items.
    /// </summary>
    /// <typeparam name="T">The type of the items.</typeparam>
    /// <remarks>
    /// Add a property of this type to display a dropdown list picker in the PropertyGrid which supports
    /// selecting one of an enumeration of items of type <typeparamref name="T"/>. The property should be
    /// decorated with the <see cref="System.ComponentModel.EditorAttribute"/> specifying an editor type
    /// of <see cref="ItemPickerEditor"/>.
    /// </remarks>
    public class ItemPickerViewModel<T> : ObservableObject
    {
        private IEnumerable<T> items = default;
        private T selectedItem = default;
        private Func<T, string> displayNameSelector = o => o.ToString();

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemPickerViewModel{T}"/> class.
        /// </summary>
        /// <param name="displayNameSelector">A function to extract the unique display name from each list item.</param>
        public ItemPickerViewModel(Func<T, string> displayNameSelector = null)
        {
            this.displayNameSelector = displayNameSelector ?? (o => o.ToString());
        }

        /// <summary>
        /// Gets or sets the enumeration of items.
        /// </summary>
        [DataMember]
        public IEnumerable<T> Items
        {
            get { return this.items; }

            set
            {
                this.Set(nameof(this.Items), ref this.items, value);

                // Save the current selection before raising property changed (which may cause SelectedItem to be cleared)
                string currentSelection = this.SelectedItem is not null ? this.displayNameSelector(this.SelectedItem) : null;
                this.RaisePropertyChanged(nameof(this.ItemDisplayNames));

                // Re-bind SelectedItem to the first item in the new list with the same display name (or the first item if currentSelection is null)
                this.SelectedItem = currentSelection is not null ?
                    this.Items.FirstOrDefault(value => this.displayNameSelector(value) == currentSelection) :
                    this.Items.FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the display names for the enumeration of items. Display names should be unique.
        /// </summary>
        public System.Collections.IEnumerable ItemDisplayNames
            => this.items?.Select(item => new { DisplayName = this.displayNameSelector(item), Value = item });

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        [DataMember]
        public T SelectedItem
        {
            get { return this.selectedItem; }
            set { this.Set(nameof(this.SelectedItem), ref this.selectedItem, value); }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Base abstract class for implementing interval visualization objects based on a ModelVisual3D view.
    /// </summary>
    /// <typeparam name="TVisualizationObject">The type of the basic visualization object for a single item.</typeparam>
    /// <typeparam name="TData">The underlying data being visualized.</typeparam>
    public abstract class ModelVisual3DIntervalVisualizationObject<TVisualizationObject, TData> : ModelVisual3DIntervalVisualizationObject<TData>
        where TVisualizationObject : ModelVisual3DValueVisualizationObject<TData>, IModelVisual3DVisualizationObject, ICustomTypeDescriptor, new()
    {
        // The list of properties of the prototype (including the prototype's children)
        // that have changed since this collection was created.  After we create a new
        // child TVisualizationObject we'll copy these changed properties into it before we add it.
        private readonly Dictionary<string, object> updatedPrototypeProperties = new ();

        private readonly List<TVisualizationObject> children = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelVisual3DIntervalVisualizationObject{TVisualizationObject, TData}"/> class.
        /// </summary>
        public ModelVisual3DIntervalVisualizationObject()
            : base()
        {
            // Create the prototype child visualization object and register for property changed notifications from it.
            this.Prototype = new TVisualizationObject();
            this.Prototype.RegisterChildPropertyChangedNotifications(this, nameof(this.Prototype));

            // Create the list of combined property descriptors (for this collection class and the child prototype)
            this.GenerateCustomPropertyDescriptors();
        }

        /// <summary>
        /// Gets the prototype child visualization object.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public TVisualizationObject Prototype { get; private set; }

        /// <summary>
        /// Updates the visual 3D elements for this visualization object.
        /// </summary>
        public override void UpdateVisual3D()
        {
            if (this.Data != null)
            {
                int index = 0;
                foreach (var message in this.Data)
                {
                    // If we don't have enough visualization objects, create a new one
                    while (index >= this.ModelVisual3D.Children.Count)
                    {
                        this.AddNew();
                    }

                    // Get the child visualization object to update itself
                    this.children[index].SetCurrentValue(message);

                    index++;
                }

                // If we have more visualization objects than data, remove the extras
                while (index < this.ModelVisual3D.Children.Count)
                {
                    this.Remove(index);
                }
            }
            else
            {
                // No data, remove everything
                this.RemoveAll();
            }
        }

        /// <inheritdoc/>
        public override void OnChildPropertyChanged(string path, object value)
        {
            if (this.children != null)
            {
                // Get the first property path segment
                ModelVisual3DVisualizationObjectsHelper.GetNextPropertyPathSegment(path, out string nextSegment, out string pathRemainder);

                // If this is a property on our prototype, set the property on all our child visualization objects.
                if (nextSegment == nameof(this.Prototype))
                {
                    // Set the new property value on all of the children
                    foreach (TVisualizationObject item in this.children)
                    {
                        item.SetDescendantProperty(pathRemainder, value);
                    }

                    // Squirrel away the property change so that it can be
                    // applied to any new children we create later on.
                    this.updatedPrototypeProperties[pathRemainder] = value;
                }
            }

            base.OnChildPropertyChanged(path, value);
        }

        /// <summary>
        /// Called when a property other than CurrentValue changes.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        public override void NotifyPropertyChanged(string propertyName)
        {
            // Properties in prototype do not appear in the property browser
            // if they have the same name as a property in this parent collection
            // class.  This applies to all public properties that were defined
            // high up in the hierarchy.  Of these hidden properties, the only
            // one we want to propagate to the children is the visible property
            // and setting it on the prototype will cause it to automatically
            // be propagated to the children.
            if (propertyName == nameof(this.Visible))
            {
                this.Prototype.Visible = this.Visible;
            }
        }

        /// <summary>
        /// Generates custom property descriptors.
        /// </summary>
        protected override void GenerateCustomPropertyDescriptors()
        {
            // Usually this method is called any time a visualization object becomes the child of another
            // visualization object, but in the case of collection classes we always need to make this call
            // in order to combine to combine the collection of properties in this collection class with
            // the collection of properties in the prototype.  If this collection subsequently becomes a
            // child visualization object this method will be called again, so we first test if the custom
            // property descriptors collection already exists before doing any work.
            if (this.CustomPropertyDescriptors == null)
            {
                // Get the list of properties in this collection class
                var localPropertyDescriptors = TypeDescriptor.GetProperties(this, true);

                // Create the combined properties collection
                this.CustomPropertyDescriptors = new PropertyDescriptorCollection(null);

                // Copy in all the local properties
                ModelVisual3DVisualizationObjectsHelper.CopyPropertyDescriptors(
                    localPropertyDescriptors,
                    this.CustomPropertyDescriptors);

                // Copy in all the properties of the child (properties that already exist will not be copied)
                ModelVisual3DVisualizationObjectsHelper.CopyPropertyDescriptors(
                    ModelVisual3DVisualizationObjectsHelper.GetChildPropertyDescriptors(this.Prototype), this.CustomPropertyDescriptors);
            }
        }

        private void AddNew()
        {
            // Create the new object
            var visualizationObject = new TVisualizationObject();

            // Copy all of the prototype's updated properties into the new object.
            foreach (var property in this.updatedPrototypeProperties)
            {
                ModelVisual3DVisualizationObjectsHelper.CopyPropertyValue(visualizationObject, property.Key, property.Value);
            }

            // Add it to the collection
            this.children.Add(visualizationObject);

            // Ad the new visualization object's model view as a child of our model view
            this.ModelVisual3D.Children.Add(visualizationObject.ModelVisual3D);
        }

        private void Remove(int index)
        {
            // Remove the visualization object's model view from our model view
            this.ModelVisual3D.Children.RemoveAt(index);

            // remove the visualization object
            this.children.RemoveAt(index);
        }

        private void RemoveAll()
        {
            this.ModelVisual3D.Children.Clear();
            this.children.Clear();
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents the base class for collections of 3D model visualization objects.
    /// </summary>
    /// <typeparam name="TVisualizationObject">The type of visualization objects in the collection.</typeparam>
    /// <typeparam name="TData">The type of data being represented.</typeparam>
    public abstract class ModelVisual3DCollectionVisualizationObject<TVisualizationObject, TData> :
        ModelVisual3DVisualizationObject<TData>
        where TVisualizationObject : VisualizationObject, IModelVisual3DVisualizationObject, ICustomTypeDescriptor, new()
    {
        // The list of properties of the prototype (including the prototype's children)
        // that have changed since this collection was created.  After we create a new
        // child TVisualizationObject we'll copy these changed properties into it before we add it.
        private readonly Dictionary<string, object> updatedPrototypeProperties = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelVisual3DCollectionVisualizationObject{TVisObj, TData}"/> class.
        /// </summary>
        public ModelVisual3DCollectionVisualizationObject()
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
        /// Gets or sets the collection of child visualization objects.
        /// </summary>
        protected IEnumerable<TVisualizationObject> Items { get;  set; }

        /// <inheritdoc/>
        public override void OnChildPropertyChanged(string path, object value)
        {
            if (this.Items != null)
            {
                // Get the first property path segment
                this.GetNextPropertyPathSegment(path, out string nextSegment, out string pathRemainder);

                // If this is a property on our prototype, set the property on all our child visualization objects.
                if (nextSegment == nameof(this.Prototype))
                {
                    // Set the new property value on all of the children
                    foreach (TVisualizationObject item in this.Items)
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

        /// <inheritdoc/>
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
        /// Creates a new created TVisObj and then copies the values of all properties
        /// of the prototype (and its children) that have changed into the new object.
        /// </summary>
        /// <returns>The newly created TVisObj.</returns>
        protected TVisualizationObject CreateNew()
        {
            // Create the new object
            var visualizationObject = new TVisualizationObject();

            // Copy all of the prototype's updated properties into the new object.
            foreach (var property in this.updatedPrototypeProperties)
            {
                this.CopyPropertyValue(visualizationObject, property.Key, property.Value);
            }

            return visualizationObject;
        }

        /// <inheritdoc/>
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
                PropertyDescriptorCollection localPropertyDescriptors = TypeDescriptor.GetProperties(this, true);

                // Get the list of properties in the prototype child visualization object
                PropertyDescriptorCollection childPropertyDescriptors = this.Prototype.GetProperties();

                // Create the combined properties collection
                this.CustomPropertyDescriptors = new PropertyDescriptorCollection(null);

                // Copy in all the local properties
                this.CopyPropertyDescriptors(localPropertyDescriptors, this.CustomPropertyDescriptors);

                // Copy in all the properties of the child (properties that already exist will not be copied)
                this.CopyPropertyDescriptors(this.ConvertToChildPropertyDescriptors(childPropertyDescriptors), this.CustomPropertyDescriptors);
            }
        }

        private void CopyPropertyValue(object destination, string path, object value)
        {
            // Get the next segment in the path to the property
            this.GetNextPropertyPathSegment(path, out string nextSegment, out string pathRemainder);

            // Get the property info for the next segment in the path
            var propertyInfo = destination.GetType().GetProperty(nextSegment);

            // If we're at the end of the path, set the value of
            // the property, otherwise drill into the child object
            if (string.IsNullOrWhiteSpace(pathRemainder))
            {
                if (propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(destination, value);
                }
            }
            else
            {
                this.CopyPropertyValue(propertyInfo.GetValue(destination), pathRemainder, value);
            }
        }

        private PropertyDescriptorCollection ConvertToChildPropertyDescriptors(PropertyDescriptorCollection properties)
        {
            // Creates a set of property descriptors for the child object
            var newProps = new PropertyDescriptorCollection(null);
            foreach (PropertyDescriptor propertyDescriptor in properties)
            {
                newProps.Add(new ChildVisualizationObjectPropertyDescriptor(this.Prototype, propertyDescriptor));
            }

            return newProps;
        }

        private void CopyPropertyDescriptors(PropertyDescriptorCollection source, PropertyDescriptorCollection target)
        {
            foreach (PropertyDescriptor propertyDescriptor in source)
            {
                // Do not add the property descriptor if it's already in the collection
                if (target.Find(propertyDescriptor.Name, false) == null)
                {
                    target.Add(propertyDescriptor);
                }
            }
        }
    }
}

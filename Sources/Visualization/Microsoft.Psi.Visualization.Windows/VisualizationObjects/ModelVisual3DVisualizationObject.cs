// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Represents a visualization object for a ModelVisual3D view.
    /// </summary>
    /// <typeparam name="TData">The underlying data being visualized.</typeparam>
    public abstract class ModelVisual3DVisualizationObject<TData> : XYZValueVisualizationObject<TData>, IModelVisual3DVisualizationObject, ICustomTypeDescriptor
    {
        // The list of properties that should not appear in the property browser if
        // this visualization object is the child of another visualization object.
        private readonly List<string> hiddenChildProperties = new ()
        {
            nameof(CursorEpsilonNegMs),
            nameof(CursorEpsilonPosMs),
            nameof(Name),
            nameof(PartitionName),
            nameof(SourceStreamName),
            nameof(StreamAdapterTypeDisplayString),
            nameof(SummarizerTypeDisplayString),
        };

        // The time elapsed while updating the data
        private string dataUpdateMilliseconds;

        // The parent of this visualization object (if any)
        private IModelVisual3DVisualizationObject parent = null;

        // The name of this component according to this component's parent
        private string componentName = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelVisual3DVisualizationObject{TData}"/> class.
        /// </summary>
        public ModelVisual3DVisualizationObject()
        {
            this.Visual3D = new ModelVisual3D();
        }

        /// <summary>
        /// Gets the time elapsed by the visualizer while updating data.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(true)]
        [DisplayName("Data Update Milliseconds")]
        [Description("The time elapsed by this visualizer while updating data.")]
        public string DataUpdateMilliseconds
        {
            get => this.dataUpdateMilliseconds;
            private set => this.Set(nameof(this.DataUpdateMilliseconds), ref this.dataUpdateMilliseconds, value);
        }

        /// <summary>
        /// Gets the model view of this visualization object.
        /// </summary>
        [Browsable(false)]
        public ModelVisual3D ModelView => this.Visual3D as ModelVisual3D;

        /// <summary>
        /// Gets or sets the collection of custom property descriptors for this visualization object.
        /// If this visualization object is made the child of another visualization object, then
        /// this collection will be created and will contain all of this visualization object's
        /// properties except those that should not be displayed for child objects (CursorEpsilonMs etc).
        /// </summary>
        protected PropertyDescriptorCollection CustomPropertyDescriptors { get; set; }

        /// <inheritdoc/>
        public void RegisterChildPropertyChangedNotifications(IModelVisual3DVisualizationObject parent, string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
            {
                throw new ArgumentException(nameof(childName));
            }

            // Set the parent and the component name
            this.parent = parent ?? throw new ArgumentException(nameof(parent));
            this.componentName = childName;

            // Generate the custom properties
            this.GenerateCustomPropertyDescriptors();

            // Remove the properties that should be hidden for a child visualization object.
            this.RemoveHiddenChildProperties();
        }

        /// <summary>
        /// Called when the current stream data has changed.
        /// </summary>
        public abstract void UpdateData();

        /// <summary>
        /// Called when a property other than CurrentValue changes.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        public abstract void NotifyPropertyChanged(string propertyName);

        /// <inheritdoc/>
        public virtual void OnChildPropertyChanged(string path, object value)
        {
            // If this component has a parent component, prepend this
            // component's name to the path and bubble the call up.
            if (this.parent != null)
            {
                this.parent.OnChildPropertyChanged(this.componentName + "." + path, value);
            }
        }

        /// <inheritdoc/>
        public void SetDescendantProperty(string path, object newValue)
        {
            // Get the next object in the property path
            this.GetNextPropertyPathSegment(path, out string nextSegment, out string remainder);

            // Get the property to set
            var property = this.GetType().GetProperty(nextSegment);

            // If we're at the end of the path, we're at the property and we can
            // set it, otherwise we still need to drill further into the hierarchy
            if (string.IsNullOrEmpty(remainder))
            {
                if (property.CanWrite)
                {
                    property.SetValue(this, newValue);
                }
            }
            else
            {
                if (property.GetValue(this) is IModelVisual3DVisualizationObject childVisualizationObject)
                {
                    childVisualizationObject.SetDescendantProperty(remainder, newValue);
                }
            }
        }

        #region ICustomTypeDescriptor

        /// <inheritdoc/>
        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        /// <inheritdoc/>
        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        /// <inheritdoc/>
        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        /// <inheritdoc/>
        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        /// <inheritdoc/>
        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        /// <inheritdoc/>
        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        /// <inheritdoc/>
        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        /// <inheritdoc/>
        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        /// <inheritdoc/>
        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        /// <inheritdoc/>
        public PropertyDescriptorCollection GetProperties()
        {
            if (this.CustomPropertyDescriptors != null)
            {
                return this.CustomPropertyDescriptors;
            }
            else
            {
                return TypeDescriptor.GetProperties(this, true);
            }
        }

        /// <inheritdoc/>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(this, attributes, true);
        }

        /// <inheritdoc/>
        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion // ICustomTypeDescriptor

        /// <inheritdoc/>
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.CurrentValue))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var startTime = DateTime.UtcNow;
                    this.UpdateData();
                    this.DataUpdateMilliseconds = $"{(DateTime.UtcNow - startTime).TotalMilliseconds:0.00}";
                });
            }
            else
            {
                // Notify of the changed property
                this.NotifyPropertyChanged(e.PropertyName);

                // Get the new value of the property that changed
                object newValue = TypeDescriptor.GetProperties(this)[e.PropertyName].GetValue(this);

                // If the property that changed is a value type or a string,
                // bubble the property change event up to the parent.
                if ((newValue is ValueType) || (newValue is string))
                {
                    this.OnChildPropertyChanged(e.PropertyName, newValue);
                }
            }

            base.OnPropertyChanged(sender, e);
        }

        /// <summary>
        /// Updates the visibility of a child visual 3D.
        /// </summary>
        /// <param name="child">The child visual 3D whose visibility should be updated.</param>
        /// <param name="visible">True if the child should be visible, otherwise false.</param>
        protected void UpdateChildVisibility(Visual3D child, bool visible)
        {
            if (visible)
            {
                if (!this.ModelView.Children.Contains(child))
                {
                    this.ModelView.Children.Add(child);
                }
            }
            else
            {
                if (this.ModelView.Children.Contains(child))
                {
                    this.ModelView.Children.Remove(child);
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="Message{T}"/> struct suitable for passing to a child <see cref="ModelVisual3DVisualizationObject{TData}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the new message.</typeparam>
        /// <param name="data">The new message data.</param>
        /// <returns>A newly created message using the envelope of the current value.</returns>
        protected Message<T>? SynthesizeMessage<T>(T data)
        {
            if (!this.CurrentValue.HasValue || this.CurrentValue.Value == null)
            {
                return null;
            }
            else
            {
                return new Message<T>(data, this.CurrentValue.Value.OriginatingTime, this.CurrentValue.Value.CreationTime, this.CurrentValue.Value.SourceId, this.CurrentValue.Value.SequenceId);
            }
        }

        /// <summary>
        /// Gets the next path segment from a property path.
        /// </summary>
        /// <param name="path">The path to get the next segment from.</param>
        /// <param name="nextSegment">The next path segment.</param>
        /// <param name="pathRemainder">The remainder of the path after the next segment was removed.</param>
        protected void GetNextPropertyPathSegment(string path, out string nextSegment, out string pathRemainder)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(nameof(path));
            }

            List<string> segments = path.Split('.').ToList();
            nextSegment = segments[0];
            segments.RemoveAt(0);
            pathRemainder = string.Join(".", segments.ToArray());
        }

        /// <summary>
        /// Generates the list of custom property descriptors for this visualization object.  If
        /// this visualization object becomes a child of another visualization object then this
        /// collection will be modified by removing certain properties that are not appropriate
        /// for child visualization objects such as CursorEpsilonMs, InterpolationStyle etc.
        /// </summary>
        protected virtual void GenerateCustomPropertyDescriptors()
        {
            this.CustomPropertyDescriptors = TypeDescriptor.GetProperties(this);
        }

        /// <summary>
        /// Removes properties that should be hidden in child visualization objects from the properties collection.
        /// </summary>
        private void RemoveHiddenChildProperties()
        {
            // TypeDeescriptor.GetProperties returns a readonly collection, so we need to copy
            // the properties to a new collection, skipping those properties we wish to hide.
            var updatedPropertyDescriptors = new PropertyDescriptorCollection(null);

            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(this))
            {
                if (!this.hiddenChildProperties.Contains(propertyDescriptor.Name))
                {
                    updatedPropertyDescriptors.Add(propertyDescriptor);
                }
            }

            this.CustomPropertyDescriptors = updatedPropertyDescriptors;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Data;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a single annotation instance in a <see cref="TimeIntervalAnnotationVisualizationObject"/>.
    /// </summary>
    public class TimeIntervalAnnotationDisplayData : ObservableObject, ICustomTypeDescriptor
    {
        private TimeIntervalAnnotationVisualizationObject parent;
        private bool isSelected = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationDisplayData"/> class.
        /// </summary>
        /// <param name="parent">The annotations visualization object that owns this display data instance.</param>
        /// <param name="annotation">The annotation event.</param>
        /// <param name="definition">The annotation definition.</param>
        public TimeIntervalAnnotationDisplayData(TimeIntervalAnnotationVisualizationObject parent, Message<TimeIntervalAnnotation> annotation, AnnotationDefinition definition)
        {
            this.parent = parent;
            this.Annotation = annotation;
            this.Definition = definition;
        }

        /// <summary>
        /// Gets the annotation object.
        /// </summary>
        [Browsable(false)]
        public Message<TimeIntervalAnnotation> Annotation { get; private set; }

        /// <summary>
        /// Gets the annotation schema definiton.
        /// </summary>
        [Browsable(false)]
        public AnnotationDefinition Definition { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the annotation is the currently selected one.
        /// </summary>
        [Browsable(false)]
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.Set(nameof(this.IsSelected), ref this.isSelected, value);
        }

        /// <summary>
        /// Gets the annotation type.
        /// </summary>
        [PropertyOrder(0)]
        public string AnnotationType => this.Definition.Name;

        /// <summary>
        /// Gets the start time of the annotation.
        /// </summary>
        [PropertyOrder(1)]
        public DateTime StartTime => this.Annotation.Data.Interval.Left;

        /// <summary>
        /// Gets the end time of the annotation.
        /// </summary>
        [PropertyOrder(2)]
        public DateTime EndTime => this.Annotation.Data.Interval.Right;

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
            List<PropertyDescriptor> propertyDescriptors = new List<PropertyDescriptor>();

            // Add the AnnotationType, StartTime, and EndTime properties.
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(this, true);
            propertyDescriptors.Add(pdc.Find(nameof(this.AnnotationType), false));
            propertyDescriptors.Add(pdc.Find(nameof(this.StartTime), false));
            propertyDescriptors.Add(pdc.Find(nameof(this.EndTime), false));

            // Then add a property for each value track in the annotation.  All of these
            // properties will use the annotation value editor for editing.
            int propertyOrder = 3;
            Attribute editorAttribute = new EditorAttribute(typeof(AnnotationValueEditor), typeof(AnnotationValueEditor));
            foreach (AnnotationSchemaDefinition schemaDefinition in this.Definition.SchemaDefinitions)
            {
                propertyDescriptors.Add(new AnnotationPropertyDescriptor(this, schemaDefinition.Name, !this.parent.EnableAnnotationValueEdit, typeof(object), new Attribute[] { new PropertyOrderAttribute(propertyOrder++), editorAttribute }));
            }

            return new PropertyDescriptorCollection(propertyDescriptors.ToArray());
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

        /// <summary>
        /// Gets a value in the annotation.
        /// </summary>
        /// <param name="valueName">The name of the value in the annotation to get.</param>
        /// <returns>The value.</returns>
        public object GetValue(string valueName)
        {
            return this.Annotation.Data.Values[valueName];
        }

        /// <summary>
        /// Sets a value in the annotation.
        /// </summary>
        /// <param name="valueName">The name of the value in the annotation to set.</param>
        /// <param name="value">The new value.</param>
        public void SetValue(string valueName, object value)
        {
            this.parent.SetAnnotationValue(this.Annotation, valueName, value);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Data.Annotations;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a single annotation instance in a <see cref="TimeIntervalAnnotationVisualizationObject"/>.
    /// </summary>
    public class TimeIntervalAnnotationDisplayData : ObservableObject, ICustomTypeDescriptor
    {
        private readonly TimeIntervalAnnotationVisualizationObject parent;
        private readonly Message<TimeIntervalAnnotationSet> annotationSetMessage;
        private bool isSelected = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotationDisplayData"/> class.
        /// </summary>
        /// <param name="parent">The annotations visualization object that owns this display data instance.</param>
        /// <param name="annotationSetMessage">The annotation set message.</param>
        /// <param name="track">The track name.</param>
        /// <param name="trackIndex">The track index.</param>
        /// <param name="annotationSchema">The annotation schema.</param>
        public TimeIntervalAnnotationDisplayData(
            TimeIntervalAnnotationVisualizationObject parent,
            Message<TimeIntervalAnnotationSet> annotationSetMessage,
            string track,
            int trackIndex,
            AnnotationSchema annotationSchema)
        {
            this.parent = parent;
            this.annotationSetMessage = annotationSetMessage;
            this.Track = track;
            this.TrackIndex = trackIndex;
            this.AnnotationSchema = annotationSchema;
        }

        /// <summary>
        /// Gets the annotation.
        /// </summary>
        [Browsable(false)]
        public TimeIntervalAnnotation Annotation => this.annotationSetMessage.Data[this.Track];

        /// <summary>
        /// Gets the track name for the annotation.
        /// </summary>
        public string Track { get; }

        /// <summary>
        /// Gets the track index for the annotation.
        /// </summary>
        public int TrackIndex { get; }

        /// <summary>
        /// Gets the annotation schema.
        /// </summary>
        [Browsable(false)]
        public AnnotationSchema AnnotationSchema { get; }

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
        /// Gets the annotation schema name .
        /// </summary>
        [PropertyOrder(0)]
        [DisplayName("Annotation Schema")]
        [Description("The name of the annotation schema")]
        public string AnnotationSchemaName => this.AnnotationSchema.Name;

        /// <summary>
        /// Gets the start time of the annotation.
        /// </summary>
        [PropertyOrder(1)]
        [DisplayName("Start Time")]
        public DateTime StartTime => this.Annotation.Interval.Left;

        /// <summary>
        /// Gets the end time of the annotation.
        /// </summary>
        [PropertyOrder(2)]
        [DisplayName("End Time")]
        public DateTime EndTime => this.Annotation.Interval.Right;

        #region ICustomTypeDescriptor

        /// <inheritdoc/>
        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);

        /// <inheritdoc/>
        public string GetClassName() => TypeDescriptor.GetClassName(this, true);

        /// <inheritdoc/>
        public string GetComponentName() => TypeDescriptor.GetComponentName(this, true);

        /// <inheritdoc/>
        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);

        /// <inheritdoc/>
        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);

        /// <inheritdoc/>
        public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);

        /// <inheritdoc/>
        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);

        /// <inheritdoc/>
        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);

        /// <inheritdoc/>
        public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);

        /// <inheritdoc/>
        public PropertyDescriptorCollection GetProperties()
        {
            var propertyDescriptors = new List<PropertyDescriptor>();

            // Add the AnnotationType, StartTime, and EndTime properties.
            var propertyDescriptorCollection = TypeDescriptor.GetProperties(this, true);
            propertyDescriptors.Add(propertyDescriptorCollection.Find(nameof(this.AnnotationSchemaName), false));
            propertyDescriptors.Add(propertyDescriptorCollection.Find(nameof(this.StartTime), false));
            propertyDescriptors.Add(propertyDescriptorCollection.Find(nameof(this.EndTime), false));

            // Then add a property for each value track in the annotation.  All of these
            // properties will use the annotation value editor for editing.
            int propertyOrder = 3;
            var editorAttribute = new EditorAttribute(typeof(AnnotationValueEditor), typeof(AnnotationValueEditor));
            foreach (var attributeSchema in this.AnnotationSchema.AttributeSchemas)
            {
                propertyDescriptors.Add(new AnnotationPropertyDescriptor(
                    this,
                    attributeSchema.Name,
                    !this.parent.AllowEditAnnotationValue,
                    typeof(object),
                    new Attribute[] { new PropertyOrderAttribute(propertyOrder++), editorAttribute }));
            }

            return new PropertyDescriptorCollection(propertyDescriptors.ToArray());
        }

        /// <inheritdoc/>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes) => TypeDescriptor.GetProperties(this, attributes, true);

        /// <inheritdoc/>
        public object GetPropertyOwner(PropertyDescriptor pd) => this;

        #endregion // ICustomTypeDescriptor

        /// <summary>
        /// Gets the value for a specified attribute.
        /// </summary>
        /// <param name="attribute">The name of the attribute to get the value for.</param>
        /// <returns>The value for the specified attribute.</returns>
        public object GetAttributeValue(string attribute) => this.Annotation.AttributeValues[attribute];

        /// <summary>
        /// Sets the value for a specified attribute.
        /// </summary>
        /// <param name="attribute">The name of the attribute to set the value for.</param>
        /// <param name="annotationValue">The new value.</param>
        public void SetAttributeValue(string attribute, IAnnotationValue annotationValue)
        {
            // Update the value in the annotation
            this.annotationSetMessage.Data[this.Track].AttributeValues[attribute] = annotationValue;

            // Call on the parent visualization object to update the annotations
            this.parent.UpdateAnnotationSetMessage(this.annotationSetMessage);
        }
    }
}

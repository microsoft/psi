// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Represents a property descriptor for an annotation.
    /// </summary>
    public class AnnotationPropertyDescriptor : PropertyDescriptor
    {
        private readonly TimeIntervalAnnotationDisplayData annotationDisplayData;
        private readonly Type propertyType;
        private readonly bool isReadOnly;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationPropertyDescriptor"/> class.
        /// </summary>
        /// <param name="annotationDisplayData">The display data for the annotation that contains the property for this property descriptor.</param>
        /// <param name="propertyName">The name of the property in the annotation represented by this property descriptor.</param>
        /// <param name="isReadOnly">Specifies whether the property may be edited.</param>
        /// <param name="propertyType">The type of the property in this property descriptor.</param>
        /// <param name="propertyAttributes">A collection of attributes that apply to this property descriptor.</param>
        public AnnotationPropertyDescriptor(TimeIntervalAnnotationDisplayData annotationDisplayData, string propertyName, bool isReadOnly, Type propertyType, Attribute[] propertyAttributes)
            : base(propertyName, propertyAttributes)
        {
            this.annotationDisplayData = annotationDisplayData;
            this.isReadOnly = isReadOnly;
            this.propertyType = propertyType;
        }

        /// <inheritdoc/>
        public override Type ComponentType => typeof(TimeIntervalAnnotationDisplayData);

        /// <inheritdoc/>
        public override bool IsReadOnly => this.isReadOnly;

        /// <inheritdoc/>
        public override Type PropertyType => this.propertyType;

        /// <inheritdoc/>
        public override bool CanResetValue(object component)
        {
            return !this.isReadOnly;
        }

        /// <inheritdoc/>
        public override object GetValue(object component)
        {
            return this.annotationDisplayData.GetAttributeValue(this.Name);
        }

        /// <inheritdoc/>
        public override void ResetValue(object component)
        {
        }

        /// <inheritdoc/>
        public override void SetValue(object component, object value)
        {
            this.annotationDisplayData.SetAttributeValue(this.Name, (IAnnotationValue)value);
        }

        /// <inheritdoc/>
        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Represents a property descriptor for child visualization object.
    /// </summary>
    public class ChildVisualizationObjectPropertyDescriptor : PropertyDescriptor
    {
        private readonly object owner;
        private readonly PropertyDescriptor descriptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChildVisualizationObjectPropertyDescriptor"/> class.
        /// </summary>
        /// <param name="owner">The visualization object that this property descriptor belongs to.</param>
        /// <param name="descriptor">The property's property descriptor.</param>
        public ChildVisualizationObjectPropertyDescriptor(object owner, PropertyDescriptor descriptor)
            : base(descriptor)
        {
            this.owner = owner;
            this.descriptor = descriptor;
        }

        /// <inheritdoc/>
        public override Type ComponentType => this.descriptor.ComponentType;

        /// <inheritdoc/>
        public override bool IsReadOnly => this.descriptor.IsReadOnly;

        /// <inheritdoc/>
        public override Type PropertyType => this.descriptor.PropertyType;

        /// <inheritdoc/>
        public override bool CanResetValue(object component)
        {
            return this.descriptor.CanResetValue(this.owner);
        }

        /// <inheritdoc/>
        public override object GetValue(object component)
        {
            return this.descriptor.GetValue(this.owner);
        }

        /// <inheritdoc/>
        public override void ResetValue(object component)
        {
            this.descriptor.ResetValue(this.owner);
        }

        /// <inheritdoc/>
        public override void SetValue(object component, object value)
        {
            this.descriptor.SetValue(this.owner, value);
        }

        /// <inheritdoc/>
        public override bool ShouldSerializeValue(object component)
        {
            return this.descriptor.ShouldSerializeValue(this.owner);
        }
    }
}

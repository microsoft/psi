// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents an axis for a visualization panel.
    /// </summary>
    public class Axis : ObservableObject, IEquatable<Axis>
    {
        private const double DefaultMinimum = 0.0d;
        private const double DefaultMaximum = 1.0d;

        /// <summary>
        /// The maximum value of the axis.
        /// </summary>
        [DataMember]
        private double maximum;

        /// <summary>
        /// The minimum value of the axis.
        /// </summary>
        [DataMember]
        private double minimum;

        /// <summary>
        /// Initializes a new instance of the <see cref="Axis"/> class.
        /// </summary>
        public Axis()
        {
            this.SetDefaultRange();
        }

        /// <summary>
        /// Gets or sets the maximum value.  Setting this property will also change the axis compute mode to manual.
        /// </summary>
        [IgnoreDataMember] // property has side effects so serialize its backing field instead
        [DisplayName("Maximum")]
        [Description("The maximum value of the axis.")]
        public double Maximum
        {
            get => this.maximum;

            set
            {
                this.Set(nameof(this.Maximum), ref this.maximum, value);
                this.RaisePropertyChanged(nameof(this.Range));
            }
        }

        /// <summary>
        /// Gets or sets the minimum value.  Setting this property will also change the axis compute mode to manual.
        /// </summary>
        [IgnoreDataMember] // property has side effects so serialize its backing field instead
        [DisplayName("Minimum")]
        [Description("The minimum value of the axis.")]
        public double Minimum
        {
            get => this.minimum;
            set
            {
                this.Set(nameof(this.Minimum), ref this.minimum, value);
                this.RaisePropertyChanged(nameof(this.Range));
            }
        }

        /// <summary>
        /// Gets the range of the axis.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public ValueRange<double> Range => new (this.Minimum, this.Maximum);

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="first">The first object to compare.</param>
        /// <param name="second">The second object to compare.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public static bool operator ==(Axis first, Axis second)
        {
            return first.minimum == second.minimum && first.maximum == second.maximum;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="first">The first object to compare.</param>
        /// <param name="second">The second object to compare.</param>
        /// <returns>True if the objects are equal, otherwise false.</returns>
        public static bool operator !=(Axis first, Axis second)
        {
            return !(first == second);
        }

        /// <inheritdoc/>
        public bool Equals(Axis other)
        {
            return this == other;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Axis))
            {
                return false;
            }

            return this == obj as Axis;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.minimum.GetHashCode() ^ this.maximum.GetHashCode();
        }

        /// <summary>
        /// Sets the range of the axis.
        /// </summary>
        /// <param name="minimum">The minimum value on the axis.</param>
        /// <param name="maximum">The maximum value on the axis.</param>
        public void SetRange(double minimum, double maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
            this.RaisePropertyChanged(nameof(this.Minimum));
            this.RaisePropertyChanged(nameof(this.Maximum));
            this.RaisePropertyChanged(nameof(this.Range));
        }

        /// <summary>
        /// Moves the upper and lower bound of the range by a specified amount.
        /// </summary>
        /// <param name="translateDistance">The distance to move both the minimum and maximim values.</param>
        public void TranslateRange(double translateDistance)
        {
            this.minimum += translateDistance;
            this.maximum += translateDistance;
            this.RaisePropertyChanged(nameof(this.Minimum));
            this.RaisePropertyChanged(nameof(this.Maximum));
            this.RaisePropertyChanged(nameof(this.Range));
        }

        /// <summary>
        /// Sets the default range of the axis.
        /// </summary>
        public void SetDefaultRange()
        {
            this.SetRange(DefaultMinimum, DefaultMaximum);
        }
    }
}

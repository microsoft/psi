// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents an axis for a visualization panel.
    /// </summary>
    public class Axis : ObservableObject
    {
        private const double DefaultMinimum = 0.0d;
        private const double DefaultMaximum = 1.0d;

        /// <summary>
        /// The compute mode for the axis.
        /// </summary>
        private AxisComputeMode axisComputeMode = AxisComputeMode.Auto;

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
            this.AxisComputeMode = AxisComputeMode.Auto;
            this.SetDefaultRange();
        }

        /// <summary>
        /// Gets or sets the axis compute mode.
        /// </summary>
        [DataMember]
        [DisplayName("Axis Compute Mode")]
        [Description("Specifies whether the axis is computed automatically or set manually.")]
        public AxisComputeMode AxisComputeMode
        {
            get { return this.axisComputeMode; }

            set
            {
                this.Set(nameof(this.AxisComputeMode), ref this.axisComputeMode, value);
            }
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
                this.AxisComputeMode = AxisComputeMode.Manual;
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
                this.AxisComputeMode = AxisComputeMode.Manual;
                this.Set(nameof(this.Minimum), ref this.minimum, value);
                this.RaisePropertyChanged(nameof(this.Range));
            }
        }

        /// <summary>
        /// Gets the range of the axis.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public ValueRange<double> Range => new ValueRange<double>(this.Minimum, this.Maximum);

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
        /// Sets the default range of the axis.
        /// </summary>
        public void SetDefaultRange()
        {
            this.SetRange(DefaultMinimum, DefaultMaximum);
        }
    }
}

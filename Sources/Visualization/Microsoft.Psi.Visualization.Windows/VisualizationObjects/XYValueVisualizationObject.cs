// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Provides an abstract base class for stream value visualization objects that show two dimensional, cartesian data.
    /// </summary>
    /// <typeparam name="TData">The type of the stream data.</typeparam>
    [VisualizationPanelType(VisualizationPanelType.XY)]
    public abstract class XYValueVisualizationObject<TData> : StreamValueVisualizationObject<TData>, IXValueRangeProvider, IYValueRangeProvider
    {
        #pragma warning disable SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

        /// <summary>
        /// The X value range of the current data.
        /// </summary>
        private ValueRange<double> xValueRange = null;

        /// <summary>
        /// The Y value range of the current data.
        /// </summary>
        private ValueRange<double> yValueRange = null;

        #pragma warning restore SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

        /// <inheritdoc/>
        public event EventHandler<EventArgs> XValueRangeChanged;

        /// <inheritdoc/>
        public event EventHandler<EventArgs> YValueRangeChanged;

        /// <summary>
        /// Gets the X axis.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public Axis XAxis => (this.Panel as XYVisualizationPanel)?.XAxis;

        /// <summary>
        /// Gets the Y axis.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public Axis YAxis => (this.Panel as XYVisualizationPanel)?.YAxis;

        #pragma warning disable SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

        /// <summary>
        /// Gets the current mouse position within the image (in image pixels).
        /// </summary>
        [IgnoreDataMember]
        [DisplayName("Mouse Position")]
        [Description("The position of the mouse in the visualization object.")]
        public string MousePositionString => (this.Panel is XYVisualizationPanel xYVisualizationPanel) ? xYVisualizationPanel.MousePositionString : default;

        #pragma warning restore SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

        /// <inheritdoc/>
        [IgnoreDataMember]
        [Browsable(false)]
        public virtual ValueRange<double> XValueRange
        {
            get => this.xValueRange;

            protected set
            {
                this.xValueRange = value;
                this.XValueRangeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        [IgnoreDataMember]
        [Browsable(false)]
        public virtual ValueRange<double> YValueRange
        {
            get => this.yValueRange;

            protected set
            {
                this.yValueRange = value;
                this.YValueRangeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Called when the x value range in a derived class has changed.
        /// </summary>
        protected void OnXValueRangeChanged()
        {
            this.XValueRangeChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the y value range in a derived class has changed.
        /// </summary>
        protected void OnYValueRangeChanged()
        {
            this.YValueRangeChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        protected override void OnPanelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(XYVisualizationPanel.MousePositionString))
            {
                this.RaisePropertyChanged(nameof(this.MousePositionString));
            }
            else if (e.PropertyName == nameof(XYVisualizationPanel.XAxis))
            {
                this.RaisePropertyChanged(nameof(this.XAxis));
            }
            else if (e.PropertyName == nameof(XYVisualizationPanel.YAxis))
            {
                this.RaisePropertyChanged(nameof(this.YAxis));
            }

            base.OnPanelPropertyChanged(sender, e);
        }
    }
}

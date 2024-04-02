// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Provides an abstract base class for stream interval visualization objects that show two dimensional, cartesian data.
    /// </summary>
    /// <typeparam name="TData">The type of the stream data.</typeparam>
    [VisualizationPanelType(VisualizationPanelType.XY)]
    public abstract class XYIntervalVisualizationObject<TData> : StreamIntervalVisualizationObject<TData>, IXValueRangeProvider, IYValueRangeProvider
    {
        /// <summary>
        /// The X value range of the current data.
        /// </summary>
        private ValueRange<double> xValueRange = null;

        /// <summary>
        /// The Y value range of the current data.
        /// </summary>
        private ValueRange<double> yValueRange = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYIntervalVisualizationObject{TData}"/> class.
        /// </summary>
        public XYIntervalVisualizationObject()
            : base()
        {
            this.VisualizationInterval = VisualizationInterval.CursorEpsilon;
        }

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

        /// <summary>
        /// Gets the current mouse position within the image (in image pixels).
        /// </summary>
        [IgnoreDataMember]
        [DisplayName("Mouse Position")]
        [Description("The position of the mouse in the visualization object.")]
        public string MousePositionString => (this.Panel is XYVisualizationPanel xYVisualizationPanel) ? xYVisualizationPanel.MousePositionString : default;

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

        /// <inheritdoc />
        protected override void OnDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // When collection changes, update the axis if in auto mode
            base.OnDataCollectionChanged(e);
            this.ComputeXYValueRange();
        }

        /// <inheritdoc />
        protected override void OnSummaryDataCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // When collection changes, update the axis if in auto mode
            base.OnSummaryDataCollectionChanged(e);
            this.ComputeXYValueRange();
        }

        /// <summary>
        /// Computes the X and Y value ranges.
        /// </summary>
        protected virtual void ComputeXYValueRange()
        {
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

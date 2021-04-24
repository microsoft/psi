// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a visualization panel that 2D visualizers can be rendered in.
    /// </summary>
    public class XYVisualizationPanel : VisualizationPanel
    {
        private Axis xAxis = new Axis();
        private Axis yAxis = new Axis();

        private int relativeWidth = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYVisualizationPanel"/> class.
        /// </summary>
        public XYVisualizationPanel()
        {
            this.Name = "2D Panel";
            this.xAxis.PropertyChanged += this.OnXAxisPropertyChanged;
            this.yAxis.PropertyChanged += this.OnYAxisPropertyChanged;
        }

        /// <summary>
        /// Gets or sets the X Axis for the panel.
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [DisplayName("X Axis")]
        [Description("The X axis for the visualization panel.")]
        [ExpandableObject]
        public Axis XAxis
        {
            get { return this.xAxis; }
            set { this.Set(nameof(this.XAxis), ref this.xAxis, value); }
        }

        /// <summary>
        /// Gets or sets the Y Axis for the panel.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("Y Axis")]
        [Description("The Y axis for the visualization panel.")]
        [ExpandableObject]
        public Axis YAxis
        {
            get { return this.yAxis; }
            set { this.Set(nameof(this.YAxis), ref this.yAxis, value); }
        }

        /// <summary>
        /// Gets or sets the name of the relative width for the panel.
        /// </summary>
        [DataMember]
        [PropertyOrder(4)]
        [Description("The relative width for the panel.")]
        public int RelativeWidth
        {
            get { return this.relativeWidth; }
            set { this.Set(nameof(this.RelativeWidth), ref this.relativeWidth, value); }
        }

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new List<VisualizationPanelType>() { VisualizationPanelType.XY };

        /// <inheritdoc/>
        protected override void OnVisualizationObjectXValueRangeChanged(object sender, EventArgs e)
        {
            this.CalculateXAxisRange();
            base.OnVisualizationObjectXValueRangeChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectYValueRangeChanged(object sender, EventArgs e)
        {
            this.CalculateYAxisRange();
            base.OnVisualizationObjectYValueRangeChanged(sender, e);
        }

        /// <summary>
        /// Called when a property of the X axis has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnXAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Axis.AxisComputeMode))
            {
                this.CalculateXAxisRange();
            }
            else if (e.PropertyName == nameof(Axis.Range))
            {
                this.RaisePropertyChanged(nameof(this.XAxis));
            }
        }

        /// <summary>
        /// Called when a property of the Y axis has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnYAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Axis.AxisComputeMode))
            {
                this.CalculateYAxisRange();
            }
            else if (e.PropertyName == nameof(Axis.Range))
            {
                this.RaisePropertyChanged(nameof(this.YAxis));
            }
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(XYVisualizationPanelView));
        }

        private void CalculateXAxisRange()
        {
            // If the x axis is in auto mode, then recalculate the x axis range
            if (this.XAxis.AxisComputeMode == AxisComputeMode.Auto)
            {
                // Get the X value range of all visualization objects that are X value range providers and have a non-null X value range
                var xValueRanges = this.VisualizationObjects
                    .Where(vo => vo is IXValueRangeProvider)
                    .Select(vo => vo as IXValueRangeProvider)
                    .Select(vrp => vrp.XValueRange)
                    .Where(vr => vr != null);

                // Set the X axis range to cover all of the axis ranges of the visualization objects.
                // If no visualization object reported a range, then use the default instead.
                if (xValueRanges.Any())
                {
                    this.XAxis.SetRange(xValueRanges.Min(ar => ar.Minimum), xValueRanges.Max(ar => ar.Maximum));
                }
                else
                {
                    this.XAxis.SetDefaultRange();
                }
            }
        }

        private void CalculateYAxisRange()
        {
            // If the y axis is in auto mode, then recalculate the y axis range
            if (this.YAxis.AxisComputeMode == AxisComputeMode.Auto)
            {
                // Get the Y value range of all visualization objects that are Y value range providers and have a non-null Y value range
                var yValueRanges = this.VisualizationObjects
                    .Where(vo => vo is IYValueRangeProvider)
                    .Select(vo => vo as IYValueRangeProvider)
                    .Select(vrp => vrp.YValueRange)
                    .Where(vr => vr != null);

                // Set the Y axis range to cover all of the axis ranges of the visualization objects.
                // If no visualization object reported a range, then use the default instead.
                if (yValueRanges.Any())
                {
                    this.YAxis.SetRange(yValueRanges.Min(ar => ar.Minimum), yValueRanges.Max(ar => ar.Maximum));
                }
                else
                {
                    this.YAxis.SetDefaultRange();
                }
            }
        }
    }
}
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
    using System.Windows.Input;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a visualization panel that 2D visualizers can be rendered in.
    /// </summary>
    public class XYVisualizationPanel : InstantVisualizationPanel
    {
        /// <summary>
        /// The scale factor used when zooming into or out of the panel with the mouse wheel.
        /// </summary>
        private const double ZoomFactor = 1.1d;

        private Axis xAxis = new ();
        private Axis yAxis = new ();

        private Axis xAxisPropertyBrowser = new ();
        private Axis yAxisPropertyBrowser = new ();

        private Point mousePosition = new (0, 0);
        private AxisComputeMode axisComputeMode = AxisComputeMode.Auto;

        // The current dimensions of the viewport window within which the child visualization objects are displayed.
        private double viewportWidth;
        private double viewportHeight;

        // The padding which defines the active display area of the panel.
        private Thickness viewportPadding;

        private RelayCommand<RoutedEventArgs> viewportLoadedCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonUpCommand;
        private RelayCommand<MouseWheelEventArgs> mouseWheelCommand;
        private RelayCommand<MouseEventArgs> mouseMoveCommand;
        private RelayCommand mouseEnterCommand;
        private RelayCommand mouseLeaveCommand;
        private RelayCommand setAutoAxisComputeModeCommand;
        private RelayCommand<SizeChangedEventArgs> viewportSizeChangedCommand;

        private Point mouseRButtonDownPosition;
        private bool isDraggingAxes = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="XYVisualizationPanel"/> class.
        /// </summary>
        public XYVisualizationPanel()
        {
            this.Name = "2D Panel";

            this.XAxis.PropertyChanged += this.OnXAxisPropertyChanged;
            this.YAxis.PropertyChanged += this.OnYAxisPropertyChanged;

            this.XAxisPropertyBrowser.PropertyChanged += this.XAxisPropertyBrowser_PropertyChanged;
            this.YAxisPropertyBrowser.PropertyChanged += this.YAxisPropertyBrowser_PropertyChanged;
        }

        /// <summary>
        /// Gets or sets the axis compute mode.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [DisplayName("Axis Compute Mode")]
        [Description("Specifies whether the axes are computed automatically or set manually.")]
        public AxisComputeMode AxisComputeMode
        {
            get { return this.axisComputeMode; }

            set
            {
                if (this.axisComputeMode != value)
                {
                    this.Set(nameof(this.AxisComputeMode), ref this.axisComputeMode, value);
                    if (value == AxisComputeMode.Auto)
                    {
                        this.CalculateAxisRangesAuto();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the X Axis for the panel.
        /// </summary>
        [DataMember]
        [Browsable(false)]
        public Axis XAxis
        {
            get => this.xAxis;
            set => this.Set(nameof(this.XAxis), ref this.xAxis, value);
        }

        /// <summary>
        /// Gets or sets the property browser visible version of the X Axis for the panel.
        /// </summary>
        [IgnoreDataMember]
        [PropertyOrder(4)]
        [ExpandableObject]
        [DisplayName("X Axis")]
        [Description("Specifies the extents of the X axis.")]
        public Axis XAxisPropertyBrowser
        {
            get => this.xAxisPropertyBrowser;
            set => this.Set(nameof(this.XAxisPropertyBrowser), ref this.xAxisPropertyBrowser, value);
        }

        /// <summary>
        /// Gets or sets the Y Axis for the panel.
        /// </summary>
        [DataMember]
        [Browsable(false)]
        public Axis YAxis
        {
            get => this.yAxis;
            set => this.Set(nameof(this.YAxis), ref this.yAxis, value);
        }

        /// <summary>
        /// Gets or sets the property browser visible version of the X Axis for the panel.
        /// </summary>
        [IgnoreDataMember]
        [PropertyOrder(5)]
        [ExpandableObject]
        [DisplayName("Y Axis")]
        [Description("Specifies the extents of the Y axis.")]
        public Axis YAxisPropertyBrowser
        {
            get => this.yAxisPropertyBrowser;
            set => this.Set(nameof(this.YAxisPropertyBrowser), ref this.yAxisPropertyBrowser, value);
        }

        /// <summary>
        /// Gets or sets the padding of the items control within the panel.
        /// </summary>
        [DataMember]
        [Browsable(false)]
        public Thickness ViewportPadding
        {
            get => this.viewportPadding;
            set => this.Set(nameof(this.ViewportPadding), ref this.viewportPadding, value);
        }

        /// <summary>
        /// Gets a string version of the mouse position.
        /// </summary>
        [IgnoreDataMember]
        [PropertyOrder(6)]
        [DisplayName("Mouse Position")]
        [Description("The position of the mouse within the XY Visualization panel.")]
        public string MousePositionString => $"{this.mousePosition.X:F},{this.mousePosition.Y:F}";

        /// <summary>
        /// Gets or sets the mouse position in the panel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Point MousePosition
        {
            get { return this.mousePosition; }

            set
            {
                this.Set(nameof(this.MousePosition), ref this.mousePosition, value);
                this.RaisePropertyChanged(nameof(this.MousePositionString));
            }
        }

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new () { VisualizationPanelType.XY };

        /// <summary>
        /// Gets the mouse right button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<RoutedEventArgs> ViewportLoadedCommand
            => this.viewportLoadedCommand ??= new RelayCommand<RoutedEventArgs>(
                e =>
                {
                    // Event source is the viewport
                    var viewport = e.Source as FrameworkElement;

                    // Initialize the display area
                    this.viewportWidth = viewport.ActualWidth;
                    this.viewportHeight = viewport.ActualHeight;
                    this.ZoomToDisplayArea();
                });

        /// <summary>
        /// Gets the mouse wheel command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseWheelEventArgs> MouseWheelCommand
            => this.mouseWheelCommand ??= new RelayCommand<MouseWheelEventArgs>(
                e =>
                {
                    if (e.Delta != 0)
                    {
                        // Event source is the items control Grid element
                        var itemsControl = e.Source as FrameworkElement;

                        // Get the current mouse location in the items control
                        var mouseLocation = Mouse.GetPosition(itemsControl);

                        // Get the current X Axis and Y Axis logical dimensions
                        double xAxisLogicalWidth = this.XAxis.Maximum - this.XAxis.Minimum;
                        double yAxisLogicalHeight = this.YAxis.Maximum - this.YAxis.Minimum;

                        // Zoom in our out if there is a non-zero mouse delta
                        if (e.Delta > 0)
                        {
                            xAxisLogicalWidth /= ZoomFactor;
                            yAxisLogicalHeight /= ZoomFactor;
                        }
                        else
                        {
                            xAxisLogicalWidth *= ZoomFactor;
                            yAxisLogicalHeight *= ZoomFactor;
                        }

                        // Calculate the new minimum X and Y logical values of the axes such
                        // that the mouse will still be above the same point in the 2D image
                        double xAxisLogicalMinimum = this.MousePosition.X - mouseLocation.X * xAxisLogicalWidth / itemsControl.ActualWidth;
                        double yAxisLogicalMinimum = this.MousePosition.Y - mouseLocation.Y * yAxisLogicalHeight / itemsControl.ActualHeight;

                        // Switch to manual axis compute mode
                        this.AxisComputeMode = AxisComputeMode.Manual;

                        this.XAxis.SetRange(xAxisLogicalMinimum, xAxisLogicalMinimum + xAxisLogicalWidth);
                        this.YAxis.SetRange(yAxisLogicalMinimum, yAxisLogicalMinimum + yAxisLogicalHeight);
                    }

                    e.Handled = true;
                });

        /// <summary>
        /// Gets the mouse right button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseButtonEventArgs> MouseRightButtonDownCommand
            => this.mouseRightButtonDownCommand ??= new RelayCommand<MouseButtonEventArgs>(e => this.mouseRButtonDownPosition = Mouse.GetPosition(e.Source as FrameworkElement));

        /// <summary>
        /// Gets the mouse right button up command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseButtonEventArgs> MouseRightButtonUpCommand
            => this.mouseRightButtonUpCommand ??= new RelayCommand<MouseButtonEventArgs>(
                e =>
                {
                    if (this.isDraggingAxes)
                    {
                        this.isDraggingAxes = false;

                        // Prevent the context menu from displaying
                        e.Handled = true;
                    }
                });

        /// <summary>
        /// Gets the mouse move command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseEventArgs> MouseMoveCommand
            => this.mouseMoveCommand ??= new RelayCommand<MouseEventArgs>(
                e =>
                {
                    // Event source is the items control Grid element
                    var itemsControl = e.Source as FrameworkElement;

                    // Get the current mouse position
                    var newMousePosition = e.GetPosition(itemsControl);

                    // Get the current scale factor between the axes logical bounds and the items control size.
                    double scaleX = (this.XAxis.Maximum - this.XAxis.Minimum) / itemsControl.ActualWidth;
                    double scaleY = (this.YAxis.Maximum - this.YAxis.Minimum) / itemsControl.ActualHeight;

                    // Set the mouse position in locical/image co-ordinates
                    this.MousePosition = new Point(newMousePosition.X * scaleX + this.XAxis.Minimum, newMousePosition.Y * scaleY + this.YAxis.Minimum);

                    // If the right mouse button is pressed, the user is attempting a drag
                    if (e.RightButton == MouseButtonState.Pressed)
                    {
                        this.isDraggingAxes = true;

                        // Determine how far the mouse moved in logical/image coordinates
                        double xDelta = (newMousePosition.X - this.mouseRButtonDownPosition.X) * scaleX;
                        double yDelta = (newMousePosition.Y - this.mouseRButtonDownPosition.Y) * scaleY;

                        // Switch to auto axis compute mode
                        this.AxisComputeMode = AxisComputeMode.Manual;

                        // Move the display area bounds by the same logical distance the mouse moved.
                        this.XAxis.TranslateRange(-xDelta);
                        this.YAxis.TranslateRange(-yDelta);

                        // Remember the current mouse position
                        this.mouseRButtonDownPosition = newMousePosition;

                        e.Handled = true;
                    }
                    else
                    {
                        this.isDraggingAxes = false;
                    }
                });

        /// <summary>
        /// Gets the mouse enter command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MouseEnterCommand
            => this.mouseEnterCommand ??= new RelayCommand(() => this.isDraggingAxes = false);

        /// <summary>
        /// Gets the mouse leave command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand MouseLeaveCommand
            => this.mouseLeaveCommand ??= new RelayCommand(() => this.isDraggingAxes = false);

        /// <summary>
        /// Gets the items control size changed command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<SizeChangedEventArgs> ViewportSizeChangedCommand
            => this.viewportSizeChangedCommand ??= new RelayCommand<SizeChangedEventArgs>(e => this.OnViewportSizeChanged(e));

        /// <summary>
        /// Gets the set auto axis compute mode command for both the X and Y axes.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SetAutoAxisComputeModeCommand
            => this.setAutoAxisComputeModeCommand ??= new RelayCommand(() => this.AxisComputeMode = AxisComputeMode.Auto);

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var items = new List<ContextMenuItemInfo>()
            {
                new ContextMenuItemInfo(
                    null,
                    "Auto-Fit Axes",
                    this.SetAutoAxisComputeModeCommand,
                    isEnabled: this.AxisComputeMode == AxisComputeMode.Manual),
            };

            items.AddRange(base.ContextMenuItemsInfo());
            return items;
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectXValueRangeChanged(object sender, EventArgs e)
        {
            if (this.AxisComputeMode == AxisComputeMode.Auto)
            {
                this.CalculateAxisRangesAuto();
            }

            base.OnVisualizationObjectXValueRangeChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnVisualizationObjectYValueRangeChanged(object sender, EventArgs e)
        {
            if (this.AxisComputeMode == AxisComputeMode.Auto)
            {
                this.CalculateAxisRangesAuto();
            }

            base.OnVisualizationObjectYValueRangeChanged(sender, e);
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(XYVisualizationPanelView));
        }

        /// <summary>
        /// Called when a property of the X axis has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        protected virtual void OnXAxisPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Axis.Range))
            {
                if (this.XAxis != this.XAxisPropertyBrowser)
                {
                    this.XAxisPropertyBrowser.SetRange(this.XAxis.Minimum, this.XAxis.Maximum);
                }

                this.ZoomToDisplayArea();

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
            if (e.PropertyName == nameof(Axis.Range))
            {
                if (this.YAxis != this.YAxisPropertyBrowser)
                {
                    this.YAxisPropertyBrowser.SetRange(this.YAxis.Minimum, this.YAxis.Maximum);
                }

                this.ZoomToDisplayArea();

                this.RaisePropertyChanged(nameof(this.YAxis));
            }
        }

        private void XAxisPropertyBrowser_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.XAxisPropertyBrowser != this.XAxis)
            {
                this.AxisComputeMode = AxisComputeMode.Manual;
                this.XAxis.SetRange(this.XAxisPropertyBrowser.Minimum, this.XAxisPropertyBrowser.Maximum);
            }
        }

        private void YAxisPropertyBrowser_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.YAxisPropertyBrowser != this.YAxis)
            {
                this.AxisComputeMode = AxisComputeMode.Manual;
                this.YAxis.SetRange(this.YAxisPropertyBrowser.Minimum, this.YAxisPropertyBrowser.Maximum);
            }
        }

        private void OnViewportSizeChanged(SizeChangedEventArgs e)
        {
            this.viewportWidth = e.NewSize.Width;
            this.viewportHeight = e.NewSize.Height;
            this.ZoomToDisplayArea();
        }

        private void CalculateAxisRangesAuto()
        {
            // Get the X value range of all visualization objects that are X value range providers and have a non-null X value range
            var xValueRanges = this.VisualizationObjects
                .Where(vo => vo is IXValueRangeProvider)
                .Select(vo => vo as IXValueRangeProvider)
                .Select(vrp => vrp.XValueRange)
                .Where(vr => vr != null);

            // Get the Y value range of all visualization objects that are Y value range providers and have a non-null Y value range
            var yValueRanges = this.VisualizationObjects
                .Where(vo => vo is IYValueRangeProvider)
                .Select(vo => vo as IYValueRangeProvider)
                .Select(vrp => vrp.YValueRange)
                .Where(vr => vr != null);

            // An X and Y value range must have been set by at least one of the visualization objects.
            if (xValueRanges.Any() && yValueRanges.Any())
            {
                // Calculate the overall X value range and Y value range of all VOs
                (double min, double max) xValueRange = (xValueRanges.Min(ar => ar.Minimum), xValueRanges.Max(ar => ar.Maximum));
                (double min, double max) yValueRange = (yValueRanges.Min(ar => ar.Minimum), yValueRanges.Max(ar => ar.Maximum));

                this.XAxis.SetRange(xValueRange.min, xValueRange.max);
                this.YAxis.SetRange(yValueRange.min, yValueRange.max);
            }
            else
            {
                this.XAxis.SetDefaultRange();
                this.YAxis.SetDefaultRange();
            }

            this.ZoomToDisplayArea();
        }

        private void ZoomToDisplayArea()
        {
            // Calculate the aspect ratios of the value ranges and the items control size
            double valueRangeAspectRatio = (this.XAxis.Maximum - this.XAxis.Minimum) / (this.YAxis.Maximum - this.YAxis.Minimum);
            double viewportAspectRatio = this.viewportWidth / this.viewportHeight;

            if (valueRangeAspectRatio > viewportAspectRatio)
            {
                // Add padding the the Y axis min/max to maintain the aspect ratio
                double scaleFactor = this.viewportWidth / (this.XAxis.Maximum - this.XAxis.Minimum);
                double padding = (this.viewportHeight - (this.YAxis.Maximum - this.YAxis.Minimum) * scaleFactor) / 2.0d;
                this.ViewportPadding = new Thickness(0, padding, 0, padding);
            }
            else
            {
                // Add padding the the X axis min/max to maintain the aspect ratio
                double scaleFactor = this.viewportHeight / (this.YAxis.Maximum - this.YAxis.Minimum);
                double padding = (this.viewportWidth - (this.XAxis.Maximum - this.XAxis.Minimum) * scaleFactor) / 2.0d;
                this.ViewportPadding = new Thickness(padding, 0, padding, 0);
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (this.XAxisPropertyBrowser != this.XAxis)
            {
                this.XAxisPropertyBrowser.SetRange(this.XAxis.Minimum, this.XAxis.Maximum);
            }

            if (this.YAxisPropertyBrowser != this.YAxis)
            {
                this.YAxisPropertyBrowser.SetRange(this.YAxis.Minimum, this.YAxis.Maximum);
            }

            this.ZoomToDisplayArea();
        }
    }
}
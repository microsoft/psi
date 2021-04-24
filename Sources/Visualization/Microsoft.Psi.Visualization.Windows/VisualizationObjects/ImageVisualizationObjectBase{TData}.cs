// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1305 // Field names must not use Hungarian notation (yMax, yMin, etc.).

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents an image visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the image visualization object.</typeparam>
    [VisualizationPanelType(VisualizationPanelType.XY)]
    public abstract class ImageVisualizationObjectBase<TData> : StreamValueVisualizationObject<TData>, IXValueRangeProvider, IYValueRangeProvider
    {
        /// <summary>
        /// Indicates whether we will flip the image horizontally.
        /// </summary>
        private bool horizontalFlip = false;

        /// <summary>
        /// Indicates whether we are stretching to fit.
        /// </summary>
        private bool stretchToFit = true;

        /// <summary>
        /// The image resolution.
        /// </summary>
        private Size resolution = default;

        /// <summary>
        /// The position of the mouse within the image.
        /// </summary>
        private Point mousePosition = new Point(0, 0);

        /// <inheritdoc/>
        public event EventHandler<EventArgs> XValueRangeChanged;

        /// <inheritdoc/>
        public event EventHandler<EventArgs> YValueRangeChanged;

        /// <summary>
        /// Gets or sets a value indicating whether we will flip the image horizontally.
        /// </summary>
        [DataMember]
        public bool HorizontalFlip
        {
            get { return this.horizontalFlip; }
            set { this.Set(nameof(this.HorizontalFlip), ref this.horizontalFlip, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether we are stretching to fit.
        /// </summary>
        [DataMember]
        public bool StretchToFit
        {
            get { return this.stretchToFit; }
            set { this.Set(nameof(this.StretchToFit), ref this.stretchToFit, value); }
        }

        /// <summary>
        /// Gets the current mouse position within the image (in image pixels).
        /// </summary>
        [IgnoreDataMember]
        [DisplayName("Mouse Position")]
        [Description("The position of the mouse over the image, in pixel space.")]
        public Point MousePosition
        {
            get { return this.mousePosition; }
            private set { this.Set(nameof(this.MousePosition), ref this.mousePosition, value); }
        }

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
        /// Gets or sets the image resolution.
        /// </summary>
        [IgnoreDataMember]
        [DisplayName("Resolution")]
        [Description("The image resolution in pixels.")]
        public Size Resolution
        {
            get => this.resolution;

            protected set
            {
                if (this.resolution != value)
                {
                    double previousWidth = this.resolution.Width;
                    double previousHeight = this.resolution.Height;

                    // Update the resolution
                    this.Set(nameof(this.Resolution), ref this.resolution, value);

                    // If the resolution width or height changed, raise the appropriate change events
                    if (previousWidth != this.resolution.Width)
                    {
                        this.XValueRangeChanged?.Invoke(this, EventArgs.Empty);
                    }

                    if (previousHeight != this.resolution.Height)
                    {
                        this.YValueRangeChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        /// <inheritdoc/>
        [IgnoreDataMember]
        [Browsable(false)]
        public ValueRange<double> XValueRange => this.Resolution.Width > 0 ? new ValueRange<double>(0, this.Resolution.Width) : null;

        /// <inheritdoc/>
        [IgnoreDataMember]
        [Browsable(false)]
        public ValueRange<double> YValueRange => this.Resolution.Height > 0 ? new ValueRange<double>(0, this.Resolution.Height) : null;

        /// <summary>
        /// Notifies the visualization object of the current mouse position (in image pixels).
        /// </summary>
        /// <param name="mousePosition">The position of the mouse in image pixels.</param>
        public void SetMousePosition(Point mousePosition)
        {
            this.MousePosition = mousePosition;
        }

        /// <inheritdoc/>
        protected override void OnPanelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(XYVisualizationPanel.XAxis))
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

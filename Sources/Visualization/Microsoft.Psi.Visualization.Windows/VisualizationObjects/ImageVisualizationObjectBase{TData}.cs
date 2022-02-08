// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents an image visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the image visualization object.</typeparam>
    [VisualizationPanelType(VisualizationPanelType.XY)]
    public abstract class ImageVisualizationObjectBase<TData> : XYValueVisualizationObject<TData>
    {
        /// <summary>
        /// Indicates whether we will flip the image horizontally.
        /// </summary>
        private bool horizontalFlip = false;

        /// <summary>
        /// The image resolution.
        /// </summary>
        private Size resolution = default;

        /// <summary>
        /// Gets or sets a value indicating whether we will flip the image horizontally.
        /// </summary>
        [DataMember]
        [DisplayName("Horizontal Flip")]
        [Description("Flip the image horizontally.")]
        public bool HorizontalFlip
        {
            get { return this.horizontalFlip; }
            set { this.Set(nameof(this.HorizontalFlip), ref this.horizontalFlip, value); }
        }

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
                        this.OnXValueRangeChanged();
                    }

                    if (previousHeight != this.resolution.Height)
                    {
                        this.OnYValueRangeChanged();
                    }
                }
            }
        }

        /// <inheritdoc/>
        [IgnoreDataMember]
        [Browsable(false)]
        public override ValueRange<double> XValueRange
        {
            get => this.Resolution.Width > 0 ? new ValueRange<double>(0, this.Resolution.Width) : null;
        }

        /// <inheritdoc/>
        [IgnoreDataMember]
        [Browsable(false)]
        public override ValueRange<double> YValueRange
        {
            get => this.Resolution.Height > 0 ? new ValueRange<double>(0, this.Resolution.Height) : null;
        }
    }
}

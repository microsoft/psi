// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Defines depth image ranges to use when pseudo-colorizing the image.
    /// </summary>
    public enum DepthImageRangeMode
    {
        /// <summary>
        /// The maximum range, i.e., 0 - 65535
        /// </summary>
        Maximum,

        /// <summary>
        /// Automatically computed range, based on the values in the image
        /// </summary>
        Auto,

        /// <summary>
        /// A custom-specified range.
        /// </summary>
        Custom,

        /// <summary>
        /// Depth range for Azure kinect device in NFOV binned mode.
        /// </summary>
        AzureKinectNFOVBinned,

        /// <summary>
        /// Depth range for Azure kinect device in NFOV unbinned mode.
        /// </summary>
        AzureKinectNFOVUnbinned,

        /// <summary>
        /// Depth range for Azure kinect device in WFOV binned mode.
        /// </summary>
        AzureKinectWFOVBinned,

        /// <summary>
        /// Depth range for Azure kinect device in WFOV unbinned mode.
        /// </summary>
        AzureKinectWFOVUnbinned,
    }

    /// <summary>
    /// Implements a depth image visualization object.
    /// </summary>
    /// <remarks>
    /// This visualization object shows a depth image by performing pseudo-
    /// colorization. The RangeLow and RangeHi parameters specify the range
    /// of the depth values expected in the depth image.
    /// </remarks>
    [VisualizationObject("Colorized Depth Image")]
    public class DepthImageVisualizationObject : ImageVisualizationObjectBase<Shared<DepthImage>>
    {
        /// <summary>
        /// Indicates the value in the depth image that is considered invalid and pseudo-colorized as transparent.
        /// </summary>
        private int invalidValue = -1;

        /// <summary>
        /// Indicates whether to render invalid depths as transparent.
        /// </summary>
        private bool invalidAsTransparent = false;

        /// <summary>
        /// Indicates the minimum of the depth values range in the image.
        /// </summary>
        private int rangeMin = 0;

        /// <summary>
        /// Indicates the maximum of the depth values range in the image.
        /// </summary>
        private int rangeMax = 65535;

        /// <summary>
        /// The depth image range.
        /// </summary>
        private DepthImageRangeMode rangeMode = DepthImageRangeMode.Maximum;

        /// <summary>
        /// The value of the pixel under the cursor.
        /// </summary>
        private string pixelValue = string.Empty;

        /// <summary>
        /// Gets the value of the pixel under the mouse cursor.
        /// </summary>
        [DataMember]
        [DisplayName("Pixel Value")]
        [Description("The value of the pixel under the mouse cursor.")]
        public string PixelValue => this.pixelValue;

        /// <summary>
        /// Gets or sets a value indicating an invalid depth.
        /// </summary>
        [DataMember]
        [DisplayName("Invalid Value")]
        [Description("Specifies the pixel value that denotes an invalid depth.")]
        public int InvalidValue
        {
            get { return this.invalidValue; }
            set { this.Set(nameof(this.InvalidValue), ref this.invalidValue, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to render invalid depths as transparent.
        /// </summary>
        [DataMember]
        [DisplayName("Invalid Value as Transparent")]
        [Description("Indicates whether to render invalid depths as transparent.")]
        public bool InvalidAsTransparent
        {
            get { return this.invalidAsTransparent; }
            set { this.Set(nameof(this.InvalidAsTransparent), ref this.invalidAsTransparent, value); }
        }

        /// <summary>
        /// Gets or sets the range of values to use.
        /// </summary>
        [DataMember]
        [DisplayName("Range Mode")]
        [Description("Specifies the range of depth values in the image.")]
        public DepthImageRangeMode RangeMode
        {
            get => this.rangeMode;
            set
            {
                this.Set(nameof(this.RangeMode), ref this.rangeMode, value);
                if (this.rangeMode != DepthImageRangeMode.Auto && this.rangeMode != DepthImageRangeMode.Custom)
                {
                    (var min, var max, var invalid) = GetRange(this.rangeMode);
                    this.SetRange(min, max);
                    this.InvalidValue = invalid;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the minimum of the depth values range in the image.
        /// </summary>
        [DataMember]
        [DisplayName("Range Min")]
        [Description("Specifies the minimum depth value for pseudo-colorizing the image.")]
        public int RangeMin
        {
            get => this.rangeMin;
            set
            {
                if (value != this.rangeMin)
                {
                    this.RangeMode = DepthImageRangeMode.Custom;
                    this.Set(nameof(this.RangeMin), ref this.rangeMin, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the maximum of the depth values range in the image.
        /// </summary>
        [DataMember]
        [DisplayName("Range Max")]
        [Description("Specifies the maximum depth value for pseudo-colorizing the image.")]
        public int RangeMax
        {
            get => this.rangeMax;
            set
            {
                if (value != this.rangeMax)
                {
                    this.RangeMode = DepthImageRangeMode.Custom;
                    this.Set(nameof(this.RangeMax), ref this.rangeMax, value);
                }
            }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(DepthImageVisualizationObjectView));

        /// <summary>
        /// Gets the range information for a specified depth image range mode.
        /// </summary>
        /// <param name="depthImageRangeMode">The specified depth image range mode.</param>
        /// <returns>A tuple containing the minimum, maximum and invalid values for the depth image pixels.</returns>
        public static (int Min, int Max, int Invalid) GetRange(DepthImageRangeMode depthImageRangeMode)
        {
            if (depthImageRangeMode == DepthImageRangeMode.Maximum)
            {
                return (0, 65535, -1);
            }
            else if (depthImageRangeMode == DepthImageRangeMode.AzureKinectNFOVBinned)
            {
                return (500, 5460, -1);
            }
            else if (depthImageRangeMode == DepthImageRangeMode.AzureKinectNFOVUnbinned)
            {
                return (500, 3860, 0);
            }
            else if (depthImageRangeMode == DepthImageRangeMode.AzureKinectWFOVBinned)
            {
                return (250, 2880, 0);
            }
            else if (depthImageRangeMode == DepthImageRangeMode.AzureKinectWFOVUnbinned)
            {
                return (250, 2210, 0);
            }
            else
            {
                throw new InvalidEnumArgumentException(nameof(depthImageRangeMode), (int)depthImageRangeMode, typeof(DepthImageRangeMode));
            }
        }

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var items = base.ContextMenuItemsInfo();

            // Add Set Range mode commands
            var rangeModeItems = new ContextMenuItemInfo("Set Range Mode");

            rangeModeItems.SubItems.Add(
                new ContextMenuItemInfo(
                    string.Empty,
                    DepthImageRangeMode.Auto.ToString(),
                    new RelayCommand(() => this.RangeMode = DepthImageRangeMode.Auto)));

            rangeModeItems.SubItems.Add(
                new ContextMenuItemInfo(
                    string.Empty,
                    DepthImageRangeMode.Maximum.ToString(),
                    new RelayCommand(() => this.RangeMode = DepthImageRangeMode.Maximum)));

            items.Add(rangeModeItems);

            return items;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.CurrentData))
            {
                if (this.CurrentData != default && this.CurrentData.Resource != null)
                {
                    this.Resolution = new Size(this.CurrentData.Resource.Width, this.CurrentData.Resource.Height);
                }
                else
                {
                    this.Resolution = default;
                }
            }

            base.OnPropertyChanged(sender, e);
        }

        /// <inheritdoc/>
        protected override void OnPanelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(XYVisualizationPanel.MousePosition))
            {
                this.UpdatePixelValue();
            }

            base.OnPanelPropertyChanged(sender, e);
        }

        /// <summary>
        /// Programmatically sets the range without altering the range compute mode.
        /// </summary>
        /// <param name="rangeMin">The new range minimum value.</param>
        /// <param name="rangeMax">The new range maximum value.</param>
        private void SetRange(int rangeMin, int rangeMax)
        {
            this.Set(nameof(this.RangeMin), ref this.rangeMin, rangeMin);
            this.Set(nameof(this.RangeMax), ref this.rangeMax, rangeMax);
        }

        /// <summary>
        /// Update the pixel value.
        /// </summary>
        private void UpdatePixelValue()
        {
            this.RaisePropertyChanging(nameof(this.PixelValue));

            if (this.CurrentData != default && this.CurrentData.Resource != default)
            {
                var mousePosition = (this.Panel as XYVisualizationPanel).MousePosition;
                if (this.CurrentData.Resource.TryGetPixel((int)mousePosition.X, (int)mousePosition.Y, out var value))
                {
                    this.pixelValue = $"{value}";
                }
                else
                {
                    this.pixelValue = string.Empty;
                }
            }
            else
            {
                this.pixelValue = string.Empty;
            }

            this.RaisePropertyChanged(nameof(this.PixelValue));
        }
    }
}

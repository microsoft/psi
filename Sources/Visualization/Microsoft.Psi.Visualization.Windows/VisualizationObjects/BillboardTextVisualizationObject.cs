// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a 3D text billboard visualization object.
    /// </summary>
    [VisualizationObject("3D Text Billboard")]
    public class BillboardTextVisualizationObject : ModelVisual3DVisualizationObject<Tuple<Point3D, string>>
    {
        private readonly BillboardTextVisual3D billboard;

        private Color billboardBackColor = Colors.Gray;
        private Color billboardBorderColor = Colors.LightSteelBlue;
        private Color billboardForeColor = Colors.White;
        private double billboardPadding = 5;
        private int billboardOpacity = 100;
        private double billboardFontSize = 10;
        private double billboardBorderThickness = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillboardTextVisualizationObject"/> class.
        /// </summary>
        public BillboardTextVisualizationObject()
        {
            this.billboard = new BillboardTextVisual3D()
            {
                FontSize = this.BillboardFontSize,
                BorderBrush = new SolidColorBrush(this.BillboardBorderColor),
                Background = new SolidColorBrush(this.BillboardBackgroundColor)
                {
                    Opacity = this.BillboardOpacity * 0.01,
                },
                Foreground = new SolidColorBrush(this.BillboardForegroundColor),
                Padding = new System.Windows.Thickness(this.BillboardPadding),
                BorderThickness = new System.Windows.Thickness(this.BillboardBorderThickness),
            };

            this.UpdateVisibility();
        }

        /// <summary>
        /// Gets or sets the billboard border thickness.
        /// </summary>
        [DataMember]
        [DisplayName("Billboard Border Thickness")]
        [Description("The billboard border thickness.")]
        [PropertyOrder(6)]
        public double BillboardBorderThickness
        {
            get { return this.billboardBorderThickness; }
            set { this.Set(nameof(this.BillboardBorderThickness), ref this.billboardBorderThickness, value); }
        }

        /// <summary>
        /// Gets or sets the border color of the billboard.
        /// </summary>
        [DataMember]
        [DisplayName("Billboard Font Size")]
        [Description("The billboard font size.")]
        [PropertyOrder(5)]
        public double BillboardFontSize
        {
            get { return this.billboardFontSize; }
            set { this.Set(nameof(this.BillboardFontSize), ref this.billboardFontSize, value); }
        }

        /// <summary>
        /// Gets or sets the opacity of the billboard.
        /// </summary>
        [DataMember]
        [DisplayName("Billboard Opacity")]
        [Description("The % transparency of the billboard.")]
        [PropertyOrder(4)]
        public int BillboardOpacity
        {
            get { return this.billboardOpacity; }
            set { this.Set(nameof(this.BillboardOpacity), ref this.billboardOpacity, value); }
        }

        /// <summary>
        /// Gets or sets the padding around the billboard.
        /// </summary>
        [DataMember]
        [DisplayName("Billboard Padding")]
        [Description("The padding around the billboard.")]
        [PropertyOrder(3)]
        public double BillboardPadding
        {
            get { return this.billboardPadding; }
            set { this.Set(nameof(this.BillboardPadding), ref this.billboardPadding, value); }
        }

        /// <summary>
        /// Gets or sets the border color of the billboard.
        /// </summary>
        [DataMember]
        [DisplayName("Billboard Border Color")]
        [Description("The border color of the billboard.")]
        [PropertyOrder(2)]
        public Color BillboardBorderColor
        {
            get { return this.billboardBorderColor; }
            set { this.Set(nameof(this.BillboardBorderColor), ref this.billboardBorderColor, value); }
        }

        /// <summary>
        /// Gets or sets the background color of the billboard.
        /// </summary>
        [DataMember]
        [DisplayName("Billboard Background Color")]
        [Description("The background color of the billboard.")]
        [PropertyOrder(1)]
        public Color BillboardBackgroundColor
        {
            get { return this.billboardBackColor; }
            set { this.Set(nameof(this.BillboardBackgroundColor), ref this.billboardBackColor, value); }
        }

        /// <summary>
        /// Gets or sets the foreground color of the billboard.
        /// </summary>
        [DataMember]
        [DisplayName("Billboard Foreground Color")]
        [Description("The foreground color of the billboard.")]
        [PropertyOrder(0)]
        public Color BillboardForegroundColor
        {
            get { return this.billboardForeColor; }
            set { this.Set(nameof(this.BillboardForegroundColor), ref this.billboardForeColor, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != null)
            {
                this.billboard.Text = this.CurrentData.Item2;
                this.UpdateBillboardPosition();
            }

            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
            else if (propertyName == nameof(this.BillboardBackgroundColor) || propertyName == nameof(this.BillboardOpacity))
            {
                this.billboard.Background = new SolidColorBrush(this.BillboardBackgroundColor)
                {
                    Opacity = this.BillboardOpacity * 0.01,
                };
            }
            else if (propertyName == nameof(this.BillboardForegroundColor))
            {
                this.billboard.Foreground = new SolidColorBrush(this.BillboardForegroundColor);
            }
            else if (propertyName == nameof(this.BillboardBorderColor))
            {
                this.billboard.BorderBrush = new SolidColorBrush(this.BillboardBorderColor);
            }
            else if (propertyName == nameof(this.BillboardPadding))
            {
                this.billboard.Padding = new System.Windows.Thickness(this.BillboardPadding);
            }
            else if (propertyName == nameof(this.BillboardFontSize))
            {
                this.billboard.FontSize = this.BillboardFontSize;
            }
            else if (propertyName == nameof(this.BillboardBorderThickness))
            {
                this.billboard.BorderThickness = new Thickness(this.BillboardBorderThickness);
            }
        }

        private void UpdateBillboardPosition()
        {
            if (this.CurrentData != null)
            {
                var pos = this.CurrentData.Item1;
                this.billboard.Position = new Point3D(pos.X, pos.Y, pos.Z);
            }
        }

        private void UpdateVisibility()
        {
            var visible = this.Visible && !string.IsNullOrWhiteSpace(this.CurrentData?.Item2);
            this.UpdateChildVisibility(this.billboard, visible);
        }
    }
}

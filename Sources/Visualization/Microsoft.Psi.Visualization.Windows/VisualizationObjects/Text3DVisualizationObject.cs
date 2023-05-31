// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows.Media;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Extensions;

    /// <summary>
    /// Implements a visualization object for a spatial image.
    /// </summary>
    [VisualizationObject("3D Text")]
    public class Text3DVisualizationObject : ModelVisual3DVisualizationObject<(string, CoordinateSystem)>
    {
        private readonly TextVisual3D textVisual3D;
        private CoordinateSystem position = null;
        private string text = null;

        private double textHeightCm = 50;
        private Color fontColor = Colors.LightGray;

        /// <summary>
        /// Initializes a new instance of the <see cref="Text3DVisualizationObject"/> class.
        /// </summary>
        public Text3DVisualizationObject()
        {
            // Create the text visual element
            this.textVisual3D = new TextVisual3D()
            {
                Height = this.textHeightCm * 0.01,
                Foreground = new SolidColorBrush(this.fontColor),
            };
        }

        /// <summary>
        /// Gets or sets the font size, in centimeters.
        /// </summary>
        [DataMember]
        [DisplayName("Text Height (cm)")]
        [Description("The text height.")]
        public double TextHeightCm
        {
            get { return this.textHeightCm; }
            set { this.Set(nameof(this.TextHeightCm), ref this.textHeightCm, value); }
        }

        /// <summary>
        /// Gets or sets the font color.
        /// </summary>
        [DataMember]
        [DisplayName("Font Color")]
        [Description("The font color.")]
        public Color FontColor
        {
            get { return this.fontColor; }
            set { this.Set(nameof(this.FontColor), ref this.fontColor, value); }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            this.text = this.CurrentData.Item1;
            this.position = this.CurrentData.Item2;

            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.TextHeightCm) ||
                propertyName == nameof(this.FontColor))
            {
                this.UpdateVisuals();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        private void UpdateVisuals()
        {
            // update the text
            this.textVisual3D.Text = this.text;

            // update the font
            this.textVisual3D.Height = this.textHeightCm * 0.01;
            this.textVisual3D.Foreground = new SolidColorBrush(this.fontColor);

            if (this.position != null)
            {
                this.textVisual3D.Position = this.position.Origin.ToPoint3D();
                this.textVisual3D.TextDirection = this.position.YAxis.ToVector3D();
                this.textVisual3D.UpDirection = this.position.ZAxis.ToVector3D();
            }
        }

        private void UpdateVisibility()
        {
            var visible = this.Visible && this.CurrentData != default && this.position != null;
            this.UpdateChildVisibility(this.textVisual3D, visible);
        }
    }
}

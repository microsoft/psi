// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Views.Visuals3D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents a XY panel 3D visualization object.
    /// </summary>
    [VisualizationObject("Visualize 2D Panel in 3D Space")]
    public class XYPanel3DVisualizationObject : Instant3DVisualizationObject<CoordinateSystem>
    {
        private double height;
        private double width;

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        [DataMember]
        public double Height
        {
            get { return this.height; }
            set { this.Set(nameof(this.Height), ref this.height, value); }
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        [DataMember]
        public double Width
        {
            get { return this.width; }
            set { this.Set(nameof(this.Width), ref this.width, value); }
        }

        /// <summary>
        /// Gets or sets the XY visualization panel.
        /// </summary>
        [IgnoreDataMember]
        public XYVisualizationPanel XYVisualizationPanel { get; set; }

        /// <inheritdoc />
        protected override void InitNew()
        {
            this.Visual3D = new XYPanelVisual(this);
            this.XYVisualizationPanel = new XYVisualizationPanel();
            base.InitNew();
        }

        /// <inheritdoc />
        protected override void OnAddToPanel()
        {
            base.OnAddToPanel();
            this.Container.AddPanel(this.XYVisualizationPanel, false);
        }

        /// <inheritdoc/>
        protected override void OnRemoveFromPanel()
        {
            this.Container.RemovePanel(this.XYVisualizationPanel);
            base.OnRemoveFromPanel();
        }
    }
}

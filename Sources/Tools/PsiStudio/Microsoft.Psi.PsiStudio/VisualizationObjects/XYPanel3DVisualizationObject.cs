// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Views.Visuals3D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents a XY panel 3D visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class XYPanel3DVisualizationObject : Instant3DVisualizationObject<CoordinateSystem, XYPanel3DVisualizationObjectConfiguration>
    {
        /// <summary>
        /// Gets or sets teh XY visualization panel.
        /// </summary>
        [IgnoreDataMember]
        public XYVisualizationPanel XYVisualizationPanel { get; set; }

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.XYVisualizationPanel = new XYVisualizationPanel();
            this.Visual3D = new XYPanelVisual(this);
        }

        /// <inheritdoc />
        protected override void OnConnect()
        {
            base.OnConnect();
            this.Container.AddPanel(this.XYVisualizationPanel, false);
        }

        /// <inheritdoc/>
        protected override void OnDisconnect()
        {
            this.Container.RemovePanel(this.XYVisualizationPanel);
            base.OnDisconnect();
        }
    }
}

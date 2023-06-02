// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Numerics;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a visualization object for PositionData.
    /// </summary>
    [VisualizationObject("PositionData")]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class PositionDataVisualizationObject : StreamValueVisualizationObject<PositionData>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(PositionDataVisualizationObjectView));
    
        public PositionDataVisualizationObject() { }
    }

    [System.Serializable]
    public class PositionData
    {
        public DateTime originatingTime { get; set; }
        public float deltatime { get; set; }
        public int userID { get; set; }
        public string headPos { get; set; }
        public string lHandPos { get; set; }
        public string rHandPos { get; set; }
        public Vector3 headPosv { get; set; }
        public Vector3 lHandPosv { get; set; }
        public Vector3 rHandPosv { get; set; }
    }
}

#pragma warning enable
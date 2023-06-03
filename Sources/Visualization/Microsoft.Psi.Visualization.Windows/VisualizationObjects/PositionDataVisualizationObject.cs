// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a visualization object for PositionData.
    /// </summary>
    [VisualizationObject("PositionData")]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class PositionDataVisualizationObject : StreamIntervalVisualizationObject<PositionData>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(PositionDataVisualizationObjectView));

        public String HeadPos
        {
            get { return this.Data != null ? this.CurrentValue.Value.Data.ToString() : "Pas de valeur"; }
        }

    }

    public class PositionData
    {
        public DateTime originatingTime;
        public float deltatime;
        public int userID;
        public string headPos;
        public string lHandPos;
        public string rHandPos;
        public Vector3 headPosv;
        public Vector3 lHandPosv;
        public Vector3 rHandPosv;

        public override string ToString() {
            return "caca" + headPos;
        }
    }
}

#pragma warning enable
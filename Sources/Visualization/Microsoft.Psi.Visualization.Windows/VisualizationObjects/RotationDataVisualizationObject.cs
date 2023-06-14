// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Numerics;
    using System.Text;
    using System.Threading.Tasks;
    using System.Runtime.Serialization;
    using System.Security.Cryptography;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a visualization object for RotationData. 
    /// </summary>
    [VisualizationObject("RotationData")]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class RotationDataVisualizationObject : StreamValueVisualizationObject<RotationData>, INotifyPropertyChanged
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(RotationDataVisualizationObjectView));

    }


    public class RotationData
    {
        public DateTime originatingTime;
        public float deltatime;
        public int userID;
        public string headRot;
        public string lHandRot;
        public string rHandRot;
        public Vector3 headRotv;
        public Vector3 lHandRotv;
        public Vector3 rHandRotv;
    }
}

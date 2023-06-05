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
    public class PositionDataVisualizationObject : StreamValueVisualizationObject<PositionData>, INotifyPropertyChanged
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(PositionDataVisualizationObjectView));

        // On update
        protected override void OnPropertyChanging(object sender, PropertyChangingEventArgs e) {
            base.OnPropertyChanging(sender, e);

            if (e.PropertyName == nameof(this.CurrentValue)){
                this.RaisePropertyChanging(nameof(this.HeadPosition));
                this.RaisePropertyChanging(nameof(this.HeadPositionX));
                this.RaisePropertyChanging(nameof(this.HeadPositionY));
                this.RaisePropertyChanging(nameof(this.HeadPositionZ));
            }
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(this.CurrentValue)) {
                this.RaisePropertyChanged(nameof(this.HeadPosition));
                this.RaisePropertyChanged(nameof(this.HeadPositionX));
                this.RaisePropertyChanged(nameof(this.HeadPositionY));
                this.RaisePropertyChanged(nameof(this.HeadPositionZ));
            }

            base.OnPropertyChanged(sender, e);
        }

        [IgnoreDataMember]
        public Vector3 HeadPosition
        {
            get
            {
                if (this.CurrentValue.HasValue)
                {
                    return this.CurrentValue.Value.Data.headPosv;
                }
                else
                {
                    // Renvoyer une valeur par défaut appropriée
                    return new Vector3(float.NaN, float.NaN, float.NaN);
                }
            }
        }

        public virtual float HeadPositionX
        {
            get
            {
                return this.HeadPosition.X;
            }
        }

        public virtual float HeadPositionY
        {
            get
            {
                return this.HeadPosition.Y;
            }
        }

        public virtual float HeadPositionZ
        {
            get
            {
                return this.HeadPosition.Z;
            }
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
    }
}

#pragma warning enable
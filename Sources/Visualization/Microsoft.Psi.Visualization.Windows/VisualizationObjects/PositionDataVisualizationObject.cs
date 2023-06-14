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
    using System.Security.Cryptography;
    using System.Windows;
    using System.Windows.Media.Imaging;
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
        private float minimumPositionX = 0;
        private float maximumPositionX = 1;
        private float minimumPositionY = 0;
        private float maximumPositionY = 1;
        private bool _isHeadNameHidden;

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

        [DataMember]
        public float MinimumPositionX
        {
            get
            {
                return this.minimumPositionX;
            }

            set
            {
                this.minimumPositionX = value;
                this.RaisePropertyChanged(nameof(this.HeadPositionX));
            }
        }

        [DataMember]
        public float MaximumPositionX
        {
            get
            {
                return this.maximumPositionX;
            }

            set
            {
                this.maximumPositionX = value;
                this.RaisePropertyChanged(nameof(this.HeadPositionX));
            }
        }

        [DataMember]
        public float MaximumPositionY
        {
            get
            {
                return this.maximumPositionY;
            }

            set
            {
                this.maximumPositionY = value;
                this.RaisePropertyChanged(nameof(this.HeadPositionY));
            }
        }

        [DataMember]
        public float MinimumPositionY
        {
            get
            {
                return this.minimumPositionY;
            }

            set
            {
                this.minimumPositionY = value;
                this.RaisePropertyChanged(nameof(this.HeadPositionY));
            }
        }

        public virtual float HeadPositionX
        {
            get
            {
                float interval = Math.Abs(minimumPositionX) + Math.Abs(maximumPositionX);
                float delta = Math.Abs(minimumPositionX) + Math.Abs(this.HeadPosition.X);
                float ratio = delta / interval * 100;

                return ratio;
            }
        }

        public virtual float HeadPositionY
        {
            get
            {
                float interval = Math.Abs(minimumPositionY) + Math.Abs(maximumPositionY);
                float delta = Math.Abs(minimumPositionY) + Math.Abs(this.HeadPosition.Y);
                float ratio = delta / interval * 100;

                return ratio;
            }
        }

        public virtual float HeadPositionZ
        {
            get
            {
                return this.HeadPosition.Z;
            }
        }

        [DataMember]
        public virtual string HeadColor
        {
            get
            {
                using var sha1 = SHA1.Create();
                byte[] hashBytes = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(this.Name));
                string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                string color = "#" + hashString.Substring(0, 6).ToString();
                return color;
            }
        }

        [DataMember]
        public virtual string HeadName
        {
            get
            {
                return this.Name;
            }
        }

        [DataMember]
        public bool IsHeadNameHidden
        {
            get { return _isHeadNameHidden; }
            set
            {
                _isHeadNameHidden = value;
                this.RaisePropertyChanged(nameof(IsHeadNameHidden));
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
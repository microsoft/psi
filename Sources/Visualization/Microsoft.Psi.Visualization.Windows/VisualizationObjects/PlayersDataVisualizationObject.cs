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
    using System.Runtime.Serialization;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a visualization object for PlayersData.
    /// </summary>
    [VisualizationObject("PlayersData")]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class PlayersDataVisualizationObject : StreamValueVisualizationObject<PlayersData>, INotifyPropertyChanged
    {
        private float minimumPositionX = 0;
        private float maximumPositionX = 1;
        private float minimumPositionY = 0;
        private float maximumPositionY = 1;
        private bool _isHeadNameHidden;


        /// <inheritdoc/>
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(PlayersDataVisualizationObjectView));

        // On update
        protected override void OnPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            base.OnPropertyChanging(sender, e);

            if (e.PropertyName == nameof(this.CurrentValue))
            {
                this.RaisePropertyChanging(nameof(this.GetPlayersData));
                this.RaisePropertyChanging(nameof(this.GetPlayersDataAsString));
                this.RaisePropertyChanging(nameof(this.PositionPlayer1));
                this.RaisePropertyChanging(nameof(this.PositionPlayer2));
                this.RaisePropertyChanging(nameof(this.RotationPlayer1));
                this.RaisePropertyChanging(nameof(this.RotationPlayer2));
                this.RaisePropertyChanging(nameof(this.VadPlayer1));
                this.RaisePropertyChanging(nameof(this.VadPlayer2));
                this.RaisePropertyChanging(nameof(this.JVAData));
            }
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.CurrentValue))
            {
                this.RaisePropertyChanged(nameof(this.GetPlayersData));
                this.RaisePropertyChanged(nameof(this.GetPlayersDataAsString));
                this.RaisePropertyChanged(nameof(this.PositionPlayer1));
                this.RaisePropertyChanged(nameof(this.PositionPlayer2));
                this.RaisePropertyChanged(nameof(this.RotationPlayer1));
                this.RaisePropertyChanged(nameof(this.RotationPlayer2));
                this.RaisePropertyChanged(nameof(this.VadPlayer1));
                this.RaisePropertyChanged(nameof(this.VadPlayer2));
                this.RaisePropertyChanged(nameof(this.JVAData));
            }

            base.OnPropertyChanged(sender, e);
        }

        /// <summary>
        /// Returns the PlayersData value at time t.
        /// </summary>
        /// <returns>
        /// PlayersData instance.
        /// </returns>
        public PlayersData GetPlayersData()
        {
            if (this.CurrentValue.HasValue)
            {
                return this.CurrentValue.Value.Data;
            }
            else
            {
;               PlayersData placeholder = new PlayersData(
                    new PipelineRejeuxDonnees.PositionData(""),
                    new PipelineRejeuxDonnees.PositionData(""),
                    new PipelineRejeuxDonnees.RotationData(""),
                    new PipelineRejeuxDonnees.RotationData(""),
                    false,
                    false,
                    new PipelineRejeuxDonnees.JVAData(
                        new DateTime(),
                        new DateTime(),
                        new DateTime(),
                        new DateTime(),
                        new TimeSpan(),
                        "",
                        0,
                        0
                    )
                );
                return placeholder;
            }
        }

        public virtual Vector3 PositionPlayer1
        {
            get
            {
                return this.GetPlayersData().position1.headPosv;
            }
        }
        public virtual float P1X
        {
            get
            {
                return this.PositionPlayer1.X;
            }
        }
        public virtual float P1Y
        {
            get
            {
                return this.PositionPlayer1.Y;
            }
        }

        public virtual PipelineRejeuxDonnees.PositionData PositionPlayer2
        {
            get
            {
                return this.GetPlayersData().position2;
            }
        }

        public virtual PipelineRejeuxDonnees.RotationData RotationPlayer1
        {
            get
            {
                return this.GetPlayersData().rotation1;
            }
        }
        public virtual PipelineRejeuxDonnees.RotationData RotationPlayer2
        {
            get
            {
                return this.GetPlayersData().rotation2;
            }
        }

        public virtual bool VadPlayer1
        {
            get
            {
                return this.GetPlayersData().vad1;
            }
        }

        public virtual bool VadPlayer2
        {
            get
            {
                return this.GetPlayersData().vad2;
            }
        }

        public virtual PipelineRejeuxDonnees.JVAData JVAData
        {
            get
            {
                return this.GetPlayersData().jvaEvent;
            }
        }

        /// <summary>
        ///  Returns the PlayersData value at time t as a string.
        /// </summary>
        /// <returns>
        /// String.
        /// </returns>
        public virtual string GetPlayersDataAsString
        {
            get
            {
                //string positions = this.PositionPlayer1.headPos.ToString() + "/" + this.PositionPlayer2.headPos.ToString();
                string rotations = this.RotationPlayer1.headRot.ToString() + "/" + this.RotationPlayer2.headRot.ToString();
                string vads = this.VadPlayer1.ToString() + "/" + this.VadPlayer2.ToString();

                string res = /*positions + "\n" +*/ rotations + "\n" + vads;

                PipelineRejeuxDonnees.JVAData JVAData = this.JVAData;
                if (JVAData != null)
                {
                    res += "\n" + JVAData.responder.ToString() + " " + JVAData.objectID.ToString();
                }

                return res;
            }
        }


        // Players positions
        /*public virtual float HeadPositionX
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
        }*/



        // Names and colors of the players
        [DataMember]
        public virtual string HeadName
        {
            get
            {
                return this.Name;
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
}
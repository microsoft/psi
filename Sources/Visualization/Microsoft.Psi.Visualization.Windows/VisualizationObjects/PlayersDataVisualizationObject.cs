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
    public class PlayersDataVisualizationObject : StreamValueVisualizationObject<List<PlayersData>>, INotifyPropertyChanged
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
                //this.RaisePropertyChanging(nameof(this.GetPlayersDataAsString));
                this.RaisePropertyChanging(nameof(this.PositionPlayer1));
                /*this.RaisePropertyChanging(nameof(this.PositionPlayer2));
                this.RaisePropertyChanging(nameof(this.RotationPlayer1));
                this.RaisePropertyChanging(nameof(this.RotationPlayer2));
                this.RaisePropertyChanging(nameof(this.VadPlayer1));
                this.RaisePropertyChanging(nameof(this.VadPlayer2));
                this.RaisePropertyChanging(nameof(this.JVAData));*/
                this.RaisePropertyChanging(nameof(this.P1X));
                this.RaisePropertyChanging(nameof(this.P1Y));
            }
        }

        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.CurrentValue))
            {
                this.RaisePropertyChanged(nameof(this.GetPlayersData));
                //this.RaisePropertyChanged(nameof(this.GetPlayersDataAsString));
                this.RaisePropertyChanged(nameof(this.PositionPlayer1));
                /*this.RaisePropertyChanged(nameof(this.PositionPlayer2));
                this.RaisePropertyChanged(nameof(this.RotationPlayer1));
                this.RaisePropertyChanged(nameof(this.RotationPlayer2));
                this.RaisePropertyChanged(nameof(this.VadPlayer1));
                this.RaisePropertyChanged(nameof(this.VadPlayer2));
                this.RaisePropertyChanged(nameof(this.JVAData));*/
                this.RaisePropertyChanged(nameof(this.P1X));
                this.RaisePropertyChanged(nameof(this.P1Y));
            }

            base.OnPropertyChanged(sender, e);
        }

        /// <summary>
        /// Returns the PlayersData value at time t.
        /// </summary>
        /// <returns>
        /// PlayersData instance.
        /// </returns>
        public List<PlayersData> GetPlayersData()
        {
            if (this.CurrentValue.HasValue)
            {
                return this.CurrentValue.Value.Data;
            }
            else
            {
                List<PlayersData> res = new List<PlayersData>(1);
                res.Add(
                    new PlayersData(
                        new PipelineRejeuxDonnees.PositionData(""),
                        new PipelineRejeuxDonnees.RotationData(""),
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
                    )
                );
                return res;
            }
        }

        public virtual Vector3 PositionPlayer1
        {
            get
            {
                return this.GetPlayersData().ElementAt(0).position.headPosv;
            }
        }
        public float P1X
        {
            get
            {
                float interval = Math.Abs(minimumPositionX) + Math.Abs(maximumPositionX);
                float delta = Math.Abs(minimumPositionX) + Math.Abs(this.PositionPlayer1.X);
                float ratio = delta / interval * 100;

                return ratio;
            }
        }
        public float P1Y
        {
            get
            {
                float interval = Math.Abs(minimumPositionY) + Math.Abs(maximumPositionY);
                float delta = Math.Abs(minimumPositionY) + Math.Abs(this.PositionPlayer1.Y);
                float ratio = delta / interval * 100;

                return ratio;
            }
        }


        /// <summary>
        ///  Returns the PlayersData value at time t as a string.
        /// </summary>
        /// <returns>
        /// String.
        /// </returns>
        /*public virtual string GetPlayersDataAsString
        {
            get
            {
                string positions = this.PositionPlayer1.ToString() + "/" + this.PositionPlayer2.ToString();
                string rotations = this.RotationPlayer1.headRot.ToString() + "/" + this.RotationPlayer2.headRot.ToString();
                string vads = this.VadPlayer1.ToString() + "/" + this.VadPlayer2.ToString();

                string res = positions + "\n" + rotations + "\n" + vads;

                PipelineRejeuxDonnees.JVAData JVAData = this.JVAData;
                if (JVAData != null)
                {
                    res += "\n" + JVAData.responder.ToString() + " " + JVAData.objectID.ToString();
                }

                return res;
            }
        }*/


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
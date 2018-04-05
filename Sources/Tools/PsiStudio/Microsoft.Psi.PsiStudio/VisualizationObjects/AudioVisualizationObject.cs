// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents an audio visualization object.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AudioVisualizationObject : PlotVisualizationObject<AudioVisualizationObjectConfiguration>
    {
        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(AudioVisualizationObjectView));

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.Configuration.LineColor = Colors.White;
            this.Configuration.MarkerColor = Colors.White;
            this.Configuration.RangeColor = Colors.White;
        }

        /// <inheritdoc />
        protected override void OnConfigurationPropertyChanged(string propertyName)
        {
            base.OnConfigurationPropertyChanged(propertyName);
            if (propertyName == nameof(AudioVisualizationObjectConfiguration.Channel))
            {
                if (this.Panel != null)
                {
                    // NOTE: Only open a stream when this visualization object is connected to it's parent

                    // Create a new binding with a different channel argument and re-open the stream
                    var newBinding = new StreamBinding(this.Configuration.StreamBinding, typeof(AudioSummarizer), new object[] { this.Configuration.Channel });
                    this.OpenStream(newBinding);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnConnect()
        {
            base.OnConnect();
            if (this.Configuration.StreamBinding != null)
            {
                this.OnConfigurationPropertyChanged(nameof(AudioVisualizationObjectConfiguration.Channel));
            }
        }
    }
}

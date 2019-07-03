// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;
    using System.Windows.Media;

    /// <summary>
    /// Represents the annotated event visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotatedEventVisualizationObjectConfiguration : TimelineVisualizationObjectConfiguration
    {
        /// <summary>
        /// The height of the annotated event.
        /// </summary>
        private double height = 20;

        /// <summary>
        /// The color of the text.
        /// </summary>
        private Color textColor = Colors.Black;

        /// <summary>
        /// Gets or sets the annotation height.
        /// </summary>
        [DataMember]
        public double Height
        {
            get { return this.height; }
            set { this.Set(nameof(this.Height), ref this.height, value); }
        }

        /// <summary>
        /// Gets or sets the text color.
        /// </summary>
        [DataMember]
        public Color TextColor
        {
            get { return this.textColor; }
            set { this.Set(nameof(this.TextColor), ref this.textColor, value); }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents data used to display axes in visualization panels.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AxisInfo : ObservableObject
    {
        private string label;
        private Color color;
        private int fontSize;
        private HorizontalAlignment horizontalAlignment;
        private VerticalAlignment verticalAlignment;

        /// <summary>
        /// Gets or sets axis label.
        /// </summary>
        [DataMember]
        public string Label
        {
            get { return this.label; }
            set { this.Set(nameof(this.Label), ref this.label, value); }
        }

        /// <summary>
        /// Gets or sets axis color.
        /// </summary>
        [DataMember]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets axis font size.
        /// </summary>
        [DataMember]
        public int FontSize
        {
            get { return this.fontSize; }
            set { this.Set(nameof(this.FontSize), ref this.fontSize, value); }
        }

        /// <summary>
        /// Gets or sets axis horizontal alignment.
        /// </summary>
        [DataMember]
        public HorizontalAlignment HorizontalAlignment
        {
            get { return this.horizontalAlignment; }
            set { this.Set(nameof(this.HorizontalAlignment), ref this.horizontalAlignment, value); }
        }

        /// <summary>
        /// Gets or sets axis vertical alignment.
        /// </summary>
        [DataMember]
        public VerticalAlignment VerticalAlignment
        {
            get { return this.verticalAlignment; }
            set { this.Set(nameof(this.VerticalAlignment), ref this.verticalAlignment, value); }
        }
    }
}

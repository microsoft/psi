// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a visualization object for IFormattable objects.
    /// </summary>
    [VisualizationObject("IFormattable")]
    [VisualizationPanelType(VisualizationPanelType.Canvas)]
    public class IFormattableVisualizationObject : StreamValueVisualizationObject<IFormattable>
    {
        private Thickness margin = new (5, 0, 0, 0);
        private string formatString = default;
        private string formattedData = default;

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(IFormattableVisualizationObjectView));

        /// <summary>
        /// Gets or sets the margin.
        /// </summary>
        [DataMember]
        [DisplayName("Margin")]
        [Description("The left, top, right and bottom margin in pixels.")]
        public Thickness Margin
        {
            get { return this.margin; }
            set { this.Set(nameof(this.Margin), ref this.margin, value); }
        }

        /// <summary>
        /// Gets or sets the radius of the cursor epsilon. (This value is exposed in the Properties UI).
        /// </summary>
        [DataMember]
        [DisplayName("Format String")]
        [Description("The format string used to format the data.")]
        public string FormatString
        {
            get { return this.formatString; }
            set { this.Set(nameof(this.FormatString), ref this.formatString, value); }
        }

        /// <summary>
        /// Gets or sets the formatted data.
        /// </summary>
        [IgnoreDataMember]
        [Browsable(false)]
        public string FormattedData
        {
            get { return this.formattedData; }
            set { this.Set(nameof(this.FormattedData), ref this.formattedData, value); }
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.CurrentValue) || e.PropertyName == nameof(this.FormatString))
            {
                this.FormattedData = this.CurrentData?.ToString(this.FormatString, CultureInfo.CurrentCulture);
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Implements visualization object for <see cref="Object2DDetectionResults"/>.
    /// </summary>
    [VisualizationObject("Object 2D Detection Results")]
    public class Object2DDetectionResultsVisualizationObject : ImageVisualizationObjectBase<Object2DDetectionResults>
    {
        private bool showMask = true;
        private bool showBoundingBox = true;
        private bool showClass = true;
        private bool showInstanceId = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the object mask.
        /// </summary>
        [DataMember]
        [DisplayName("Show Mask")]
        [Description("Shows the object mask.")]
        public bool ShowMask
        {
            get { return this.showMask; }
            set { this.Set(nameof(this.ShowMask), ref this.showMask, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the object bounding box.
        /// </summary>
        [DataMember]
        [DisplayName("Show Bounding Box")]
        [Description("Shows the object bounding box.")]
        public bool ShowBoundingBox
        {
            get { return this.showBoundingBox; }
            set { this.Set(nameof(this.ShowBoundingBox), ref this.showBoundingBox, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the object class.
        /// </summary>
        [DataMember]
        [DisplayName("Show Class")]
        [Description("Shows the object vlass.")]
        public bool ShowClass
        {
            get { return this.showClass; }
            set { this.Set(nameof(this.ShowClass), ref this.showClass, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the object instance id.
        /// </summary>
        [DataMember]
        [DisplayName("Show Instance Id")]
        [Description("Shows the object instance id.")]
        public bool ShowInstanceId
        {
            get { return this.showInstanceId; }
            set { this.Set(nameof(this.ShowInstanceId), ref this.showInstanceId, value); }
        }

        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(Object2DDetectionResultsVisualizationObjectView));

        /// <inheritdoc />
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.CurrentData))
            {
                if (this.CurrentData != default && this.CurrentData != null)
                {
                    this.Resolution = new Size(this.CurrentData.ImageSize.Width, this.CurrentData.ImageSize.Height);
                }
                else
                {
                    this.Resolution = default;
                }
            }

            base.OnPropertyChanged(sender, e);
        }
    }
}

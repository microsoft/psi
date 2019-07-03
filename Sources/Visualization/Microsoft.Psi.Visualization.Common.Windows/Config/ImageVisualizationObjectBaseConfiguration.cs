// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Config
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an image visualization object configuration.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ImageVisualizationObjectBaseConfiguration : InstantVisualizationObjectConfiguration
    {
        /// <summary>
        /// Indicates whether we will flip the image horizontally.
        /// </summary>
        private bool horizontalFlip = false;

        /// <summary>
        /// Indicates whether we are stretching to fit.
        /// </summary>
        private bool stretchToFit = true;

        /// <summary>
        /// Gets or sets a value indicating whether we will flip the image horizontally.
        /// </summary>
        [DataMember]
        public bool HorizontalFlip
        {
            get { return this.horizontalFlip; }
            set { this.Set(nameof(this.HorizontalFlip), ref this.horizontalFlip, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether we are stretching to fit.
        /// </summary>
        [DataMember]
        public bool StretchToFit
        {
            get { return this.stretchToFit; }
            set { this.Set(nameof(this.StretchToFit), ref this.stretchToFit, value); }
        }
    }
}

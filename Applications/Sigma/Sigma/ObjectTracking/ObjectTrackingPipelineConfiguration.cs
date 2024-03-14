// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Defines the object detector type.
    /// </summary>
    public enum ObjectDetectorType
    {
        /// <summary>
        /// Use the Detic object detector.
        /// </summary>
        Detic,

        /// <summary>
        /// Use the SEEM object detector.
        /// </summary>
        SEEM,
    }

    /// <summary>
    /// Represents the configuration for the object tracking pipeline.
    /// </summary>
    public class ObjectTrackingPipelineConfiguration : ObservableObject
    {
        private ObjectDetectorType objectDetectorType = ObjectDetectorType.Detic;

        /// <summary>
        /// Gets or sets a value indicating which object detector to use.
        /// </summary>
        [DataMember]
        public ObjectDetectorType ObjectDetectorType
        {
            get => this.objectDetectorType;
            set { this.Set(nameof(this.ObjectDetectorType), ref this.objectDetectorType, value); }
        }
    }
}

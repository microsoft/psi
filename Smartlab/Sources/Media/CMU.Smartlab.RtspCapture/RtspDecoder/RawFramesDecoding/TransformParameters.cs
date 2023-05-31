// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System.Drawing;

    /// <summary>
    /// this component..
    /// </summary>
    public class TransformParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformParameters"/> class.
        /// this component..
        /// </summary>
        /// <param name="regionOfInterest">.</param>
        /// <param name="targetFrameSize">..</param>
        /// <param name="scalePolicy">...</param>
        /// <param name="targetFormat">....</param>
        /// <param name="scaleQuality">.....</param>
        public TransformParameters(RectangleF regionOfInterest, Size targetFrameSize, ScalingPolicy scalePolicy, PixelFormat targetFormat, ScalingQuality scaleQuality)
        {
            this.RegionOfInterest = regionOfInterest;
            this.TargetFrameSize = targetFrameSize;
            this.TargetFormat = targetFormat;
            this.ScaleQuality = scaleQuality;
            this.ScalePolicy = scalePolicy;
        }

        /// <summary>
        /// Gets...
        /// </summary>
        public RectangleF RegionOfInterest { get; }

        /// <summary>
        /// Gets...
        /// </summary>
        public Size TargetFrameSize { get; }

        /// <summary>
        /// Gets...
        /// </summary>
        public ScalingPolicy ScalePolicy { get; }

        /// <summary>
        /// Gets...
        /// </summary>
        public PixelFormat TargetFormat { get; }

        /// <summary>
        /// Gets...
        /// </summary>
        public ScalingQuality ScaleQuality { get; }

        /// <summary>
        /// this component..
        /// </summary>
        /// <param name="obj">.</param>
        /// <returns>...</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return this.Equals((TransformParameters)obj);
        }

        /// <summary>
        /// ....
        /// </summary>
        /// <returns>..</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.RegionOfInterest.GetHashCode();
                hashCode = (hashCode * 397) ^ this.TargetFrameSize.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)this.TargetFormat;
                hashCode = (hashCode * 397) ^ (int)this.ScaleQuality;
                return hashCode;
            }
        }

        /// <summary>
        /// this component..
        /// </summary>
        /// <param name="other">.</param>
        /// <returns>..</returns>
        protected bool Equals(TransformParameters other)
        {
            return this.RegionOfInterest.Equals(other.RegionOfInterest) &&
                   this.TargetFrameSize.Equals(other.TargetFrameSize) &&
                   this.TargetFormat == other.TargetFormat && this.ScaleQuality == other.ScaleQuality;
        }
    }
}
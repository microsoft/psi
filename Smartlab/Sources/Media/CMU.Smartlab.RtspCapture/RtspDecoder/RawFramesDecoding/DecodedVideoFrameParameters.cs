// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    /// <summary>
    /// Component that..
    /// </summary>
    public class DecodedVideoFrameParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DecodedVideoFrameParameters"/> class.
        /// Component that..
        /// </summary>
        /// <param name="width">.</param>
        /// <param name="height">..</param>
        /// <param name="pixelFormat">...</param>
        public DecodedVideoFrameParameters(int width, int height, FFmpegPixelFormat pixelFormat)
        {
            this.Width = width;
            this.Height = height;
            this.PixelFormat = pixelFormat;
        }

        /// <summary>
        /// Gets the ....
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Gets the ....
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Gets the ....
        /// </summary>
        public FFmpegPixelFormat PixelFormat { get; }

        /// <summary>
        /// Component that..
        /// </summary>
        /// <param name="obj">..</param>
        /// <returns>.</returns>
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

            return this.Equals((DecodedVideoFrameParameters)obj);
        }

        /// <summary>
        ///  Component that..
        /// </summary>
        /// <returns>..</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = this.Width;
                hashCode = (hashCode * 397) ^ this.Height;
                hashCode = (hashCode * 397) ^ (int)this.PixelFormat;
                return hashCode;
            }
        }

        /// <summary>
        /// Component that..
        /// </summary>
        /// <param name="other">.</param>
        /// <returns>..</returns>
        protected bool Equals(DecodedVideoFrameParameters other)
        {
            return this.Width == other.Width && this.Height == other.Height && this.PixelFormat == other.PixelFormat;
        }
    }
}
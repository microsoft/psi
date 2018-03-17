// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// DepthToColorConverter defines a component for converting a depth image from the Kinect
    /// into a color image (where more distant objects are blue, and closer objects are reddish)
    /// </summary>
    public class DepthToColorConverter : ConsumerProducer<Shared<Image>, Shared<Image>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepthToColorConverter"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline this component is a part of</param>
        public DepthToColorConverter(Pipeline pipeline)
           : base(pipeline)
        {
        }

        /// <summary>
        /// Pipeline callback for converting depth image to colored image
        /// </summary>
        /// <param name="depthImage">Depth image</param>
        /// <param name="e">Pipeline information about current depthImage sample</param>
        protected override void Receive(Shared<Image> depthImage, Envelope e)
        {
            using (var colorImageDest = ImagePool.GetOrCreate(depthImage.Resource.Width, depthImage.Resource.Height, Imaging.PixelFormat.BGR_24bpp))
            {
                unsafe
                {
                    ushort maxDepth = ushort.MaxValue;
                    ushort minDepth = 0;

                    Parallel.For(0, depthImage.Resource.Height, iy =>
                    {
                        ushort* src = (ushort*)((byte*)depthImage.Resource.ImageData.ToPointer() + (iy * depthImage.Resource.Stride));
                        byte* dst = (byte*)colorImageDest.Resource.ImageData.ToPointer() + (iy * colorImageDest.Resource.Stride);

                        for (int ix = 0; ix < depthImage.Resource.Width; ix++)
                        {
                            ushort depth = *src;

                            // short adaptation
                            int normalizedDepth = (depth >= minDepth && depth <= maxDepth) ? (depth * 1024 / 8000) : 0;
                            dst[0] = (byte)this.Saturate(384 - (int)Math.Abs(normalizedDepth - 256));
                            dst[1] = (byte)this.Saturate(384 - (int)Math.Abs(normalizedDepth - 512));
                            dst[2] = (byte)this.Saturate(384 - (int)Math.Abs(normalizedDepth - 768));

                            dst += 3;
                            src += 1;
                        }
                    });
                }

                this.Out.Post(colorImageDest, e.OriginatingTime);
            }
        }

        /// <summary>
        /// Simple saturate (clamp between 0..255) helper
        /// </summary>
        /// <param name="value">Value to saturate</param>
        /// <returns>Clamped color value</returns>
        private float Saturate(int value)
        {
            return Math.Max(0f, Math.Min(255, value));
        }
    }
}
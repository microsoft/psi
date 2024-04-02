// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Imaging
{
    /// <summary>
    /// Implements methods for registering platform specific resources.
    /// </summary>
    public static class Resources
    {
        /// <summary>
        /// Registers platform specific image encoders and decoders.
        /// </summary>
        public static void RegisterPlatformResources()
        {
            PlatformResources.RegisterDefault<IImageFromStreamDecoder>(new ImageFromStreamDecoder());
            PlatformResources.RegisterDefault<IDepthImageFromStreamDecoder>(new DepthImageFromStreamDecoder());
            PlatformResources.RegisterDefault<IImageToStreamEncoder>(new ImageToJpegStreamEncoder());
            PlatformResources.Register<IImageToStreamEncoder>(nameof(ImageToJpegStreamEncoder), new ImageToJpegStreamEncoder());
            PlatformResources.Register<IImageToStreamEncoder>(nameof(ImageToPngStreamEncoder), new ImageToPngStreamEncoder());
            PlatformResources.RegisterDefault<IDepthImageToStreamEncoder>(new DepthImageToPngStreamEncoder());
            PlatformResources.Register<IDepthImageToStreamEncoder>(nameof(DepthImageToPngStreamEncoder), new DepthImageToPngStreamEncoder());
            PlatformResources.Register<IDepthImageToStreamEncoder>(nameof(DepthImageToTiffStreamEncoder), new DepthImageToTiffStreamEncoder());
        }
    }
}

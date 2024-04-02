// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using global::StereoKit;
    using Windows.Storage;
    using Windows.Storage.Streams;

    /// <summary>
    /// Implements model import methods for StereoKit on HoloLens.
    /// </summary>
    public static class ModelImporter
    {
        /// <summary>
        /// Create a StereoKit <see cref="Model"/> from a file located in a HoloLens folder.
        /// </summary>
        /// <param name="modelFileName">The file to load, including its extension and path relative to the folder location.</param>
        /// <param name="folderLocation">The HoloLens folder where the model file is located.
        /// Make sure your app has been granted the capability to access this folder in its Package.appxmanifest file.</param>
        /// <returns>A StereoKit <see cref="Model"/>.</returns>
        /// <remarks> Make sure to only invoke this method *after* StereoKit has been initialized.
        /// Supported file types include .obj, .stl, .ply (ASCII), .gltf, or .glb.
        /// It won't work well on files that reference other files, such as .gltf files with references in them.</remarks>
        public static Model FromFile(string modelFileName, StorageFolder folderLocation)
        {
            async Task<IBuffer> ReadBufferAsync()
            {
                var meshFile = await folderLocation.GetFileAsync(modelFileName);
                return await FileIO.ReadBufferAsync(meshFile);
            }

            var modelBuffer = ReadBufferAsync().GetAwaiter().GetResult();
            return Model.FromMemory(modelFileName, modelBuffer.ToArray());
        }
    }
}
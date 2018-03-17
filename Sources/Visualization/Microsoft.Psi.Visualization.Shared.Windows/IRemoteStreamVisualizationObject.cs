// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if COM_SERVER
namespace Microsoft.Psi.Visualization.Server
#elif COM_CLIENT
namespace Microsoft.Psi.Visualization.Client
#endif
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents a remote stream visualization object.
    /// </summary>
    [Guid(Guids.IRemoteStreamVisualizationObjectIIDString)]
#if COM_SERVER
    [ComVisible(true)]
#elif COM_CLIENT
    [ComImport]
#endif
    public interface IRemoteStreamVisualizationObject
    {
        /// <summary>
        /// Close the underlying stream.
        /// </summary>
        void CloseStream();

        /// <summary>
        /// Open a stream, closing underlying stream if neeeded.
        /// </summary>
        /// <param name="jsonStreamBinding">Stream binding serialized as JSON.</param>
        void OpenStream(string jsonStreamBinding);
    }
}

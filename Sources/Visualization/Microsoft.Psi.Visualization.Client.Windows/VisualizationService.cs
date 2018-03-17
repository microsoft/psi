// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Client
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class implements a generic client proxy for host object of the same name.
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(Guids.RemoteVisualizationServiceCLSIDString)]
    [ComImport]
    public class VisualizationService
    {
    }
}

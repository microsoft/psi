// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Forked from https://github.com/microsoft/automatic-graph-layout/tree/master/GraphLayout/tools/WpfGraphControl

namespace Microsoft.Msagl.WpfGraphControl
{
    /// <summary>
    /// Invalidatable data.
    /// </summary>
    internal interface IInvalidatable
    {
        /// <summary>
        /// Invalidate data.
        /// </summary>
        void Invalidate();
    }
}
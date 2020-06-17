// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Forked from https://github.com/microsoft/automatic-graph-layout/tree/master/GraphLayout/tools/WpfGraphControl

namespace Microsoft.Msagl.WpfGraphControl
{
    ﻿using System.Runtime.InteropServices;

    /// <summary>
    /// Native methods.
    /// </summary>
    ﻿internal static class NativeMethods
    {
        /// <summary>
        /// Get device capabilities.
        /// </summary>
        /// <param name="hdc">Handle to device context.</param>
        /// <param name="nIndex">Item index.</param>
        /// <returns>Device-specific value.</returns>
        [DllImport("GDI32.dll")]
        internal static extern int GetDeviceCaps(int hdc, int nIndex);

        /// <summary>
        /// Get handle to desktop window.
        /// </summary>
        /// <returns>Handle to desktop window.</returns>
        [DllImport("User32.dll")]
        internal static extern int GetDesktopWindow();

        /// <summary>
        /// Get window device context.
        /// </summary>
        /// <param name="hWnd">Window handle.</param>
        /// <returns>Device context.</returns>
        [DllImport("User32.dll")]
        internal static extern int GetWindowDC(int hWnd);

        /// <summary>
        /// Release device context.
        /// </summary>
        /// <param name="hWnd">Window handle.</param>
        /// <param name="hDc">Device context handle.</param>
        /// <returns>Indication of whether released.</returns>
        [DllImport("User32.dll")]
        internal static extern int ReleaseDC(int hWnd, int hDc);
    }
}
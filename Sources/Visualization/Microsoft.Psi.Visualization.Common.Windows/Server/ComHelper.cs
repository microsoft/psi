// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Server
{
    using System;
    using Microsoft.Win32;

    /// <summary>
    /// Represents helper methods used when implementing a COM server.
    /// </summary>
    public static class ComHelper
    {
        /// <summary>
        /// Register the component as a local server.
        /// </summary>
        /// <param name="t">Type of the local server.</param>
        public static void RegasmRegisterLocalServer(Type t)
        {
            if (t == null)
            {
                throw new ArgumentException("The type must be specified.", nameof(t));
            }

            // Open the CLSID key of the component.
            using (RegistryKey keyCLSID = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + t.GUID.ToString("B"), /*writable*/true))
            {
                // Remove the auto-generated InprocServer32 key after registration (REGASM puts it there but we are going out-of-proc).
                keyCLSID.DeleteSubKeyTree("InprocServer32");

                // Create "LocalServer32" under the CLSID key
                using (RegistryKey subkey = keyCLSID.CreateSubKey("LocalServer32"))
                {
                    subkey.SetValue(string.Empty, t.Assembly.Location, RegistryValueKind.String);
                }
            }
        }

        /// <summary>
        /// Unregister the component.
        /// </summary>
        /// <param name="t">Type of the local server.</param>
        public static void RegasmUnregisterLocalServer(Type t)
        {
            if (t == null)
            {
                throw new ArgumentException("The type must be specified.", nameof(t));
            }

            // Delete the CLSID key of the component
            Registry.ClassesRoot.DeleteSubKeyTree(@"CLSID\" + t.GUID.ToString("B"));
        }
    }
}

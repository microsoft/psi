// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System;

    /// <summary>
    /// Helper class for type resolution.
    /// </summary>
    internal static class TypeResolutionHelper
    {
        /// <summary>
        /// Finds a type in the loaded assemblies.
        /// </summary>
        /// <param name="typeName">The name of the type to find.</param>
        /// <returns>The type.</returns>
        internal static Type FindType(string typeName)
        {
            var result = Type.GetType(typeName);
            if (result == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    result = assembly.GetType(typeName);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}

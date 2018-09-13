// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    /// <summary>
    /// Represents the common elements of Annotation data stores.
    /// </summary>
    public static class AnnotationStoreCommon
    {
        /// <summary>
        /// Default extension for Annotation stores.
        /// </summary>
        public const string DefaultExtension = ".pas";

        private static readonly string DefinitionFileName = "Definition";

        /// <summary>
        /// Gets the definition file name for the specified application.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <returns>The definition file name.</returns>
        public static string GetDefinitionFileName(string appName)
        {
            return appName + "." + DefinitionFileName;
        }
    }
}

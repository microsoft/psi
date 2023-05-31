// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common
{
    /// <summary>
    /// Represents the security warning text displayed to the user
    /// when they elect to load additional third-party assemblies.
    /// </summary>
    public static class AdditionalAssembliesWarning
    {
        /// <summary>
        /// The warning title.
        /// </summary>
        public const string Title = "Third-Party Component Security Warning";

        /// <summary>
        /// The first line of the warning message, above the list of assemblies.
        /// </summary>
        public const string Line1 = "You have chosen to add the following third-party components to {0}. These components and their dependencies will be able to execute their own code on your machine in the context of the {0} user.";

        /// <summary>
        /// The first line of the warning message, below the list of assemblies.
        /// </summary>
        public const string Line2 = "These components were not written by Microsoft and have not been verified to be free from security vulnerabilities, nor have they been scanned by us for the presence of malware. Before continuing you should verify that these components have come from a trusted source.";

        /// <summary>
        /// The question asking the user if they wish to proceed.
        /// </summary>
        public const string Question = "Are you sure you wish to add these components to {0}?";
    }
}

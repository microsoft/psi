// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Interop.Serialization
{
    using System.IO;

    /// <summary>
    /// Defines an interface for interop serializable objects.
    /// </summary>
    public interface IInteropSerializable
    {
        /// <summary>
        /// Writes the object to a binary writer.
        /// </summary>
        /// <param name="writer">The binary writer.</param>
        void Write(BinaryWriter writer);

        /// <summary>
        /// Reads the object from a binary reader.
        /// </summary>
        /// <param name="reader">The binary reader.</param>
        void ReadFrom(BinaryReader reader);
    }
}

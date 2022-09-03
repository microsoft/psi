// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Common
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Runtime info metadata.
    /// </summary>
    public class RuntimeInfo : Metadata
    {
        /// <summary>
        /// The current version of the serialization subsystem. This is the default.
        /// </summary>
        public const int CurrentRuntimeVersion = 2;

        /// <summary>
        /// Gets name of the executing assembly.
        /// </summary>
        public static readonly AssemblyName RuntimeName = Assembly.GetExecutingAssembly().GetName();

        /// <summary>
        /// Gets the current runtime info.
        /// </summary>
        public static readonly RuntimeInfo Current = new ();

        internal RuntimeInfo(int serializationSystemVersion = CurrentRuntimeVersion)
            : this(
                  name: RuntimeName.FullName,
                  id: 0,
                  typeName: default(string),
                  version: (RuntimeName.Version.Major << 16) | RuntimeName.Version.Minor,
                  serializerTypeName: default(string),
                  serializerVersion: serializationSystemVersion)
        {
        }

        internal RuntimeInfo(string name, int id, string typeName, int version, string serializerTypeName, int serializerVersion)
        : base(MetadataKind.RuntimeInfo, name, id, typeName, version, serializerTypeName, serializerVersion, 0)
        {
        }
    }
}

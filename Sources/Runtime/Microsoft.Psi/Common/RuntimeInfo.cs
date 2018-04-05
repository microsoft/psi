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
        /// Gets the NetBIOS name of the local machine.
        /// </summary>
        public static readonly string MachineName = Environment.MachineName;

        /// <summary>
        /// Get the command line for the process.
        /// </summary>
        public static readonly string CmdLine = Environment.CommandLine;

        /// <summary>
        /// Gets name of the executing assembly.
        /// </summary>
        public static readonly AssemblyName RuntimeName = Assembly.GetExecutingAssembly().GetName();

        /// <summary>
        /// Gets the current runtime info.
        /// </summary>
        public static readonly RuntimeInfo Current = new RuntimeInfo();

        internal RuntimeInfo(int serializationSystemVersion = CurrentRuntimeVersion)
            : this(
                  name: CmdLine,
                  id: 0,
                  typeName: RuntimeName.FullName,
                  version: (RuntimeName.Version.Major << 16) | RuntimeName.Version.Minor,
                  serializerTypeName: MachineName,
                  serializerVersion: serializationSystemVersion)
        {
        }

        internal RuntimeInfo(string name, int id, string typeName, int version, string serializerTypeName, int serializerVersion)
        : base(MetadataKind.RuntimeInfo, name, id, typeName, version, serializerTypeName, serializerVersion, 0)
        {
        }
    }
}

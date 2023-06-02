// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Helpers
{
    using System;
    using System.Diagnostics;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Represents a JSON serialization binder that will only deserialize
    /// types in assemblies referenced directly by the application or
    /// assemblies that have been allowed to load by the user.
    /// </summary>
    public class SafeSerializationBinder : ISerializationBinder
    {
        /// <inheritdoc/>
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = serializedType.Assembly.FullName;
            typeName = serializedType.FullName;
            Debug.WriteLine("CACA: ", serializedType, typeName);
        }

        /// <inheritdoc/>
        public Type BindToType(string assemblyName, string typeName)
        {
            return TypeResolutionHelper.GetVerifiedType($"{typeName}, {assemblyName}");
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Serialization
{
    /// <summary>
    /// Provides a base class for authoring backwards compatible custom serializers (for reading) for class types.
    /// </summary>
    /// <typeparam name="T">The type of objects handled by the custom serializer.</typeparam>
    public abstract class BackCompatClassSerializer<T> : BackCompatSerializer<T>
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackCompatClassSerializer{T}"/> class.
        /// </summary>
        /// <param name="schemaVersion">The current schema version.</param>
        public BackCompatClassSerializer(int schemaVersion)
            : base(schemaVersion, new ClassSerializer<T>())
        {
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Json
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents the common elements of JSON data stores.
    /// </summary>
    public abstract class JsonStoreBase : IDisposable
    {
        /// <summary>
        /// Default extension for the underlying file.
        /// </summary>
        public const string DefaultExtension = ".json";

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStoreBase"/> class.
        /// </summary>
        /// <param name="extension">The extension for the underlying file.</param>
        protected JsonStoreBase(string extension)
        {
            this.Extension = extension;
            this.Serializer = new JsonSerializer();
        }

        /// <summary>
        /// Gets or sets the underlying file extension.
        /// </summary>
        public string Extension { get; protected set; }

        /// <summary>
        /// Gets or sets the name of the data store.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the path of the data store.
        /// </summary>
        public string Path { get; protected set; }

        /// <summary>
        /// Gets or sets the JSON serializer.
        /// </summary>
        public JsonSerializer Serializer { get; protected set; }

        /// <inheritdoc />
        public abstract void Dispose();
    }
}

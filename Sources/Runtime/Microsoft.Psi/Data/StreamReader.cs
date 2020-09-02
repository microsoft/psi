// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data
{
    using System;

    /// <summary>
    /// Represents factory for dynamically creating stream readers.
    /// </summary>
    public static class StreamReader
    {
        /// <summary>
        /// Create instance of stream reader (assumes ctor taking name and path.
        /// </summary>
        /// <param name="storeName">Store name.</param>
        /// <param name="storePath">Store path.</param>
        /// <param name="streamReaderType">Stream reader type.</param>
        /// <returns>Stream reader instance.</returns>
        public static IStreamReader Create(string storeName, string storePath, Type streamReaderType)
        {
            streamReaderType ??= typeof(PsiStoreStreamReader);
            return (IStreamReader)streamReaderType.GetConstructor(
                new Type[] { typeof(string), typeof(string) })
                .Invoke(new object[] { storeName, storePath });
        }

        /// <summary>
        /// Create instance of stream reader (assumes ctor taking name and path.
        /// </summary>
        /// <param name="storeName">Store name.</param>
        /// <param name="storePath">Store path.</param>
        /// <param name="streamReaderTypeName">Stream reader type name.</param>
        /// <returns>Stream reader instance.</returns>
        public static IStreamReader Create(string storeName, string storePath, string streamReaderTypeName)
        {
            return Create(storeName, storePath, TypeResolutionHelper.GetVerifiedType(streamReaderTypeName));
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Extension methods that simplify operator usage
    /// </summary>
    public static partial class Operators
    {
        public static IProducer<T> Name<T>(this IProducer<T> source, string name)
        {
            source.Out.Name = name;
            return source;
        }
    }
}

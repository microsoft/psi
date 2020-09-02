// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    /// <summary>
    /// Represents an annotation schema.
    /// </summary>
    public interface IAnnotationSchema
    {
        /// <summary>
        /// Gets the name of the schema.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the annotation schema is a finite annotation schema.
        /// </summary>
        bool IsFiniteAnnotationSchema { get; }
    }
}

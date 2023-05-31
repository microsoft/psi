// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines an enumerable annotation value schema, i.e., a value schema with
    /// a fixed set of possible values.
    /// </summary>
    public interface IEnumerableAnnotationValueSchema : IAnnotationValueSchema
    {
        /// <summary>
        /// Gets the set of possible values for this annotation value schema.
        /// </summary>
        /// <returns>The set of possible values.</returns>
        public IEnumerable<IAnnotationValue> GetPossibleAnnotationValues();
    }
}

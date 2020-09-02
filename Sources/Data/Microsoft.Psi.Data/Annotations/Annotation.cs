// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Represents an annotation instance.
    /// </summary>
    public class Annotation
    {
        /// <summary>
        /// Gets or sets the collection of values in the annotation.
        /// </summary>
        public Dictionary<string, object> Values { get; set; }
    }
}
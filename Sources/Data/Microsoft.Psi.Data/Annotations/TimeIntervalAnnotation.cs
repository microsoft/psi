// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an instance of a time interval annotation.
    /// </summary>
    public class TimeIntervalAnnotation : Annotation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalAnnotation"/> class.
        /// </summary>
        /// <param name="interval">The interval over which the annotation occurs.</param>
        /// <param name="values">The list of values for the annotation.</param>
        public TimeIntervalAnnotation(TimeInterval interval, Dictionary<string, object> values)
        {
            this.Interval = interval;
            this.Values = values;
        }

        /// <summary>
        /// Gets or sets the interval over which this annotation occurs.
        /// </summary>
        public TimeInterval Interval { get; set; }
    }
}

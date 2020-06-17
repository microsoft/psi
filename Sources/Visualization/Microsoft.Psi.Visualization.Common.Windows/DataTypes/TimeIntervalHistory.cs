// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.DataTypes
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an indexed history of time intervals.
    /// </summary>
    [Serializable]
    public class TimeIntervalHistory : Dictionary<string, List<(TimeInterval, string, System.Drawing.Color?)>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeIntervalHistory"/> class with serialized data.
        /// </summary>
        /// <remarks>
        /// This is the serialization constructor. Satisfies rule CA2229: ImplementSerializationConstructors.
        /// </remarks>
        /// <param name="serializationInfo">The serialization info.</param>
        /// <param name="streamingContext">The streaming context.</param>
        protected TimeIntervalHistory(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of booleans into doubles.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class BoolAdapter : StreamAdapter<bool, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoolAdapter"/> class.
        /// </summary>
        public BoolAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(bool value, Envelope env)
        {
            return value ? 1.0 : 0.0;
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of integers into doubles.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class IntAdapter : StreamAdapter<int, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntAdapter"/> class.
        /// </summary>
        public IntAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(int value, Envelope env)
        {
            return (double)value;
        }
    }
}
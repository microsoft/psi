// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of floats into doubles.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class FloatAdapter : StreamAdapter<float, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatAdapter"/> class.
        /// </summary>
        public FloatAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(float value, Envelope env)
        {
            return (double)value;
        }
    }
}
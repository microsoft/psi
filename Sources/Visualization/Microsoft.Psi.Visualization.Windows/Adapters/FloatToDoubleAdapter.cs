// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of floats into doubles.
    /// </summary>
    [StreamAdapter]
    public class FloatToDoubleAdapter : StreamAdapter<float, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FloatToDoubleAdapter"/> class.
        /// </summary>
        public FloatToDoubleAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(float value, Envelope env)
        {
            return (double)value;
        }
    }
}
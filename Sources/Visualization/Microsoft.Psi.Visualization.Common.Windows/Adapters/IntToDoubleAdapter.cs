// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of integers into doubles.
    /// </summary>
    [StreamAdapter]
    public class IntToDoubleAdapter : StreamAdapter<int, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntToDoubleAdapter"/> class.
        /// </summary>
        public IntToDoubleAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(int value, Envelope env)
        {
            return (double)value;
        }
    }
}
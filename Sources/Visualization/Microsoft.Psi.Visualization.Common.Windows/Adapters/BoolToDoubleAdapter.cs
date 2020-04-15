// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of booleans into doubles.
    /// </summary>
    [StreamAdapter]
    public class BoolToDoubleAdapter : StreamAdapter<bool, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BoolToDoubleAdapter"/> class.
        /// </summary>
        public BoolToDoubleAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(bool value, Envelope env)
        {
            return value ? 1.0 : 0.0;
        }
    }
}
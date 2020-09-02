// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Converters
{
    using System.Windows.Data;

    /// <summary>
    /// Provides a way to apply custom logic to a binding. Specifically, converting from an integer to a boolean.
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class IntegerToBoolConverter : IntegerToValueConverter<bool>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerToBoolConverter"/> class.
        /// </summary>
        public IntegerToBoolConverter()
        {
            this.NegativeValue = true;
            this.ZeroValue = false;
            this.PositiveValue = true;
        }
    }
}

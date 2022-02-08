// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Provides helper methods for identifying numeric series visualization
    /// objects by type.
    /// </summary>
    internal static class NumericSeriesVisualizationObject
    {
        private static readonly Dictionary<Type, Type> NumericTypeToSeriesVisualizationObjectType = new ()
        {
            { typeof(bool), typeof(BoolSeriesVisualizationObject) },
            { typeof(decimal), typeof(DecimalSeriesVisualizationObject) },
            { typeof(double), typeof(DoubleSeriesVisualizationObject) },
            { typeof(float), typeof(FloatSeriesVisualizationObject) },
            { typeof(int), typeof(IntSeriesVisualizationObject) },
            { typeof(long), typeof(LongSeriesVisualizationObject) },
            { typeof(bool?), typeof(NullableBoolSeriesVisualizationObject) },
            { typeof(decimal?), typeof(NullableDecimalSeriesVisualizationObject) },
            { typeof(double?), typeof(NullableDoubleSeriesVisualizationObject) },
            { typeof(float?), typeof(NullableFloatSeriesVisualizationObject) },
            { typeof(int?), typeof(NullableIntSeriesVisualizationObject) },
            { typeof(long?), typeof(NullableLongSeriesVisualizationObject) },
        };

        /// <summary>
        /// Gets the type of the series visualization object for a specified numeric type, or null otherwise.
        /// </summary>
        /// <param name="type">The numeric type to get the series visualization object type for.</param>
        /// <returns>The series visualization object type.</returns>
        internal static Type GetSeriesVisualizationObjectTypeByNumericType(Type type)
            => NumericTypeToSeriesVisualizationObjectType.ContainsKey(type) ? NumericTypeToSeriesVisualizationObjectType[type] : null;
    }
}

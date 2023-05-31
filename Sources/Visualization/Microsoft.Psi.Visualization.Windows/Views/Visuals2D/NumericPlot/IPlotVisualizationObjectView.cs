// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Defines a plot visualization object view.
    /// </summary>
    /// <typeparam name="TKey">The type of the series key.</typeparam>
    /// <typeparam name="TData">The type of data.</typeparam>
    /// <remarks>
    /// This interface is used to abstract over the two different types of plot visualization
    /// object views currently available: <see cref="PlotVisualizationObjectView{TPlotVisualizationObject, TData}"/>,
    /// which is used for simple numerical plots, and
    /// <see cref="PlotSeriesVisualizationObjectView{TPlotVisualizationObject, TKey, TData}"/>,
    /// which is used for plot series. The interface enables encapsulating the common
    /// functionality for creating plot views in the
    /// <see cref="PlotVisualizationObjectViewHelper{TKey, TData}"/> class such as to avoid
    /// code duplication.
    /// </remarks>
    public interface IPlotVisualizationObjectView<TKey, TData>
    {
        /// <summary>
        /// Gets the navigator.
        /// </summary>
        public Navigator Navigator { get; }

        /// <summary>
        /// Gets the canvas.
        /// </summary>
        public Canvas Canvas { get; }

        /// <summary>
        /// Gets the transform group.
        /// </summary>
        public Transform TransformGroup { get; }

        /// <summary>
        /// Gets the interpolation style.
        /// </summary>
        public InterpolationStyle InterpolationStyle { get; }

        /// <summary>
        /// Gets the marker style.
        /// </summary>
        public MarkerStyle MarkerStyle { get; }

        /// <summary>
        /// Gets the marker size.
        /// </summary>
        public double MarkerSize { get; }

        /// <summary>
        /// Gets the scale transform.
        /// </summary>
        public ScaleTransform ScaleTransform { get; }

        /// <summary>
        /// Creates the bindings for the line, markers and range paths for a specified series key.
        /// </summary>
        /// <param name="seriesKey">The series key.</param>
        /// <param name="linePath">The line path.</param>
        /// <param name="markerPath">The marker path.</param>
        /// <param name="rangePath">The range path.</param>
        public void CreateBindings(TKey seriesKey, Path linePath, Path markerPath, Path rangePath);

        /// <summary>
        /// Gets the data points to render for a given series.
        /// </summary>
        /// <param name="seriesKey">The series key.</param>
        /// <returns>An enumeration of datapoints to render.</returns>
        public IEnumerable<(DateTime OriginatingTime, TData Value, bool Available)> GetDataPoints(TKey seriesKey);

        /// <summary>
        /// Gets the numeric value.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The corresponding numberic value.</returns>
        public double GetNumericValue(TData data);
    }
}

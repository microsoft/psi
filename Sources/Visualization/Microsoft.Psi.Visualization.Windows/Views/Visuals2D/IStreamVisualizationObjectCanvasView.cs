// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// Defines an untyped interface for a stream visualization object canvas view.
    /// </summary>
    public interface IStreamVisualizationObjectCanvasView
    {
        /// <summary>
        /// Gets the dynamic canvas element.
        /// </summary>
        public Canvas Canvas { get; }

        /// <summary>
        /// Gets the transform group.
        /// </summary>
        public TransformGroup TransformGroup { get; }

        /// <summary>
        /// Gets the scale transform.
        /// </summary>
        public ScaleTransform ScaleTransform { get; }

        /// <summary>
        /// Gets the translate transform.
        /// </summary>
        public TranslateTransform TranslateTransform { get; }
    }
}

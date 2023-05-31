// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Forked from https://github.com/microsoft/automatic-graph-layout/tree/master/GraphLayout/tools/WpfGraphControl

namespace Microsoft.Msagl.WpfGraphControl
{
    using System.Windows;
    using System.Windows.Media;
    using MsaglColor = Microsoft.Msagl.Drawing.Color;
    using MsaglPoint = Microsoft.Msagl.Core.Geometry.Point;
    using WpfPoint = System.Windows.Point;

    /// <summary>
    /// Common utilities.
    /// </summary>
    internal class Common
    {
        /// <summary>
        /// Convert Msagl color to WPF brush.
        /// </summary>
        /// <param name="color">Msagl color.</param>
        /// <returns>WPF brush.</returns>
        public static Brush BrushFromMsaglColor(MsaglColor color)
        {
            Color avalonColor = new Color { A = color.A, B = color.B, G = color.G, R = color.R };
            return new SolidColorBrush(avalonColor);
        }

        /// <summary>
        /// Convert Msagl color components to WPF brush.
        /// </summary>
        /// <param name="colorA">Alpha component.</param>
        /// <param name="colorR">Red component.</param>
        /// <param name="colorG">Green component.</param>
        /// <param name="colorB">Blue component.</param>
        /// <returns>WPF brush.</returns>
        public static Brush BrushFromMsaglColor(byte colorA, byte colorR, byte colorG, byte colorB)
        {
            Color avalonColor = new Color { A = colorA, R = colorR, G = colorG, B = colorB };
            return new SolidColorBrush(avalonColor);
        }

        /// <summary>
        /// Convert Msagl point to WPF point.
        /// </summary>
        /// <param name="p">Msagl point.</param>
        /// <returns>WPF point.</returns>
        internal static WpfPoint WpfPoint(MsaglPoint p)
        {
            return new WpfPoint(p.X, p.Y);
        }

        /// <summary>
        /// Convert WPF point to Msagl point.
        /// </summary>
        /// <param name="p">WPF point.</param>
        /// <returns>Msagl point.</returns>
        internal static MsaglPoint MsaglPoint(WpfPoint p)
        {
            return new MsaglPoint(p.X, p.Y);
        }

        /// <summary>
        /// Position element to center point.
        /// </summary>
        /// <param name="frameworkElement">Element to position.</param>
        /// <param name="center">Center point.</param>
        /// <param name="scale">Zoom level.</param>
        internal static void PositionFrameworkElement(FrameworkElement frameworkElement, MsaglPoint center, double scale)
        {
            PositionFrameworkElement(frameworkElement, center.X, center.Y, scale);
        }

        /// <summary>
        /// Position element to center point.
        /// </summary>
        /// <param name="frameworkElement">Element to position.</param>
        /// <param name="x">Center x.</param>
        /// <param name="y">Center y.</param>
        /// <param name="scale">Zoom level.</param>
        private static void PositionFrameworkElement(FrameworkElement frameworkElement, double x, double y, double scale)
        {
            if (frameworkElement != null)
            {
                frameworkElement.RenderTransform = new MatrixTransform(new Matrix(scale, 0, 0, -scale, x - scale * frameworkElement.Width / 2, y + scale * frameworkElement.Height / 2));
            }
        }
    }
}
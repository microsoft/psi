// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Forked from https://github.com/microsoft/automatic-graph-layout/tree/master/GraphLayout/tools/WpfGraphControl

namespace Microsoft.Msagl.WpfGraphControl
{
    ﻿using System.Windows;
    ﻿using System.Windows.Input;
    ﻿using Microsoft.Msagl.Drawing;

    /// <summary>
    /// Graph view mouse event args.
    /// </summary>
    ﻿internal class GvMouseEventArgs : MsaglMouseEventArgs
    {
        private MouseEventArgs args;
        private Point position;

        /// <summary>
        /// Initializes a new instance of the <see cref="GvMouseEventArgs"/> class.
        /// </summary>
        /// <param name="argsPar">Mouse event args.</param>
        /// <param name="graphScrollerP">Graph viewer.</param>
        internal GvMouseEventArgs(MouseEventArgs argsPar, GraphViewer graphScrollerP)
        {
            this.args = argsPar;
            this.position = this.args.GetPosition((IInputElement)graphScrollerP.GraphCanvas.Parent);
        }

        /// <summary>
        /// Gets a value indicating whether left mouse button is pressed.
        /// </summary>
        public override bool LeftButtonIsPressed
        {
            get { return this.args.LeftButton == MouseButtonState.Pressed; }
        }

        /// <summary>
        /// Gets a value indicating whether middle mouse button is pressed.
        /// </summary>
        public override bool MiddleButtonIsPressed
        {
            get { return this.args.MiddleButton == MouseButtonState.Pressed; }
        }

        /// <summary>
        /// Gets a value indicating whether right mouse button is pressed.
        /// </summary>
        public override bool RightButtonIsPressed
        {
            get { return this.args.RightButton == MouseButtonState.Pressed; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the event is handled.
        /// </summary>
        public override bool Handled
        {
            get { return this.args.Handled; }
            set { this.args.Handled = value; }
        }

        /// <summary>
        /// Gets mouse X position.
        /// </summary>
        public override int X
        {
            get { return (int)this.position.X; }
        }

        /// <summary>
        /// Gets mouse Y position.
        /// </summary>
        public override int Y
        {
            get { return (int)this.position.Y; }
        }

        /// <summary>
        /// Gets number of clicks.
        /// </summary>
        public override int Clicks
        {
            get
            {
                var e = this.args as MouseButtonEventArgs;
                return e != null ? e.ClickCount : 0;
            }
        }
    }
}
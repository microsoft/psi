// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
// Forked from https://github.com/microsoft/automatic-graph-layout/tree/master/GraphLayout/tools/WpfGraphControl

namespace Microsoft.Msagl.WpfGraphControl
{
    ﻿using System;
    ﻿using System.Windows;
    ﻿using System.Windows.Threading;

    /// <summary>
    /// Mouse click counter.
    /// </summary>
    ﻿internal class ClickCounter
    {
        private readonly Func<Point> mousePosition;
        private readonly DispatcherTimer clickTimer = new DispatcherTimer();

        private Point lastDownClickPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickCounter"/> class.
        /// </summary>
        /// <param name="mousePosition">Position of mouse pointer.</param>
        internal ClickCounter(Func<Point> mousePosition)
        {
            this.mousePosition = mousePosition;
            this.clickTimer.Tick += this.TimeTick;
            this.clickTimer.Interval = TimeSpan.FromMilliseconds(500);
        }

        /// <summary>
        /// Time elapsed event.
        /// </summary>
        public event EventHandler<EventArgs> Elapsed;

        /// <summary>
        /// Gets or sets clicked object.
        /// </summary>
        internal object ClickedObject { get; set; }

        /// <summary>
        /// Gets a value indicating whether count is running.
        /// </summary>
        internal bool IsRunning { get; private set; }

        /// <summary>
        /// Gets mouse down count.
        /// </summary>
        internal int DownCount { get; private set; }

        /// <summary>
        /// Gets mouse up count.
        /// </summary>
        internal int UpCount { get; private set; }

        /// <summary>
        /// Add mouse down event.
        /// </summary>
        /// <param name="objectUnderMouseCursor">Object currently under the mouse cursor.</param>
        internal void AddMouseDown(object objectUnderMouseCursor)
        {
            if (!this.IsRunning)
            {
                this.DownCount = 0;
                this.UpCount = 0;
                this.clickTimer.Start();
                this.IsRunning = true;
            }

            this.lastDownClickPosition = this.mousePosition();
            this.ClickedObject = objectUnderMouseCursor;
            this.DownCount++;
        }

        /// <summary>
        /// Add mouse up event.
        /// </summary>
        internal void AddMouseUp()
        {
            const double minDistanceForClickDownAndUp = 0.1;
            if (this.IsRunning)
            {
                if ((this.mousePosition() - this.lastDownClickPosition).Length > minDistanceForClickDownAndUp)
                {
                    // it is not a click
                    this.UpCount = 0;
                    this.DownCount = 0;
                    this.clickTimer.Stop();
                    this.IsRunning = false;
                }
                else
                {
                    this.UpCount++;
                }
            }
        }

        private void TimeTick(object sender, EventArgs e)
        {
            this.clickTimer.Stop();
            this.IsRunning = false;
            this.OnElapsed();
        }

        private void OnElapsed()
        {
            this?.Elapsed(this, EventArgs.Empty);
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System.Diagnostics;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents a frame counter (FPS).
    /// </summary>
    public class FrameCounter : ObservableObject
    {
        private readonly Stopwatch stopwatch = new ();
        private long lastTicks;
        private float averageTicksPerFrame;
        private int rate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameCounter"/> class.
        /// </summary>
        public FrameCounter()
        {
            Debug.Assert(Stopwatch.IsHighResolution, "Stopwatch must be high resolution.");
            this.Reset();
        }

        /// <summary>
        /// Gets the frame rate.
        /// </summary>
        public int Rate
        {
            get => this.rate;
            private set
            {
                if (value != this.rate)
                {
                    this.Set(nameof(this.Rate), ref this.rate, value);
                }
            }
        }

        /// <summary>
        /// Increments the frame rate.
        /// </summary>
        public void Increment()
        {
            long currentTicks = this.stopwatch.ElapsedTicks;
            long elapsedTicks = currentTicks - this.lastTicks;
            this.lastTicks = currentTicks;
            float blendFactor = 0.1f;
            this.averageTicksPerFrame = (this.averageTicksPerFrame * (1 - blendFactor)) + (elapsedTicks * blendFactor);
            this.Rate = (int)(Stopwatch.Frequency / this.averageTicksPerFrame);
        }

        private void Reset()
        {
            this.stopwatch.Start();
            this.lastTicks = 0;
            this.averageTicksPerFrame = 0;
        }
    }
}

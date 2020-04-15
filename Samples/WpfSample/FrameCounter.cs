// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Samples.WpfSample
{
    using System.ComponentModel;
    using System.Diagnostics;

    /// <summary>
    /// FrameCounter is a simple class for determining the frame rate of the playback.
    /// </summary>
    public class FrameCounter : INotifyPropertyChanged
    {
        private Stopwatch stopwatch = new Stopwatch();
        private long lastTicks;
        private long averageTicksPerFrame;
        private int rate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrameCounter"/> class.
        /// </summary>
        public FrameCounter()
        {
            Debug.Assert(Stopwatch.IsHighResolution, "High resolution time is required.");
            this.Reset();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the current frame rate.
        /// </summary>
        public int Rate
        {
            get => this.rate;

            private set
            {
                if (value != this.rate)
                {
                    this.rate = value;
                    this.OnPropertyChanged(nameof(this.Rate));
                }
            }
        }

        /// <summary>
        /// Called each time we output a frame.
        /// </summary>
        public void Increment()
        {
            long currentTicks = this.stopwatch.ElapsedTicks;
            long elapsedTicks = currentTicks - this.lastTicks;
            this.lastTicks = currentTicks;
            float blendFactor = 0.1f;
            this.averageTicksPerFrame = (long)((this.averageTicksPerFrame * (1 - blendFactor)) + (elapsedTicks * blendFactor));
            this.Rate = (int)(Stopwatch.Frequency / this.averageTicksPerFrame);
        }

        /// <summary>
        /// Used to reset the frame rate counter.
        /// </summary>
        private void Reset()
        {
            this.stopwatch.Start();
            this.lastTicks = 0;
            this.averageTicksPerFrame = 0;
        }

        /// <summary>
        /// WPF helper for firing an event when a property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

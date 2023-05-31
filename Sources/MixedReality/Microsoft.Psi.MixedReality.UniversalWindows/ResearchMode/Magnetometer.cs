// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.ResearchMode
{
    using HoloLens2ResearchMode;
    using Microsoft.Psi;

    /// <summary>
    /// Source component that publishes magnetometer data on a stream.
    /// </summary>
    public class Magnetometer : ResearchModeImu
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Magnetometer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for the component.</param>
        public Magnetometer(Pipeline pipeline, string name = nameof(Magnetometer))
            : base(pipeline, ResearchModeSensorType.ImuMag, name)
        {
        }

        /// <inheritdoc/>
        protected override void ProcessSensorFrame(IResearchModeSensorFrame sensorFrame)
        {
            this.PostSamples(
                sensorFrame,
                (sensorFrame as ResearchModeMagFrame).GetMagnetometerSamples(),
                f => (f.X, f.Y, f.Z),
                f => f.VinylHupTicks);
        }
    }
}

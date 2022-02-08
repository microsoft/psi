// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
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
        public Magnetometer(Pipeline pipeline)
            : base(pipeline, ResearchModeSensorType.ImuMag)
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

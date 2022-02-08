// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System.Numerics;
    using HoloLens2ResearchMode;
    using Microsoft.Psi;

    /// <summary>
    /// Source component that publishes accelerometer data on a stream.
    /// </summary>
    public class Accelerometer : ResearchModeImu
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Accelerometer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        public Accelerometer(Pipeline pipeline)
            : base(pipeline, ResearchModeSensorType.ImuAccel)
        {
        }

        /// <inheritdoc/>
        protected override void ProcessSensorFrame(IResearchModeSensorFrame sensorFrame)
        {
            this.PostSamples(
                sensorFrame,
                (sensorFrame as ResearchModeAccelFrame).GetCalibratedAccelarationSamples(),
                f => (f.X, f.Y, f.Z),
                f => f.VinylHupTicks);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using StereoKit;
    using StereoKit.Framework;

    /// <summary>
    /// Base abstract class for implementing StereoKit \psi components.
    /// </summary>
    public abstract class StereoKitComponent : IStepper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StereoKitComponent"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        public StereoKitComponent(Pipeline pipeline)
        {
            // Defer call to SK.AddStepper(this) to PipelineRun to ensure derived classes have finished construction!
            // Otherwise IStepper.Initialize() could get called before this object is fully constructed.
            pipeline.PipelineRun += (_, _) =>
            {
                if (SK.AddStepper(this) == default)
                {
                    throw new Exception($"Unable to add {this} as a Stepper to StereoKit.");
                }
            };

            // Remove this stepper when pipeline is no longer running, otherwise Step() will continue to be called!
            pipeline.PipelineCompleted += (_, _) =>
            {
                SK.RemoveStepper(this);
            };
        }

        /// <inheritdoc />
        public bool Enabled => true;

        /// <inheritdoc />
        public virtual bool Initialize() => true;

        /// <inheritdoc />
        public virtual void Step()
        {
        }

        /// <inheritdoc />
        public virtual void Shutdown()
        {
        }
    }
}

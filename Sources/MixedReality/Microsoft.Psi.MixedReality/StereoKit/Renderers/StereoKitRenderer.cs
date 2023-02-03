// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using global::StereoKit;

    /// <summary>
    /// Base class for StereoKit rendering components.
    /// </summary>
    /// <remarks>This class ensures that rendering of \psi objects occurs in the correct world frame.</remarks>
    public abstract class StereoKitRenderer : StereoKitComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for the component.</param>
        public StereoKitRenderer(Pipeline pipeline, string name = nameof(StereoKitRenderer))
            : base(pipeline, name)
        {
        }

        /// <inheritdoc />
        public override void Step()
        {
            if (StereoKitTransforms.WorldHierarchy.HasValue)
            {
                Hierarchy.Push(StereoKitTransforms.WorldHierarchy.Value);
                this.Render();
                Hierarchy.Pop();
            }
        }

        /// <summary>
        /// All rendering/drawing that needs to happen on each frame.
        /// </summary>
        protected abstract void Render();
    }
}

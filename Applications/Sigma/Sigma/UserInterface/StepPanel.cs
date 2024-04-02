// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    /// <summary>
    /// Base abstract class for step panels.
    /// </summary>
    public abstract class StepPanel : Rectangle3DUserInterface
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StepPanel"/> class.
        /// </summary>
        /// <param name="name">The name of the step panel.</param>
        public StepPanel(string name)
            : base(name)
        {
        }
    }
}

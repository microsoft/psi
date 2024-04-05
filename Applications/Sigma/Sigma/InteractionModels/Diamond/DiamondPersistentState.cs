// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Diamond
{
    /// <summary>
    /// Represents the persistent state for the Diamond version of the Sigma app.
    /// </summary>
    public class DiamondPersistentState : SigmaPersistentState<DiamondTask>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiamondPersistentState"/> class.
        /// </summary>
        public DiamondPersistentState()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiamondPersistentState"/> class.
        /// </summary>
        /// <param name="persistentStateFilename">The filename to read the persistent state from.</param>
        public DiamondPersistentState(string persistentStateFilename)
            : base(persistentStateFilename)
        {
        }
    }
}

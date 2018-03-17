// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    /// <summary>
    /// Enables message passing between components.
    /// </summary>
    public interface IEmitter
    {
        string Name { get; }

        object Owner { get; }

        Pipeline Pipeline { get; }

        int Id { get; }

        /// <summary>
        /// Enables debug visualization for this stream.
        /// </summary>
        /// <param name="debugName">An optional name to use in the visualization window.</param>
        /// <returns>The debug name of the stream, either as provided or the generated one if one was not specified</returns>
        string DebugView(string debugName = null);
    }
}

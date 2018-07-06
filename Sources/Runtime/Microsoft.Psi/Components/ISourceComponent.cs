// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    /// <summary>
    /// Marker interface indicating that a component is a "source" of messages; sensor inputs, generators, etc.
    /// Reactive components that generate messages only in response to incoming messages should not implement this interface.
    /// A source component that has a concept of "completion" should implement <see cref="IFiniteSourceComponent"/>.
    /// A pipeline containing source components will shut down only once all sources have completed (or earlier if explicitly stopped/disposed).
    /// </summary>
    public interface ISourceComponent
    {
    }
}

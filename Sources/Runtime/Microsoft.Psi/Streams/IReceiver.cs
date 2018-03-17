// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;

    /// <summary>
    /// Enables message passing between components.
    /// </summary>
    public interface IReceiver : IDisposable
    {
        object Owner { get; }

        IEmitter Source { get; }
    }
}

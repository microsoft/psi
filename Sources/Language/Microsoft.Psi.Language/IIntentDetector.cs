// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Language
{
    using Microsoft.Psi.Components;

    /// <summary>
    /// Defines an interface for speech intent detectors.
    /// </summary>
    public interface IIntentDetector : IConsumerProducer<string, IntentData>
    {
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System;
    using Microsoft.Psi.Language;

    /// <summary>
    /// Implements methods for registering platform specific resources.
    /// </summary>
    public static class Resources
    {
        /// <summary>
        /// Registers platform specific resources.
        /// </summary>
        public static void RegisterPlatformResources()
        {
            PlatformResources.RegisterDefault<Func<Pipeline, IIntentDetector>>(p => new SystemSpeechIntentDetector(p));
            PlatformResources.Register<Func<Pipeline, IIntentDetector>>(nameof(SystemSpeechIntentDetector), p => new SystemSpeechIntentDetector(p));

            PlatformResources.RegisterDefault<Func<Pipeline, ISpeechRecognizer>>(p => new SystemSpeechRecognizer(p));
            PlatformResources.Register<Func<Pipeline, ISpeechRecognizer>>(nameof(SystemSpeechRecognizer), p => new SystemSpeechRecognizer(p));

            PlatformResources.RegisterDefault<Func<Pipeline, ISpeechSynthesizer>>(p => new SystemSpeechSynthesizer(p));
            PlatformResources.Register<Func<Pipeline, ISpeechSynthesizer>>(nameof(SystemSpeechSynthesizer), p => new SystemSpeechSynthesizer(p));

            PlatformResources.RegisterDefault<Func<Pipeline, IVoiceActivityDetector>>(p => new SystemVoiceActivityDetector(p));
            PlatformResources.Register<Func<Pipeline, IVoiceActivityDetector>>(nameof(SystemVoiceActivityDetector), p => new SystemVoiceActivityDetector(p));
        }
    }
}

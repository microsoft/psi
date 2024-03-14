// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    using System.Drawing;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Implements a set of common annotation schemas.
    /// </summary>
    public class AnnotationSchemas
    {
        /// <summary>
        /// The speech synthesis annotation schema.
        /// </summary>
        public static readonly AnnotationSchema SpeechSynthesisAnnotationSchema =
            new (
                nameof(SpeechSynthesisAnnotationSchema),
                new AnnotationAttributeSchema(
                    "Speech",
                    "The synthesized speech",
                    new StringAnnotationValueSchema("<Unknown>", Color.DodgerBlue, Color.White)));
    }
}

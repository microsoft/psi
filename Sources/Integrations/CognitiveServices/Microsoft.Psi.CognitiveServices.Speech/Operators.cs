// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech
{
    using System;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Implements operators for processing speech streams.
    /// </summary>
    public static class Operators
    {
        /// <summary>
        /// Converts a stream of <see cref="SpeechSynthesisProgress"/> events into speech synthesis annotations.
        /// </summary>
        /// <param name="source">The input stream.</param>
        /// <returns>The stream of annotations.</returns>
        public static IProducer<TimeIntervalAnnotationSet> ToAnnotations(this IProducer<SpeechSynthesisProgress> source)
        {
            var speechSynthesisStarted = DateTime.MinValue;
            var speechSynthesisAttributeSchema = AnnotationSchemas.SpeechSynthesisAnnotationSchema.AttributeSchemas[0];
            return source.Process<SpeechSynthesisProgress, TimeIntervalAnnotationSet>(
                (sse, envelope, emitter) =>
                {
                    if (sse.EventType == SpeechSynthesisProgressEventType.SynthesisStarted)
                    {
                        speechSynthesisStarted = envelope.OriginatingTime;
                    }
                    else if (sse.EventType == SpeechSynthesisProgressEventType.SynthesisCompleted ||
                        sse.EventType == SpeechSynthesisProgressEventType.SynthesisCancelled)
                    {
                        var timeIntervalAnnotation = new TimeIntervalAnnotation(
                            new TimeInterval(speechSynthesisStarted, envelope.OriginatingTime),
                            "SpeechSynthesis",
                            speechSynthesisAttributeSchema.CreateAttribute(sse.Text));
                        var timeIntervalAnnotationSet = new TimeIntervalAnnotationSet(timeIntervalAnnotation);
                        emitter.Post(timeIntervalAnnotationSet, envelope.OriginatingTime);
                    }
                },
                DeliveryPolicy.Unlimited);
        }
    }
}

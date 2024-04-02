// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System.IO;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.CognitiveServices.Speech;
    using Microsoft.Psi.Interop.Serialization;
    using Microsoft.Psi.MixedReality.WinRT;
    using HoloLensSerializers = HoloLensCaptureInterop.Serialization;

    /// <summary>
    /// Implements format serializers for types specific to mixed reality applications.
    /// </summary>
    internal class Serializers
    {
        /// <summary>
        /// Format for <see cref="SpeechSynthesisProgress"/>.
        /// </summary>
        /// <returns><see cref="Format{SpeechSynthesizerEvent}"/> serializer/deserializer.</returns>
        public static Format<SpeechSynthesisProgress> SpeechSynthesisProgressFormat()
            => new (WriteSpeechSynthesisProgress, ReadSpeechSynthesisProgress);

        /// <summary>
        /// Write <see cref="bool"/> to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="speechSynthesizerEvent"><see cref="SpeechSynthesisProgress"/> to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteSpeechSynthesisProgress(SpeechSynthesisProgress speechSynthesizerEvent, BinaryWriter writer)
        {
            InteropSerialization.WriteBool(speechSynthesizerEvent != null, writer);
            if (speechSynthesizerEvent == null)
            {
                return;
            }

            InteropSerialization.WriteInt32((int)speechSynthesizerEvent.EventType, writer);
            InteropSerialization.WriteString(speechSynthesizerEvent.Text, writer);
        }

        /// <summary>
        /// Read <see cref="SpeechSynthesisProgress"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="bool"/>.</returns>
        public static SpeechSynthesisProgress ReadSpeechSynthesisProgress(BinaryReader reader)
        {
            if (!InteropSerialization.ReadBool(reader))
            {
                return null;
            }

            var type = (SpeechSynthesisProgressEventType)InteropSerialization.ReadInt32(reader);
            var text = InteropSerialization.ReadString(reader);

            return type switch
            {
                SpeechSynthesisProgressEventType.SynthesisStarted => SpeechSynthesisProgress.SynthesisStarted(),
                SpeechSynthesisProgressEventType.SynthesisInProgress => SpeechSynthesisProgress.SynthesisInProgress(text),
                SpeechSynthesisProgressEventType.SynthesisCompleted => SpeechSynthesisProgress.SynthesisCompleted(text),
                SpeechSynthesisProgressEventType.SynthesisCancelled => SpeechSynthesisProgress.SynthesisCancelled(text),
                _ => throw new System.Exception("Unexpected synthesis progress type."),
            };
        }

        /// <summary>
        /// Format for a tuple of eyes and head.
        /// </summary>
        /// <returns>Format serializer/deserializer for a tuple of eyes and head.</returns>
        public static Format<(Eyes, CoordinateSystem)> EyesAndHeadFormat()
            => new (WriteEyesAndHead, ReadEyesAndHead);

        /// <summary>
        /// Write a tuple of eyes and head to <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="eyesAndHead">The tuple of eyes and head to write.</param>
        /// <param name="writer"><see cref="BinaryWriter"/> to which to write.</param>
        public static void WriteEyesAndHead((Eyes Eyes, CoordinateSystem Head) eyesAndHead, BinaryWriter writer)
        {
            HoloLensSerializers.WriteWinRTEyes(eyesAndHead.Eyes, writer);
            HoloLensSerializers.WriteCoordinateSystem(eyesAndHead.Head, writer);
        }

        /// <summary>
        /// Read <see cref="SpeechSynthesisProgress"/> from <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader"><see cref="BinaryReader"/> from which to read.</param>
        /// <returns><see cref="bool"/>.</returns>
        public static (Eyes Eyes, CoordinateSystem Head) ReadEyesAndHead(BinaryReader reader)
            => (HoloLensSerializers.ReadWinRTEyes(reader), HoloLensSerializers.ReadCoordinateSystem(reader));
    }
}

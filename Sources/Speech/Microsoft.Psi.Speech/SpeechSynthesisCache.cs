// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Speech
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Psi.Audio;

    /// <summary>
    /// Implements a cache for speech synthesis content, mapping strings to audio buffers and corresponding text offsets.
    /// </summary>
    public class SpeechSynthesisCache
    {
        private readonly Dictionary<string, (AudioBuffer Data, List<(string Text, ulong Offset)> TextMapping)> cache = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechSynthesisCache"/> class.
        /// </summary>
        /// <param name="speechSynthesisVoiceName">The name of the speech synthesis voice.</param>
        public SpeechSynthesisCache(string speechSynthesisVoiceName)
        {
            this.SpeechSynthesisVoiceName = speechSynthesisVoiceName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeechSynthesisCache"/> class.
        /// </summary>
        /// <param name="stream">An optional stream from which to initialize the cache.</param>
        public SpeechSynthesisCache(Stream stream = default)
        {
            if (stream != default)
            {
                this.Read(stream);
            }
        }

        /// <summary>
        /// Gets the speech synthesis voice name.
        /// </summary>
        public string SpeechSynthesisVoiceName { get; private set; }

        /// <summary>
        /// Reads the contents of the cache from a specified stream.
        /// </summary>
        /// <param name="stream">The stream from which to read the cache.</param>
        public void Read(Stream stream)
        {
            using var reader = new BinaryReader(stream);
            this.SpeechSynthesisVoiceName = reader.ReadString();
            var count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                // Read the key (text)
                var text = reader.ReadString();

                // Read the audio buffer
                var data = reader.ReadBytes(reader.ReadInt32());
                var waveFormat = WaveFormat.FromStream(stream, reader.ReadInt32());
                var audioBuffer = new AudioBuffer(data, waveFormat);

                // Read the text mapping
                var textMapping = new List<(string Text, ulong Offset)>();
                var textMappingCount = reader.ReadInt32();
                for (int j = 0; j < textMappingCount; j++)
                {
                    var textMappingText = reader.ReadString();
                    var textMappingOffset = reader.ReadUInt64();
                    textMapping.Add((textMappingText, textMappingOffset));
                }

                // Add it to the cache
                this.cache.Add(text, (audioBuffer, textMapping));
            }
        }

        /// <summary>
        /// Writes the cache to a file stream.
        /// </summary>
        /// <param name="stream">The stream to which to write the cache.</param>
        public void Write(Stream stream)
        {
            using var writer = new BinaryWriter(stream);
            writer.Write(this.SpeechSynthesisVoiceName);
            writer.Write(this.cache.Count);
            foreach (var entry in this.cache)
            {
                // Write the key (text)
                writer.Write(entry.Key);

                // Write the audio buffer
                var audioBuffer = entry.Value.Data;
                writer.Write(audioBuffer.Data.Length);
                writer.Write(audioBuffer.Data);
                writer.Write(WaveFormat.Size + audioBuffer.Format.ExtraSize);
                audioBuffer.Format.WriteTo(writer);

                // Write the text mapping
                writer.Write(entry.Value.TextMapping.Count);
                foreach (var (text, offset) in entry.Value.TextMapping)
                {
                    writer.Write(text);
                    writer.Write(offset);
                }
            }
        }

        /// <summary>
        /// Adds a new entry to the cache.
        /// </summary>
        /// <param name="text">The text to add to the cache.</param>
        /// <param name="audioBuffer">The corresponding audio buffer for the specified text.</param>
        /// <param name="textMapping">A mapping of the text to the audio buffer.</param>
        public void Add(string text, AudioBuffer audioBuffer, List<(string Text, ulong Offset)> textMapping)
            => this.cache[text] = (audioBuffer, textMapping);

        /// <summary>
        /// Tries to get the audio buffer for the specified text.
        /// </summary>
        /// <param name="text">The text to look up in the cache.</param>
        /// <param name="audioBuffer">The audio buffer for the specified text.</param>
        /// <param name="textMapping">A mapping of the text to the audio buffer.</param>
        /// <returns>True if the text is found in the cache, false otherwise.</returns>
        public bool TryGetValue(string text, out AudioBuffer audioBuffer, out List<(string Text, ulong Offset)> textMapping)
        {
            if (this.cache.TryGetValue(text, out var tuple))
            {
                audioBuffer = tuple.Data;
                textMapping = tuple.TextMapping;
                return true;
            }
            else
            {
                audioBuffer = default;
                textMapping = default;
                return false;
            }
        }
    }
}

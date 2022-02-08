// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Provides static Wave file helper methods.
    /// </summary>
    public static class WaveFileHelper
    {
        /// <summary>
        /// Reads a Wave file header from a binary stream.
        /// </summary>
        /// <param name="br">The binary reader to read from.</param>
        /// <returns>A WaveFormat object representing the Wave header.</returns>
        public static WaveFormat ReadWaveFileHeader(BinaryReader br)
        {
            if (Encoding.UTF8.GetString(BitConverter.GetBytes(br.ReadInt32())) != "RIFF")
            {
                throw new FormatException("RIFF header missing");
            }

            uint fileSize = br.ReadUInt32();

            if (Encoding.UTF8.GetString(BitConverter.GetBytes(br.ReadInt32())) != "WAVE")
            {
                throw new FormatException("WAVE header missing");
            }

            if (Encoding.UTF8.GetString(BitConverter.GetBytes(br.ReadInt32())) != "fmt ")
            {
                throw new FormatException("Format header missing");
            }

            uint headerLength = br.ReadUInt32();

            WaveFormat format = WaveFormat.FromStream(br.BaseStream, (int)headerLength);

            return format;
        }

        /// <summary>
        /// Reads a Wave file header from a Wave file.
        /// </summary>
        /// <param name="filename">The name of the file to read from.</param>
        /// <returns>A WaveFormat object representing the Wave header.</returns>
        public static WaveFormat ReadWaveFileHeader(string filename)
        {
            using (BinaryReader br = new BinaryReader(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                return ReadWaveFileHeader(br);
            }
        }

        /// <summary>
        /// Reads the length in bytes of the data section of a Wave file.
        /// </summary>
        /// <param name="br">The binary reader to read from.</param>
        /// <returns>The number of byte of wave data that follow.</returns>
        public static long ReadWaveDataLength(BinaryReader br)
        {
            var name = Encoding.UTF8.GetString(BitConverter.GetBytes(br.ReadInt32()));
            if (name != "data")
            {
                if (name == "fact" || name == "LIST")
                {
                    // Some formats (e.g. IEEE float) contain fact and LIST chunks (which we skip).
                    // see fhe "fact Chunk" section of the spec: http://www-mmsp.ece.mcgill.ca/Documents/AudioFormats/WAVE/WAVE.html
                    // "IEEE float data (introduced after the Rev. 3 documention) need a fact"
                    br.ReadBytes((int)br.ReadUInt32()); // skip
                    return ReadWaveDataLength(br);
                }

                throw new FormatException("Data header missing");
            }

            return br.ReadUInt32();
        }

        /// <summary>
        /// Reads the data section of a Wave file.
        /// </summary>
        /// <param name="br">The binary reader to read from.</param>
        /// <returns>An array of the raw audio data bytes.</returns>
        public static byte[] ReadWaveData(BinaryReader br)
        {
            return br.ReadBytes((int)ReadWaveDataLength(br));
        }

        /// <summary>
        /// Extension method to write WaveFormat objects.
        /// </summary>
        /// <param name="writer">The BinaryWriter to which to write the WaveFormat.</param>
        /// <param name="format">The WaveFormat object to write.</param>
        public static void Write(this BinaryWriter writer, WaveFormat format)
        {
            format.WriteTo(writer);
        }
    }
}

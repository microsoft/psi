// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an audio format based on the Windows WAVEFORMAT structure.
    /// </summary>
    [Serializable]
    [DataContract]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class WaveFormat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFormat"/> class.
        /// </summary>
        internal WaveFormat()
        {
        }

        /// <summary>
        /// Gets or sets the wave format tag.
        /// </summary>
        [DataMember]
        public WaveFormatTag FormatTag { get; set; }

        /// <summary>
        /// Gets or sets the number of audio channels.
        /// </summary>
        [DataMember]
        public ushort Channels { get; set; }

        /// <summary>
        /// Gets or sets the audio sampling frequency.
        /// </summary>
        [DataMember]
        public uint SamplesPerSec { get; set; } // SampleRate

        /// <summary>
        /// Gets or sets the average number of bytes per second.
        /// </summary>
        [DataMember]
        public uint AvgBytesPerSec { get; set; } // SampleRate * NumChannels * BitsPerSample/8

        /// <summary>
        /// Gets or sets the block alignment in bytes.
        /// </summary>
        [DataMember]
        public ushort BlockAlign { get; set; } // NumChannels * BitsPerSample/8

        /// <summary>
        /// Gets or sets the audio sample size in bits.
        /// </summary>
        [DataMember]
        public ushort BitsPerSample { get; set; }

        /// <summary>
        /// Gets or sets the number of extra format info bytes.
        /// </summary>
        [DataMember]
        public ushort ExtraSize { get; set; }

        /// <summary>
        /// Creates a WaveFormat object representing 1-channel 16-bit PCM audio sampled at 16000 Hz.
        /// </summary>
        /// <returns>The WaveFormat object.</returns>
        public static WaveFormat Create16kHz1Channel16BitPcm()
        {
            return Create16BitPcm(16000, 1);
        }

        /// <summary>
        /// Creates a WaveFormat object representing 16-bit PCM audio.
        /// </summary>
        /// <param name="samplingRate">The sampling frequency.</param>
        /// <param name="channels">The number of audio channels.</param>
        /// <returns>The WaveFormat object.</returns>
        public static WaveFormat Create16BitPcm(int samplingRate, int channels)
        {
            return CreatePcm(samplingRate, 16, channels);
        }

        /// <summary>
        /// Creates a WaveFormat object representing PCM audio.
        /// </summary>
        /// <param name="samplingRate">The sampling frequency.</param>
        /// <param name="bitsPerSample">The number of bits per channel sample.</param>
        /// <param name="channels">The number of audio channels.</param>
        /// <returns>The WaveFormat object.</returns>
        public static WaveFormat CreatePcm(int samplingRate, int bitsPerSample, int channels)
        {
            ushort blockAlign = (ushort)(channels * (bitsPerSample / 8));
            return Create(WaveFormatTag.WAVE_FORMAT_PCM, samplingRate, bitsPerSample, channels, blockAlign, samplingRate * blockAlign);
        }

        /// <summary>
        /// Creates a WaveFormat object representing 1-channel 32-bit IEEE float audio sampled at 16000 Hz.
        /// </summary>
        /// <returns>The WaveFormat object.</returns>
        public static WaveFormat Create16kHz1ChannelIeeeFloat()
        {
            return CreateIeeeFloat(16000, 1);
        }

        /// <summary>
        /// Creates a WaveFormat object representing 32-bit IEEE float audio.
        /// </summary>
        /// <param name="samplingRate">The sampling frequency.</param>
        /// <param name="channels">The number of audio channels.</param>
        /// <returns>The WaveFormat object.</returns>
        public static WaveFormat CreateIeeeFloat(int samplingRate, int channels)
        {
            ushort blockAlign = (ushort)(channels * 4); // 32-bit float = 4 bytes
            return Create(WaveFormatTag.WAVE_FORMAT_IEEE_FLOAT, samplingRate, 32, channels, blockAlign, samplingRate * blockAlign);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="WaveFormat"/> class.
        /// </summary>
        /// <param name="formatTag">The format tag.</param>
        /// <param name="samplingRate">The sampling frequency.</param>
        /// <param name="bitsPerSample">The number of bits per channel sample.</param>
        /// <param name="channels">The number of audio channels.</param>
        /// <param name="blockAlign">The block alignment.</param>
        /// <param name="avgBytesPerSecond">The average number of bytes per second.</param>
        /// <returns>The WaveFormat object.</returns>
        public static WaveFormat Create(WaveFormatTag formatTag, int samplingRate, int bitsPerSample, int channels, int blockAlign, int avgBytesPerSecond)
        {
            return new WaveFormat()
            {
                FormatTag = formatTag,
                SamplesPerSec = (uint)samplingRate,
                BitsPerSample = (ushort)bitsPerSample,
                Channels = (ushort)channels,
                BlockAlign = (ushort)blockAlign,
                AvgBytesPerSec = (uint)avgBytesPerSecond,
            };
        }

        /// <summary>
        /// Creates a copy of an existing <see cref="WaveFormat"/>.
        /// </summary>
        /// <param name="other">The <see cref="WaveFormat"/> to copy.</param>
        /// <returns>The WaveFormat object.</returns>
        public static WaveFormat Create(WaveFormat other)
        {
            if (other == null)
            {
                return null;
            }

            WaveFormat waveFormat = null;

            if (other is WaveFormatEx)
            {
                waveFormat = new WaveFormatEx();
            }
            else
            {
                waveFormat = new WaveFormat();
            }

            waveFormat.CopyFrom(other);
            return waveFormat;
        }

        /// <summary>
        /// Reads from a stream to create a WaveFormat object.
        /// </summary>
        /// <param name="stream">
        /// The stream to read the WaveFormat from (e.g. a file). The stream should already
        /// be positioned on the first byte of the WaveFormat header (i.e. the format tag).
        /// Note that the stream is not disposed after the header has been read.
        /// </param>
        /// <param name="formatLength">
        /// The total number of bytes in the stream that represent the WaveFormat header.
        /// </param>
        /// <returns>
        /// A new WaveFormat header constructed from the stream.
        /// </returns>
        public static WaveFormat FromStream(Stream stream, int formatLength)
        {
            if (formatLength < 16)
            {
                throw new InvalidDataException("A WaveFormat must contain at least 16 bytes");
            }

            BinaryReader reader = new BinaryReader(stream);
            WaveFormat waveFormat = new WaveFormat();
            waveFormat.FormatTag = (WaveFormatTag)reader.ReadUInt16();
            waveFormat.Channels = reader.ReadUInt16();
            waveFormat.SamplesPerSec = reader.ReadUInt32();
            waveFormat.AvgBytesPerSec = reader.ReadUInt32();
            waveFormat.BlockAlign = reader.ReadUInt16();
            waveFormat.BitsPerSample = reader.ReadUInt16();

            if (formatLength > 16)
            {
                // Format may contain extra bytes
                waveFormat.ExtraSize = reader.ReadUInt16();

                // Sanity check and size limit
                if (waveFormat.ExtraSize > formatLength - 18)
                {
                    throw new FormatException("Format extra size too large");
                }

                if (waveFormat.ExtraSize > 0)
                {
                    // Promote to a WaveFormatEx object to hold the extra info
                    waveFormat = new WaveFormatEx()
                    {
                        FormatTag = waveFormat.FormatTag,
                        SamplesPerSec = waveFormat.SamplesPerSec,
                        BitsPerSample = waveFormat.BitsPerSample,
                        Channels = waveFormat.Channels,
                        BlockAlign = waveFormat.BlockAlign,
                        AvgBytesPerSec = waveFormat.AvgBytesPerSec,
                        ExtraSize = waveFormat.ExtraSize,

                        // Read in the extended format extra bytes
                        ExtraInfo = reader.ReadBytes(waveFormat.ExtraSize),
                    };
                }
            }

            return waveFormat;
        }

        /// <summary>
        /// Marshals a native WAVEFORMATEX structure to a <see cref="WaveFormat"/> object.
        /// </summary>
        /// <param name="ptr">Pointer to a WAVEFORMATEX structure.</param>
        /// <returns>A <see cref="WaveFormat"/> object representing the WAVEFORMATEX structure.</returns>
        public static WaveFormat MarshalFromPtr(IntPtr ptr)
        {
            WaveFormat waveFormat = null;

            if (ptr != IntPtr.Zero)
            {
                waveFormat = Marshal.PtrToStructure<WaveFormat>(ptr);
                if (waveFormat.ExtraSize > 0)
                {
                    WaveFormatEx waveFormatEx = new WaveFormatEx();
                    waveFormatEx.CopyFrom(waveFormat);
                    IntPtr extraInfoPtr = new IntPtr(ptr.ToInt64() + Marshal.SizeOf<WaveFormat>());

                    // Read extra info
                    waveFormatEx.ExtraInfo = new byte[waveFormatEx.ExtraSize];
                    Marshal.Copy(extraInfoPtr, waveFormatEx.ExtraInfo, 0, waveFormatEx.ExtraSize);

                    // Return the WaveFormatEx object
                    waveFormat = waveFormatEx;
                }
            }

            return waveFormat;
        }

        /// <summary>
        /// Marshals a <see cref="WaveFormat"/> object to a native WAVEFORMATEX structure.
        /// </summary>
        /// <param name="format">The <see cref="WaveFormat"/> object to marshal.</param>
        /// <param name="ptr">A pointer to an unmanaged block of memory, which must be allocated before this method is called.</param>
        public static void MarshalToPtr(WaveFormat format, IntPtr ptr)
        {
            // If format is of a derived type, demote it to the base WaveFormat to ensure
            // that the base structure marshal correctly and does not overflow the allocated
            // memory. The extra info bytes will be marshaled separately.
            WaveFormat baseFormat = format;
            if (format.GetType() != typeof(WaveFormat))
            {
                baseFormat = new WaveFormat();
                baseFormat.CopyFrom(format);
            }

            Marshal.StructureToPtr(baseFormat, ptr, false);
            if (format.ExtraSize > 0)
            {
                WaveFormatEx waveFormatEx = format as WaveFormatEx;

                if ((waveFormatEx == null) ||
                    (waveFormatEx.ExtraInfo == null))
                {
                    throw new InvalidOperationException("WaveFormat has non-zero extra bytes but no extra bytes field was present.");
                }

                if (waveFormatEx.ExtraInfo.Length < format.ExtraSize)
                {
                    throw new InvalidOperationException($"WaveFormat extra size field and extra byte count mismatch. "
                        + "Expected {format.ExtraSize} bytes but ExtraInfo only contains {waveFormatEx.ExtraInfo.Length} bytes.");
                }

                if (waveFormatEx != null)
                {
                    // Write out extra info
                    IntPtr extraInfoPtr = new IntPtr(ptr.ToInt64() + Marshal.SizeOf<WaveFormat>());
                    Marshal.Copy(waveFormatEx.ExtraInfo, 0, extraInfoPtr, format.ExtraSize);
                }
            }
        }

        /// <summary>
        /// Marshals a <see cref="WaveFormat"/> object to a native WAVEFORMATEX structure.
        /// </summary>
        /// <param name="format">The <see cref="WaveFormat"/> object to marshal.</param>
        /// <returns>A pointer to the native WAVEFORMATEX structure. This should be freed by the caller using Marshal.FreeHGlobal.</returns>
        public static IntPtr MarshalToPtr(WaveFormat format)
        {
            IntPtr formatPtr = Marshal.AllocHGlobal(MarshalSizeOf(format));
            MarshalToPtr(format, formatPtr);
            return formatPtr;
        }

        /// <summary>
        /// Returns the size in bytes required to create a marshaled unmanaged copy of the object.
        /// </summary>
        /// <param name="format">The object that is to be marshaled.</param>
        /// <returns>The unmanaged size of the object.</returns>
        public static int MarshalSizeOf(WaveFormat format)
        {
            return Marshal.SizeOf<WaveFormat>() + format?.ExtraSize ?? 0;
        }

        /// <summary>
        /// Copy field values from another <see cref="WaveFormat"/> object.
        /// </summary>
        /// <param name="other">The <see cref="WaveFormat"/> object to copy from.</param>
        public virtual void CopyFrom(WaveFormat other)
        {
            if (other != null)
            {
                this.FormatTag = other.FormatTag;
                this.SamplesPerSec = other.SamplesPerSec;
                this.BitsPerSample = other.BitsPerSample;
                this.Channels = other.Channels;
                this.BlockAlign = other.BlockAlign;
                this.AvgBytesPerSec = other.AvgBytesPerSec;
                this.ExtraSize = other.ExtraSize;
            }
        }

        /// <summary>
        /// Serializes the WaveFormat to a BinaryWriter.
        /// </summary>
        /// <param name="writer">The BinaryWriter to serialize to.</param>
        public virtual void WriteTo(BinaryWriter writer)
        {
            writer.Write((ushort)this.FormatTag);
            writer.Write(this.Channels);
            writer.Write(this.SamplesPerSec);
            writer.Write(this.AvgBytesPerSec);
            writer.Write(this.BlockAlign);
            writer.Write(this.BitsPerSample);
            writer.Write(this.ExtraSize);
        }

        /// <summary>
        /// Gets a byte array containing the serialized format structure.
        /// </summary>
        /// <returns>A byte array containing the serialized format structure.</returns>
        public byte[] GetBytes()
        {
            MemoryStream formatStream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(formatStream))
            {
                writer.Write(this);
                return formatStream.ToArray();
            }
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object.
        /// </summary>
        /// <param name="other">
        /// An object to compare with this object.
        /// </param>
        /// <returns>
        /// true if the current object is equal to the other parameter; otherwise, false.
        /// </returns>
        public override bool Equals(object other)
        {
            WaveFormat otherFormat = other as WaveFormat;

            // null and check
            if (object.ReferenceEquals(otherFormat, null))
            {
                return false;
            }

            // same reference implies equality
            if (object.ReferenceEquals(otherFormat, this))
            {
                return true;
            }

            // field comparison
            return (this.FormatTag == otherFormat.FormatTag) &&
                (this.Channels == otherFormat.Channels) &&
                (this.SamplesPerSec == otherFormat.SamplesPerSec) &&
                (this.AvgBytesPerSec == otherFormat.AvgBytesPerSec) &&
                (this.BlockAlign == otherFormat.BlockAlign) &&
                (this.BitsPerSample == otherFormat.BitsPerSample) &&
                (this.ExtraSize == otherFormat.ExtraSize);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            // use of primes in hash computation reduces likelihood of collisions
            int hash = 17;
            hash = (hash * 23) + this.FormatTag.GetHashCode();
            hash = (hash * 23) + this.Channels.GetHashCode();
            hash = (hash * 23) + this.SamplesPerSec.GetHashCode();
            hash = (hash * 23) + this.AvgBytesPerSec.GetHashCode();
            hash = (hash * 23) + this.BlockAlign.GetHashCode();
            hash = (hash * 23) + this.BitsPerSample.GetHashCode();
            hash = (hash * 23) + this.ExtraSize.GetHashCode();
            return hash;
        }
    }
}

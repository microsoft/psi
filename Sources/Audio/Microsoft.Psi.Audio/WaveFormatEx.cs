// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    /// <summary>
    /// Represents an audio format based on the Windows WAVEFORMATEX structure.
    /// </summary>
    [Serializable]
    [DataContract]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class WaveFormatEx : WaveFormat
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaveFormatEx"/> class.
        /// </summary>
        internal WaveFormatEx()
        {
        }

        /// <summary>
        /// Gets or sets the extra info bytes.
        /// </summary>
        [DataMember]
        [XmlElement(DataType = "hexBinary")]
        public byte[] ExtraInfo { get; set; }

        /// <summary>
        /// Gets the WAVEFORMATEXTENSIBLE SubFormat field.
        /// </summary>
        [IgnoreDataMember]
        [XmlIgnore]
        public Guid SubFormat
        {
            get
            {
                byte[] guidBytes = new byte[16];
                Array.Copy(this.ExtraInfo, 6, guidBytes, 0, 16);
                return new Guid(guidBytes);
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="WaveFormatEx"/> class.
        /// </summary>
        /// <param name="formatTag">The format tag.</param>
        /// <param name="samplingRate">The sampling frequency.</param>
        /// <param name="bitsPerSample">The number of bits per channel sample.</param>
        /// <param name="channels">The number of audio channels.</param>
        /// <param name="blockAlign">The block alignment.</param>
        /// <param name="avgBytesPerSecond">The average number of bytes per second.</param>
        /// <param name="extraInfo">An array containing extra format-specific information.</param>
        /// <returns>The WaveFormat object.</returns>
        public static WaveFormatEx Create(WaveFormatTag formatTag, int samplingRate, int bitsPerSample, int channels, int blockAlign, int avgBytesPerSecond, byte[] extraInfo = null)
        {
            return new WaveFormatEx()
            {
                FormatTag = formatTag,
                SamplesPerSec = (uint)samplingRate,
                BitsPerSample = (ushort)bitsPerSample,
                Channels = (ushort)channels,
                BlockAlign = (ushort)blockAlign,
                AvgBytesPerSec = (uint)avgBytesPerSecond,
                ExtraSize = (ushort)(extraInfo?.Length ?? 0),
                ExtraInfo = extraInfo,
            };
        }

        /// <summary>
        /// Copy field values from another <see cref="WaveFormat"/> object.
        /// </summary>
        /// <param name="other">The <see cref="WaveFormat"/> object to copy from.</param>
        public override void CopyFrom(WaveFormat other)
        {
            base.CopyFrom(other);

            WaveFormatEx otherEx = other as WaveFormatEx;
            if (otherEx != null)
            {
                this.ExtraInfo = (byte[])otherEx.ExtraInfo?.Clone();
            }
        }

        /// <summary>
        /// Serializes the WaveFormat to a BinaryWriter.
        /// </summary>
        /// <param name="writer">The BinaryWriter to serialize to.</param>
        public override void WriteTo(BinaryWriter writer)
        {
            base.WriteTo(writer);

            if (this.ExtraSize > 0)
            {
                writer.Write(this.ExtraInfo, 0, this.ExtraSize);
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
            if (base.Equals(other))
            {
                // extra size is zero for both, so extra bytes are irrelevant
                if (this.ExtraSize == 0)
                {
                    return true;
                }

                // Downcast other to WaveFormatEx to compare extra bytes
                WaveFormatEx format = other as WaveFormatEx;

                // sanity checks for extra bytes
                if ((format == null) ||
                    (this.ExtraInfo == null) ||
                    (format.ExtraInfo == null) ||
                    (this.ExtraInfo.Length < this.ExtraSize) ||
                    (format.ExtraInfo.Length < format.ExtraSize))
                {
                    return false;
                }

                // byte-wise comparison of extra bytes
                for (int i = 0; i < this.ExtraSize; ++i)
                {
                    if (this.ExtraInfo[i] != format.ExtraInfo[i])
                    {
                        return false;
                    }
                }

                // equality
                return true;
            }

            return false;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();

            if ((this.ExtraSize > 0) &&
                (this.ExtraInfo != null) &&
                (this.ExtraInfo.Length >= this.ExtraSize))
            {
                for (int i = 0; i < this.ExtraSize; ++i)
                {
                    hash = (hash * 23) + this.ExtraInfo[i].GetHashCode();
                }
            }

            return hash;
        }
    }
}

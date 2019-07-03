// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Media
{
    using Microsoft.Psi.Media_Interop;

    /// <summary>
    /// Encapsulates configuration for Mpeg4Writer component.
    /// </summary>
    public class Mpeg4WriterConfiguration
    {
        /// <summary>
        /// Default configuration.
        /// </summary>
        public static readonly Mpeg4WriterConfiguration Default = new Mpeg4WriterConfiguration()
        {
            ImageWidth = 1920,
            ImageHeight = 1080,
            PixelFormat = Imaging.PixelFormat.BGR_24bpp,
            FrameRateNumerator = 30,
            FrameRateDenominator = 1,
            TargetBitrate = 10000000,
            ContainsAudio = true,
            AudioBitsPerSample = 16,
            AudioSamplesPerSecond = 48000,
            AudioChannels = 2,
        };

        /// <summary>
        /// Gets or sets a value indicating output image width of the .mp4 file.
        /// </summary>
        public uint ImageWidth
        {
            get
            {
                return this.Config.imageWidth;
            }

            set
            {
                this.Config.imageWidth = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating output image height of the .mp4 file.
        /// </summary>
        public uint ImageHeight
        {
            get
            {
                return this.Config.imageHeight;
            }

            set
            {
                this.Config.imageHeight = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that defines the image format expected for input images.
        /// </summary>
        public Imaging.PixelFormat PixelFormat
        {
            get
            {
                return (Imaging.PixelFormat)this.Config.pixelFormat;
            }

            set
            {
                this.Config.pixelFormat = (int)value;
            }
        }

        /// <summary>
        /// Gets or sets a value that defines the output frame rate's numerator.
        /// </summary>
        public uint FrameRateNumerator
        {
            get
            {
                return this.Config.frameRateNumerator;
            }

            set
            {
                this.Config.frameRateNumerator = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that defines the output frame rate's denominator.
        /// </summary>
        public uint FrameRateDenominator
        {
            get
            {
                return this.Config.frameRateDenominator;
            }

            set
            {
                this.Config.frameRateDenominator = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that defines the output bitrate.
        /// </summary>
        public uint TargetBitrate
        {
            get
            {
                return this.Config.targetBitrate;
            }

            set
            {
                this.Config.targetBitrate = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the .mp4 files contains an audio stream.
        /// </summary>
        public bool ContainsAudio
        {
            get
            {
                return this.Config.containsAudio;
            }

            set
            {
                this.Config.containsAudio = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that defines the number of bits per audio sample (typically 16).
        /// </summary>
        public uint AudioBitsPerSample
        {
            get
            {
                return this.Config.bitsPerSample;
            }

            set
            {
                this.Config.bitsPerSample = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that defines the number of audio samples per second.
        /// </summary>
        public uint AudioSamplesPerSecond
        {
            get
            {
                return this.Config.samplesPerSecond;
            }

            set
            {
                this.Config.samplesPerSecond = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that defines number of audio channels in the output file (should be set to 1 or 2).
        /// </summary>
        public uint AudioChannels
        {
            get
            {
                return this.Config.numChannels;
            }

            set
            {
                this.Config.numChannels = value;
            }
        }

        /// <summary>
        /// Gets or sets the native MP4Writer's configuration object.
        /// </summary>
        internal MP4WriterConfiguration Config { get; set; } = new MP4WriterConfiguration();
    }
}

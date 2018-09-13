// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Audio
{
    /// <summary>
    /// Represents the configuration for the <see cref="AcousticFeaturesExtractor"/> component.
    /// </summary>
    public sealed class AcousticFeaturesExtractorConfiguration
    {
        /// <summary>
        /// The default configuration.
        /// </summary>
        public static readonly AcousticFeaturesExtractorConfiguration Default = new AcousticFeaturesExtractorConfiguration();

        /// <summary>
        /// Backing store for the InputFormat property.
        /// </summary>
        private WaveFormat inputFormat;

        /// <summary>
        /// Backing store for the ComputeFFT property.
        /// </summary>
        private bool computeFFT;

        /// <summary>
        /// Backing store for the ComputeFFTPower property.
        /// </summary>
        private bool computeFFTPower;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcousticFeaturesExtractorConfiguration"/> class.
        /// </summary>
        public AcousticFeaturesExtractorConfiguration()
        {
            // Default parameters for acoustic features computation
            this.FrameDurationInSeconds = 0.025f;
            this.FrameRateInHz = 100.0f;
            this.AddDither = true;
            this.DitherScaleFactor = 1.0f;
            this.StartFrequency = 250.0f;
            this.EndFrequency = 7000.0f;
            this.LowEndFrequency = 3000.0f;
            this.HighStartFrequency = 2500.0f;
            this.EntropyBandwidth = 2500.0f;
            this.ComputeLogEnergy = true;
            this.ComputeZeroCrossingRate = true;
            this.ComputeFrequencyDomainEnergy = true;
            this.ComputeLowFrequencyEnergy = true;
            this.ComputeHighFrequencyEnergy = true;
            this.ComputeSpectralEntropy = true;
            this.ComputeFFT = false;
            this.ComputeFFTPower = false;

            // Defaults to 16 kHz, 16-bit, 1-channel PCM samples
            this.InputFormat = WaveFormat.Create16kHz1Channel16BitPcm();
        }

        /// <summary>
        /// Gets or sets the duration of the frame of audio over which the acoustic features will be computed.
        /// </summary>
        public float FrameDurationInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the frame rate at which the acoustic features will be computed.
        /// </summary>
        public float FrameRateInHz { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether dither is to be applied to the audio data.
        /// </summary>
        public bool AddDither { get; set; }

        /// <summary>
        /// Gets or sets the scale factor by which the dither to be applied will be multiplied.
        /// A scale factor of 1.0 will result in a dither with a range of -1.0 to +1.0.
        /// </summary>
        public float DitherScaleFactor { get; set; }

        /// <summary>
        /// Gets or sets the start frequency for frequency-domain features.
        /// </summary>
        public float StartFrequency { get; set; }

        /// <summary>
        /// Gets or sets the end frequency for frequency-domain features.
        /// </summary>
        public float EndFrequency { get; set; }

        /// <summary>
        /// Gets or sets the end frequency for low-frequency features.
        /// </summary>
        public float LowEndFrequency { get; set; }

        /// <summary>
        /// Gets or sets the start frequency for high-frequency features.
        /// </summary>
        public float HighStartFrequency { get; set; }

        /// <summary>
        /// Gets or sets the bandwidth for entropy features.
        /// </summary>
        public float EntropyBandwidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to compute the log energy stream.
        /// </summary>
        public bool ComputeLogEnergy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to compute the zero-crossing rate stream.
        /// </summary>
        public bool ComputeZeroCrossingRate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to compute the FFT stream.
        /// </summary>
        public bool ComputeFFT
        {
            get
            {
                return this.computeFFT || this.ComputeFFTPower;
            }

            set
            {
                this.computeFFT = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to compute the FFT power stream.
        /// </summary>
        public bool ComputeFFTPower
        {
            get
            {
                return this.computeFFTPower ||
                    this.ComputeFrequencyDomainEnergy ||
                    this.ComputeLowFrequencyEnergy ||
                    this.ComputeHighFrequencyEnergy ||
                    this.ComputeSpectralEntropy;
            }

            set
            {
                this.computeFFTPower = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to compute the frequency domain energy stream.
        /// </summary>
        public bool ComputeFrequencyDomainEnergy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to compute the low frequency energy stream.
        /// </summary>
        public bool ComputeLowFrequencyEnergy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to compute the high frequency energy stream.
        /// </summary>
        public bool ComputeHighFrequencyEnergy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to compute the spectral entropy stream.
        /// </summary>
        public bool ComputeSpectralEntropy { get; set; }

        /// <summary>
        /// Gets or sets the format of the audio stream.
        /// </summary>
        public WaveFormat InputFormat
        {
            get
            {
                return this.inputFormat;
            }

            set
            {
                this.inputFormat = value;

                if (this.inputFormat != null)
                {
                    // compute derived values
                    this.inputFormat.BlockAlign = (ushort)(this.inputFormat.Channels * (this.inputFormat.BitsPerSample / 8));
                    this.inputFormat.AvgBytesPerSec = this.inputFormat.BlockAlign * this.inputFormat.SamplesPerSec;
                }
            }
        }
    }
}

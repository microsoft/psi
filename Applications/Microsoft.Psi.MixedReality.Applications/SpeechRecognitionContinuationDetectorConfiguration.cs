// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Represents the configuration for the speech recognition continuation detector.
    /// </summary>
    public class SpeechRecognitionContinuationDetectorConfiguration : ObservableObject
    {
        private int minSilenceTimeSpanMs = 1200;
        private string separator = " ... ";
        private int maxAlternatesCount = 10;

        /// <summary>
        /// Gets or sets the minimum silence time span.
        /// </summary>
        [DataMember]
        [DisplayName("Min Silence Timespan (ms)")]
        [Description("The minimum amount of silence between recognition results to consider them a continuation.")]
        public int MinSilenceTimeSpanMs
        {
            get => this.minSilenceTimeSpanMs;
            set { this.Set(nameof(this.MinSilenceTimeSpanMs), ref this.minSilenceTimeSpanMs, value); }
        }

        /// <summary>
        /// Gets or sets the separator.
        /// </summary>
        [DataMember]
        [DisplayName("Separator")]
        [Description("The string separator to use when concatenating results.")]
        public string Separator
        {
            get => this.separator;
            set { this.Set(nameof(this.Separator), ref this.separator, value); }
        }

        /// <summary>
        /// Gets or sets the max alternates count.
        /// </summary>
        [DataMember]
        [DisplayName("Max Alternates Count")]
        [Description("The maximum number of alternates to generate.")]
        public int MaxAlternatesCount
        {
            get => this.maxAlternatesCount;
            set { this.Set(nameof(this.MaxAlternatesCount), ref this.maxAlternatesCount, value); }
        }
    }
}

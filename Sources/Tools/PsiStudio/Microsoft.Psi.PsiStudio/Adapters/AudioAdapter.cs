// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of <see cref="AudioBuffer"/>s into doubles.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AudioAdapter : StreamAdapter<AudioBuffer, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioAdapter"/> class.
        /// </summary>
        public AudioAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(AudioBuffer value, Envelope env)
        {
            return (double)BitConverter.ToInt16(value.Data, value.Length - 2);
        }
    }
}

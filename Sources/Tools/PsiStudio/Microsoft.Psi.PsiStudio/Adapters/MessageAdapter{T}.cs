// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Used to adapt streams of any type into doubles (0.0).
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class MessageAdapter<T> : StreamAdapter<T, double>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageAdapter{T}"/> class.
        /// </summary>
        public MessageAdapter()
            : base(Adapter)
        {
        }

        private static double Adapter(T value, Envelope env)
        {
            return 0.0;
        }
    }
}

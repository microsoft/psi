// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.DataTypes
{
    /// <summary>
    /// Represents the globals for a script.
    /// </summary>
    /// <typeparam name="T">The type of the stream on which the script will operate.</typeparam>
    public class ScriptGlobals<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptGlobals{T}"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="env">The envelope.</param>
        public ScriptGlobals(T message, Envelope env)
        {
            this.m = message;
            this.e = env;
        }

#pragma warning disable SA1300 // Element should begin with an uppercase letter
        /// <summary>
        /// Gets the message.
        /// </summary>
        public T m { get; }

        /// <summary>
        /// Gets the envelope.
        /// </summary>
        public Envelope e { get; }
#pragma warning restore SA1300 // Element should begin with an uppercase letter
    }
}

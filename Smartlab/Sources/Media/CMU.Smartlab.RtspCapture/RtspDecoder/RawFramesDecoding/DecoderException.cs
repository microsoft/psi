// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace CMU.Smartlab.Rtsp
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// ..
    /// </summary>
    [Serializable]
    public class DecoderException : Exception
    {
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp

        /// <summary>
        /// Initializes a new instance of the <see cref="DecoderException"/> class.
        /// </summary>
        public DecoderException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecoderException"/> class.
        /// </summary>
        /// <param name="message">..</param>
        public DecoderException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecoderException"/> class.
        /// </summary>
        /// <param name="message">.</param>
        /// <param name="inner">..</param>
        public DecoderException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecoderException"/> class.
        /// </summary>
        /// <param name="info">.</param>
        /// <param name="context">..</param>
        protected DecoderException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}
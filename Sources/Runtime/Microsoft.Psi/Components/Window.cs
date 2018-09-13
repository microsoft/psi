// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Implements a buffering component. The component adds each incoming message to the buffer and trims the buffer
    /// by evaluating a remove condition.
    /// </summary>
    /// <typeparam name="T">The type of messages</typeparam>
    public class Window<T> : BufferProcess<T, IEnumerable<Message<T>>>
    {
        private readonly RelativeTimeInterval relativeTimeInterval;

        private int anchorMessageSequenceId = -1;
        private DateTime anchorMessageOriginatingTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="Window{T}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="relativeTimeInterval">The relative time interval over which to gather messages.</param>
        /// <param name="maxSize">Maximum buffer size.</param>
        public Window(Pipeline pipeline, RelativeTimeInterval relativeTimeInterval, int maxSize = int.MaxValue)
            : base(pipeline, maxSize)
        {
            this.relativeTimeInterval = relativeTimeInterval;
            this.InitializeLambdas(this.BufferRemoveCondition, this.BufferProcessor);
        }

        private bool BufferRemoveCondition(Message<T> message, DateTime currentTime)
        {
            return this.anchorMessageOriginatingTime > DateTime.MinValue && message.OriginatingTime < (this.anchorMessageOriginatingTime + this.relativeTimeInterval).Left;
        }

        private void BufferProcessor(IEnumerable<Message<T>> messageList, bool final, Emitter<IEnumerable<Message<T>>> emitter)
        {
            var messages = messageList.ToArray();
            var anchorMessageIndex = 0;
            if (this.anchorMessageSequenceId >= 0)
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    if (messages[i].Envelope.SequenceId == this.anchorMessageSequenceId)
                    {
                        anchorMessageIndex = i + 1;
                        break;
                    }
                }
            }

            if (anchorMessageIndex < messages.Length)
            {
                // compute the time interval from the next point we should output
                TimeInterval window = messages[anchorMessageIndex].OriginatingTime + this.relativeTimeInterval;

                // decide whether we should output - only output when we have seen enough (or know that nothing further is to be seen - `final`).
                // evidence that nothing else will appear in the window
                bool shouldOutputNextMessage = final || messages.Last().OriginatingTime > window.Right;

                if (shouldOutputNextMessage)
                {
                    // compute the buffer to return
                    var returnBuffer = messageList.Where(m => window.PointIsWithin(m.OriginatingTime));

                    // post it with the originating time of the anchor message
                    emitter.Post(returnBuffer, messages[anchorMessageIndex].OriginatingTime);

                    // set the sequence id for the last originating message that was posted
                    this.anchorMessageSequenceId = messages[anchorMessageIndex].SequenceId;
                    this.anchorMessageOriginatingTime = messages[anchorMessageIndex].OriginatingTime;
                }
            }
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;

    /// <summary>
    /// Component that implements a dynamic window stream operator.
    /// </summary>
    /// <typeparam name="TWindow">The type of messages on the window stream.</typeparam>
    /// <typeparam name="TInput">The type of messages on the input stream.</typeparam>
    /// <typeparam name="TOutput">The type of messages on the output stream.</typeparam>
    /// <remarks>The component implements a dynamic window operator over a stream of data. Messages
    /// on the incoming <see cref="WindowIn"/>stream are used to compute a relative time
    /// interval over the in input stream. The output is created by a function that has access
    /// to the window message and the computed buffer of messages on the input stream.</remarks>
    public class DynamicWindow<TWindow, TInput, TOutput> : ConsumerProducer<TInput, TOutput>
    {
        private readonly List<Message<TWindow>> windowBuffer = new ();
        private readonly List<Message<TInput>> inputBuffer = new ();
        private readonly Func<Message<TWindow>, (TimeInterval Window, DateTime ObsoleteTime)> dynamicWindowFunction;
        private readonly Func<Message<TWindow>, IEnumerable<Message<TInput>>, TOutput> outputCreator;

        private DateTime minimumObsoleteTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicWindow{TWindow, TInput, TOutput}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="windowCreator">The function that creates the actual window to use at every point, and specified the time point previous to which no future windows will extend.</param>
        /// <param name="outputCreator">A function that creates output messages given a message on the window-defining stream and a buffer of messages on the source stream.</param>
        /// <param name="name">An optional name for the component.</param>
        public DynamicWindow(
            Pipeline pipeline,
            Func<Message<TWindow>, (TimeInterval, DateTime)> windowCreator,
            Func<Message<TWindow>, IEnumerable<Message<TInput>>, TOutput> outputCreator,
            string name = nameof(DynamicWindow<TWindow, TInput, TOutput>))
            : base(pipeline, name)
        {
            this.dynamicWindowFunction = windowCreator;
            this.outputCreator = outputCreator;
            this.WindowIn = pipeline.CreateReceiver<TWindow>(this, this.ReceiveWindow, nameof(this.WindowIn));
            this.In.Unsubscribed += _ => this.Publish(true);
        }

        /// <summary>
        /// Gets the received for the input stream of window messages.
        /// </summary>
        public Receiver<TWindow> WindowIn { get; }

        /// <inheritdoc/>
        protected override void Receive(TInput data, Envelope envelope)
        {
            this.inputBuffer.Add(Message.Create(data.DeepClone(this.In.Recycler), envelope));
            this.Publish(false);
        }

        private void ReceiveWindow(TWindow data, Envelope envelope)
        {
            this.windowBuffer.Add(Message.Create(data.DeepClone(this.WindowIn.Recycler), envelope));
            this.Publish(false);
        }

        private void Publish(bool final)
        {
            while (this.TryPublish(final))
            {
            }
        }

        private bool TryPublish(bool final)
        {
            if (this.windowBuffer.Count == 0)
            {
                return false;
            }

            (var timeInterval, var obsoleteTime) = this.dynamicWindowFunction(this.windowBuffer[0]);
            if (timeInterval.IsNegative)
            {
                throw new ArgumentException("Dynamic window must be a positive time interval.");
            }

            if (timeInterval.Left < this.minimumObsoleteTime)
            {
                throw new ArgumentException("Dynamic window must not extend before previous obsolete time.");
            }

            if (!timeInterval.IsFinite)
            {
                throw new ArgumentException("Dynamic window must be finite (bounded at both ends).");
            }

            if (!final && (this.inputBuffer.Count == 0 || this.inputBuffer[this.inputBuffer.Count - 1].OriginatingTime < timeInterval.RightEndpoint.Point))
            {
                return false;
            }

            // if we have enough data, find the index of where to start and where to end
            var startIndex = this.inputBuffer.FindIndex(m => timeInterval.PointIsWithin(m.OriginatingTime));
            var endIndex = this.inputBuffer.FindLastIndex(m => timeInterval.PointIsWithin(m.OriginatingTime));

            // if endIndex is -1 (all inputBuffer messages are after the time interval)
            if (endIndex == -1)
            {
                // then post an empty buffer
                this.PostAndClearObsoleteInputs(obsoleteTime, Enumerable.Empty<Message<TInput>>());
                return true;
            }
            else if (startIndex == -1)
            {
                // o/w if the startIndex is -1 (all inputBuffer messages are before the time interval)
                // we cannot post yet, we are still waiting for data messages in the temporal range of the
                // entity, so return false
                return false;
            }
            else if (endIndex >= startIndex)
            {
                // o/w if the endIndex is strictly larger than the start index, then we have some overlap
                this.PostAndClearObsoleteInputs(obsoleteTime, this.inputBuffer.GetRange(startIndex, endIndex - startIndex + 1));
                return true;
            }
            else
            {
                // o/w if the endindex is strictly smaller than the startindex, that means the temporal interval
                // is caught in between the two different indices (endindex -> startindex)
                // in this case, we can post an empty buffer
                this.PostAndClearObsoleteInputs(obsoleteTime, Enumerable.Empty<Message<TInput>>());
                return true;
            }
        }

        private void PostAndClearObsoleteInputs(DateTime obsoleteTime, IEnumerable<Message<TInput>> inputs)
        {
            // check that obsolete times don't backtrack
            if (obsoleteTime < this.minimumObsoleteTime)
            {
                throw new ArgumentException("Dynamic window with obsolete time prior to previous window.");
            }

            this.minimumObsoleteTime = obsoleteTime;

            // post output
            var sourceMessage = this.windowBuffer[0];
            var value = this.outputCreator(sourceMessage, inputs);
            this.Out.Post(value, sourceMessage.OriginatingTime);

            // remove & recycle window and obsolete inputs
            this.windowBuffer.RemoveAt(0);
            this.WindowIn.Recycler.Recycle(sourceMessage.Data);

            if (this.inputBuffer.Any())
            {
                var obsoleteIndex = this.inputBuffer.FindIndex(m => m.OriginatingTime >= obsoleteTime);

                // if the is no message larger than or equal to the obsolete time
                if (obsoleteIndex == -1)
                {
                    // then all messages are obsolete
                    obsoleteIndex = this.inputBuffer.Count;
                }

                for (var i = 0; i < obsoleteIndex; i++)
                {
                    this.In.Recycler.Recycle(this.inputBuffer[i].Data);
                }

                this.inputBuffer.RemoveRange(0, obsoleteIndex);
            }
        }
    }
}

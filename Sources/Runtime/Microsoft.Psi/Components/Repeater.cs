// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    /// <summary>
    /// Outputs the last input message on every clock tick.
    /// </summary>
    /// <typeparam name="T">The input message type</typeparam>
    /// <typeparam name="TClock">The clock message type</typeparam>
    public sealed class Repeater<T, TClock> : ConsumerProducer<T, T>, IConsumer<TClock>
    {
        private readonly bool useInitialValue;
        private bool valueReceived = false;
        private T last;

        /// <summary>
        /// Initializes a new instance of the <see cref="Repeater{T, TClock}"/> class.
        /// </summary>
        /// <param name="pipeline">Pipeline to which this component belongs.</param>
        /// <param name="useInitialValue">Whether to seed with an initial value (before any messages seen).</param>
        /// <param name="initialValue">Initial value (repeated before any messages seen).</param>
        public Repeater(Pipeline pipeline, bool useInitialValue = false, T initialValue = default(T))
            : base(pipeline)
        {
            this.useInitialValue = useInitialValue;
            this.last = initialValue;
            this.ClockIn = pipeline.CreateReceiver<TClock>(this, this.ReceiveClock, nameof(this.ClockIn));
        }

        /// <summary>
        /// Gets clock signal receiver.
        /// </summary>
        public Receiver<TClock> ClockIn { get; }

        /// <inheritdoc />
        Receiver<TClock> IConsumer<TClock>.In => this.ClockIn;

        /// <inheritdoc />
        protected override void Receive(T data, Envelope e)
        {
            data.DeepClone(ref this.last);
            this.valueReceived = true;
        }

        private void ReceiveClock(Message<TClock> message)
        {
            if (this.useInitialValue || this.valueReceived)
            {
                this.Out.Post(this.last, message.OriginatingTime);
            }
        }
    }
}
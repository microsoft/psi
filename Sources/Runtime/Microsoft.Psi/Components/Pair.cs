// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System;

    /// <summary>
    /// Performs a wall-clock based pairing of streams; taking the last (or provided initial) value from the secondary.
    /// </summary>
    /// <typeparam name="TPrimary">The type the messages on the primary stream.</typeparam>
    /// <typeparam name="TSecondary">The type messages on the secondary stream.</typeparam>
    /// <typeparam name="TOut">The type of output message.</typeparam>
    public class Pair<TPrimary, TSecondary, TOut> : IProducer<TOut>
    {
        private readonly string name;
        private readonly Func<TPrimary, TSecondary, TOut> outputCreator;
        private bool secondaryValueReady = false;
        private TSecondary lastSecondaryValue = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pair{TPrimary, TSecondary, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="outputCreator">Mapping function from primary/secondary stream values to output type.</param>
        /// <param name="name">An optional name for the component.</param>
        public Pair(
            Pipeline pipeline,
            Func<TPrimary, TSecondary, TOut> outputCreator,
            string name = nameof(Pair<TPrimary, TSecondary, TOut>))
        {
            this.name = name;
            this.outputCreator = outputCreator;
            this.Out = pipeline.CreateEmitter<TOut>(this, nameof(this.Out));
            this.InPrimary = pipeline.CreateReceiver<TPrimary>(this, this.ReceivePrimary, nameof(this.InPrimary));
            this.InSecondary = pipeline.CreateReceiver<TSecondary>(this, this.ReceiveSecondary, nameof(this.InSecondary));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pair{TPrimary, TSecondary, TOut}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="outputCreator">Mapping function from primary/secondary stream values to output type.</param>
        /// <param name="initialSecondaryValue">An initial secondary value to be used until the first message arrives on the secondary stream.</param>
        /// <param name="name">An optional name for the component.</param>
        public Pair(
            Pipeline pipeline,
            Func<TPrimary, TSecondary, TOut> outputCreator,
            TSecondary initialSecondaryValue,
            string name = nameof(Pair<TPrimary, TSecondary, TOut>))
            : this(pipeline, outputCreator, name)
        {
            this.secondaryValueReady = true;
            this.lastSecondaryValue = initialSecondaryValue;
        }

        /// <summary>
        /// Gets the output emitter.
        /// </summary>
        public Emitter<TOut> Out { get; }

        /// <summary>
        /// Gets the primary receiver.
        /// </summary>
        public Receiver<TPrimary> InPrimary { get; }

        /// <summary>
        /// Gets the secondary receiver.
        /// </summary>
        public Receiver<TSecondary> InSecondary { get; }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void ReceivePrimary(TPrimary message, Envelope e)
        {
            // drop unless a secondary value has been received or using an initial value
            if (this.secondaryValueReady)
            {
                this.Out.Post(this.outputCreator(message, this.lastSecondaryValue), e.OriginatingTime);
            }
        }

        private void ReceiveSecondary(TSecondary message, Envelope e)
        {
            message.DeepClone(ref this.lastSecondaryValue);
            this.secondaryValueReady = true;
        }
    }
}
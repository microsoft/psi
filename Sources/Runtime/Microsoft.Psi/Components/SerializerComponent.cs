// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Serializer optimized for streaming scenarios, where buffers and instances can be cached.
    /// </summary>
    /// <typeparam name="T">The type of messages to serialize.</typeparam>
    internal sealed class SerializerComponent<T> : ConsumerProducer<Message<T>, Message<BufferReader>>
    {
        private readonly SerializationContext context;
        private readonly BufferWriter serializationBuffer = new BufferWriter(16);
        private readonly SerializationHandler<T> handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerComponent{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="serializers">Known serializers.</param>
        /// <param name="name">An optional name for the component.</param>
        internal SerializerComponent(Pipeline pipeline, KnownSerializers serializers, string name = nameof(SerializerComponent<T>))
            : base(pipeline, name)
        {
            this.context = new SerializationContext(serializers);
            this.handler = serializers.GetHandler<T>();
        }

        /// <inheritdoc />
        protected override void Receive(Message<T> data, Envelope e)
        {
            this.serializationBuffer.Reset();
            this.handler.Serialize(this.serializationBuffer, data.Data, this.context);
            this.context.Reset();
            var outputBuffer = new BufferReader(this.serializationBuffer);

            // preserve the envelope we received
            var resultMsg = Message.Create(outputBuffer, data.Envelope);
            this.Out.Post(resultMsg, e.OriginatingTime);
        }
    }
}

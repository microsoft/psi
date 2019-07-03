// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Components
{
    using System.Collections.Generic;
    using Microsoft.Psi.Common;
    using Microsoft.Psi.Serialization;

    /// <summary>
    /// Dynamic deserializer optimized for streaming scenarios, where buffers and instances can be cached.
    /// </summary>
    /// <remarks>Uses TypeSchema to construct message type as dynamic primitive and/or ExpandoObject of dynamic.</remarks>
    public sealed class DynamicDeserializerComponent : ConsumerProducer<Message<BufferReader>, dynamic>
    {
        private readonly DynamicMessageDeserializer deserializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicDeserializerComponent"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to which to attach.</param>
        /// <param name="typeName">Type name.</param>
        /// <param name="schemas">Known type schemas.</param>
        public DynamicDeserializerComponent(Pipeline pipeline, string typeName, IDictionary<string, TypeSchema> schemas)
            : base(pipeline)
        {
            this.deserializer = new DynamicMessageDeserializer(typeName, schemas);
        }

        /// <summary>
        /// Deserializes and instance from the specified reader.
        /// </summary>
        /// <param name="message">The byte data to deserialize.</param>
        /// <param name="envelope">The envelope of the message.</param>
        protected override void Receive(Message<BufferReader> message, Envelope envelope)
        {
            this.Out.Deliver(this.deserializer.Deserialize(message.Data), message.Envelope);
        }
    }
}

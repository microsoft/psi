using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Psi;
using Microsoft.Psi.Components; 
using Microsoft.Psi.Audio;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Media;
using Microsoft.Psi.Interop.Format;
using Microsoft.Psi.Interop.Transport;

namespace CMU.Smartlab.Communication
{
    /// <summary>
    /// Merge one or more streams (T) into a single stream (Message{T}) interleaved in wall-clock time.
    /// </summary>
    /// <remarks>Messages are produced in the order they arrive, in wall-clock time; not necessarily in originating-time order.</remarks>
    /// <typeparam name="T">The type of the messages.</typeparam>
    public class SmartlabMerge<T> : IProducer<Message<T>>
    {
        private readonly Pipeline pipeline;
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartlabMerge{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for this component.</param>
        public SmartlabMerge(Pipeline pipeline, string name = nameof(SmartlabMerge<T>))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.Out = pipeline.CreateEmitter<Message<T>>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets the output emitter.
        /// </summary>
        public Emitter<Message<T>> Out { get; }

        /// <summary>
        /// Add input receiver.
        /// </summary>
        /// <param name="name">The unique debug name of the receiver.</param>
        /// <returns>Receiver.</returns>
        public Receiver<T> AddInput(string name)
        {
            return this.pipeline.CreateReceiver<T>(this, this.Receive, name);
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Receive(T message, Envelope e)
        {
            Console.WriteLine("SmartlabMerge, Receive - message: '{0}'", message);
            this.Out.Post(Message.Create(message, e), this.pipeline.GetCurrentTime());
        }
    }
}
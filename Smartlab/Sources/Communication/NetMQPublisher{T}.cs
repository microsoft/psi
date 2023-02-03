
namespace CMU.Smartlab.Communication
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NetMQ;
    using NetMQ.Sockets;
    using Microsoft.Psi;
    using Microsoft.Psi.Interop.Serialization;

    /// <summary>
    /// NetMQ (ZeroMQ) publisher component.
    /// </summary>
    /// <typeparam name="T">Message type.</typeparam>
    public class NetMQPublisher<T> : NetMQPublisher, IConsumer<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetMQPublisher{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="topic">Topic name.</param>
        /// <param name="address">Connection string.</param>
        /// <param name="serializer">Format serializer with which messages are serialized.</param>
        /// <param name="name">An optional name for the component.</param>
        public NetMQPublisher(Pipeline pipeline, string topic, string address, IFormatSerializer serializer, string name = nameof(NetMQPublisher<T>))
            : base(pipeline, address, serializer, name)
        {
            this.In = this.AddTopic<T>(topic);
        }

        /// <inheritdoc />
        public Receiver<T> In { get; }

        public void publish_hellos<Type>() {
            // while (true) {
            for (int i=0; i < 10; i++) {
                Console.WriteLine("NetMQPublisher publish_hello");
                this.socket.SendMoreFrame("PSI_To_Remote").SendFrame("Hello from psi!");
                Thread.Sleep(2000); 
            }
        }

    }
}
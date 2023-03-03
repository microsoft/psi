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

namespace CMU.Smartlab.Communication
{
    /// <summary>
    /// Merge one or more streams (T) into a single stream (Message{T}) interleaved in wall-clock time.
    /// </summary>
    /// <remarks>Messages are produced in the order they arrive, in wall-clock time; not necessarily in originating-time order.</remarks>
    /// <typeparam name="T">The type of the messages.</typeparam>
    // public class SmartlabMerge<T> : IProducer<T>, ISourceComponent, IDisposable
    public class SmartlabMerge<T> : IProducer<T>
    {
        private readonly Pipeline pipeline;
        private readonly string name;
        private bool useSourceOriginatingTimes; 
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SmartlabMerge{T}"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="name">An optional name for this component.</param>
        public SmartlabMerge(Pipeline pipeline, string name = nameof(SmartlabMerge<T>))
        {
            this.pipeline = pipeline;
            this.name = name;
            this.Out = pipeline.CreateEmitter<T>(this, nameof(this.Out));
            this.useSourceOriginatingTimes = true; 
            // this.IDictionaryIn = pipeline.CreateReceiver<IDictionary<string,object>>(this, ReceiveIDictionary, nameof(this.IDictionaryIn));
        }
        /// <summary>
        /// Gets the output emitter.
        /// </summary>
        public Emitter<T> Out { get; }

        /// <summary>
        /// Add input receiver.
        /// </summary>
        /// <param name="name">The unique debug name of the receiver.</param>
        /// <returns>Receiver.</returns>
        public Receiver<T> AddInput(string name)
        {
            return this.pipeline.CreateReceiver<T>(this, this.Receive, name);
        }

        // public Receiver<IDictionary<string,object>> AddInput(string name)
        // {
        //     return this.pipeline.CreateReceiver<T>(this, this.Receive, name);
        // }


        // Receiver that encapsulates the Dictionary input stream
        public Receiver<IDictionary<string,object>> IDictionaryIn { get; private set; }

        // private static Boolean processIDictionary(IDictionary<string,object> dictionaryIn)
        private static Boolean processIDictionary(object message)
        {

            // Console.WriteLine("SmartlabMerge processIDictionary -  enter");  
            IDictionary<string,object> dictionaryIn = (IDictionary<string,object>)message; 
            string messageToBazaar = null;  
            IDictionary<string,object> messageDictionary = new Dictionary<string,object>(); 
            foreach (KeyValuePair<string,object> kvp in dictionaryIn) {
                messageDictionary.Add(kvp.Key,kvp.Value); 
                Console.WriteLine("SmartlabMerge processIDictionary: message - key: '{0}'  --  value: '{1}'", kvp.Key,kvp.Value); 
            }
            return true;
        }

        /// <inheritdoc/>
        public override string ToString() => this.name;

        private void Receive(T message, Envelope e)
        {
            // Console.WriteLine("SmartlabMerge, Receive T - enter");
            Boolean uselessValue = processIDictionary(message); 
            // Console.WriteLine("SmartlabMerge, Receive T - posting message");
            this.Out.Post(message, this.useSourceOriginatingTimes ? e.OriginatingTime : this.pipeline.GetCurrentTime());
        }
    }
}
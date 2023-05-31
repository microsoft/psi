using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMU.Smartlab.Communication
{
    public class NetMqPublisher
    {
        private object _lockObject = new object();

        private PublisherSocket _publisherSocket;

        public NetMqPublisher()
        {
            _publisherSocket = new PublisherSocket();
            _publisherSocket.Options.SendHighWatermark = 1000;
            _publisherSocket.Bind("tcp://127.0.0.1:8888");
        }

        public NetMqPublisher(string endPoint)
        {
            _publisherSocket = new PublisherSocket();
            _publisherSocket.Options.SendHighWatermark = 1000;
            _publisherSocket.Bind(endPoint);
        }


        public void Dispose()
        {
            lock (_lockObject)
            {
                _publisherSocket.Close();
                _publisherSocket.Dispose();
            }
        }

        /// <summary>
        /// publish the messages
        /// </summary>
        /// <param name="topicName">topic</param>
        /// <param name="data">content</param>
        public void Publish(string topicName, string data)
        {
            lock (_lockObject)
            {
                _publisherSocket.SendMoreFrame(topicName).SendFrame(data);
            }
        }
    }
}

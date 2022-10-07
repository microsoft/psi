using System;
using CMU.Smartlab.Communication;
using NetMQ;
using NetMQ.Sockets;


namespace TcpDemo
{
    class Program
    {
        static private ZeroMqManager manager;
        static void Main(string[] args)
        {
            try
            {
                manager.Send("Hello BNU");
                Console.WriteLine("Send to server!!");
                string str = manager.Recieve();
                Console.WriteLine(str);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}

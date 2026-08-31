
using Microsoft.Psi.Interop.Rendezvous;
using Microsoft.Psi;
using Microsoft.Psi.Media;
using Microsoft.Psi.Media_Interop;
using System.Net.Sockets;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Interop.Serialization;

namespace TestingConsole
{
    public class Program
    {
        /// <summary>
        /// Provides serialization format for string type.
        /// </summary>
        public class PsiFormatString
        {
            /// <summary>
            /// Gets the format for serializing and deserializing string values.
            /// </summary>
            /// <returns>A Format instance for string serialization.</returns>
            public static Format<string> GetFormat()
            {
                return new Format<string>(WriteString, ReadSring);
            }

            /// <summary>
            /// Writes a string to a binary writer.
            /// </summary>
            /// <param name="data">The string to write.</param>
            /// <param name="writer">The binary writer to write to.</param>
            public static void WriteString(string data, BinaryWriter writer)
            {
                writer.Write(data);
            }

            /// <summary>
            /// Reads a string from a binary reader.
            /// </summary>
            /// <param name="reader">The binary reader to read from.</param>
            /// <returns>The deserialized string.</returns>
            public static string ReadSring(BinaryReader reader)
            {
                return reader.ReadString();
            }
        }

        static void proc(string message, DateTime dateTime)
        {
            Console.WriteLine($"{message} @{dateTime}");
        }

        static void Main(string[] args)
        {
            //RendezvousServer server = new RendezvousServer(13331);
            //server.Start();
            //server.Rendezvous.ProcessAdded += (_, process) =>
            //{
            //    Console.WriteLine($"Process added: {process.Name}");
            //};

            Pipeline pipeline = Pipeline.Create();
            UdpSource<string> udpSource = new UdpSource<string>(pipeline, 15552, PsiFormatString.GetFormat());
            udpSource.OnMessageReceived += proc;
            RendezvousClient client = new RendezvousClient("localhost", 13331);
            client.Start();
            pipeline.RunAsync();
            //udpSource.Start((e)=>{ });
            //int i = 0;
            //while (true)
            //{
            //    Console.WriteLine("Press");
            //    Console.ReadLine();
            //    client.Rendezvous.TryAddProcess(new Rendezvous.Process($"test{i++}"));
            //}


            Console.WriteLine("Press");
            Console.ReadLine();
            //List<string> cameras = MediaCapture.GetAvailableCameras();
            //List<string> CameraCaptureFormat = new List<string>();

            //var vals = MediaCapture.GetAvailableFormats(cameras.First());
            //foreach (CaptureFormat format in vals)
            //{
            //    CameraCaptureFormat.Add($"{format.nWidth}x{format.nHeight}@{format.nFrameRateNumerator}");
            //}
        }
    }
}

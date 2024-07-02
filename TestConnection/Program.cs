using Microsoft.Psi;
using Microsoft.Psi.Remoting;
using Microsoft.Psi.Interop.Serialization;
using Microsoft.Psi.Interop.Rendezvous;

namespace TestConnection
{
    public class PsiFormatMatrix4x4
    {
        public Format<System.Numerics.Matrix4x4> GetFormat()
        {
            return new Format<System.Numerics.Matrix4x4>(WriteMatrix4x4, ReadMatrix4x4);
        }

        public void WriteMatrix4x4(System.Numerics.Matrix4x4 matrix, BinaryWriter writer)
        {
            writer.Write((double)matrix.M11);
            writer.Write((double)matrix.M12);
            writer.Write((double)matrix.M13);
            writer.Write((double)matrix.M14);
            writer.Write((double)matrix.M21);
            writer.Write((double)matrix.M22);
            writer.Write((double)matrix.M23);
            writer.Write((double)matrix.M24);
            writer.Write((double)matrix.M31);
            writer.Write((double)matrix.M32);
            writer.Write((double)matrix.M33);
            writer.Write((double)matrix.M34);
            writer.Write((double)matrix.M41);
            writer.Write((double)matrix.M42);
            writer.Write((double)matrix.M43);
            writer.Write((double)matrix.M44);
        }

        public System.Numerics.Matrix4x4 ReadMatrix4x4(BinaryReader reader)
        {
            System.Numerics.Matrix4x4 matrix = new System.Numerics.Matrix4x4();
            matrix.M11 = (float)reader.ReadDouble();
            matrix.M12 = (float)reader.ReadDouble();
            matrix.M13 = (float)reader.ReadDouble();
            matrix.M14 = (float)reader.ReadDouble();
            matrix.M21 = (float)reader.ReadDouble();
            matrix.M22 = (float)reader.ReadDouble();
            matrix.M23 = (float)reader.ReadDouble();
            matrix.M24 = (float)reader.ReadDouble();
            matrix.M31 = (float)reader.ReadDouble();
            matrix.M32 = (float)reader.ReadDouble();
            matrix.M33 = (float)reader.ReadDouble();
            matrix.M34 = (float)reader.ReadDouble();
            matrix.M41 = (float)reader.ReadDouble();
            matrix.M42 = (float)reader.ReadDouble();
            matrix.M43 = (float)reader.ReadDouble();
            matrix.M44 = (float)reader.ReadDouble();
            return matrix;
        }
    }


    public class PsiFormatDateTime
    {
        public Format<System.DateTime> GetFormat()
        {
            return new Format<System.DateTime>(WriteDateTime, ReadDateTime);
        }

        public void WriteDateTime(System.DateTime dateTime, BinaryWriter writer)
        {
            writer.Write(dateTime.ToBinary());
        }

        public System.DateTime ReadDateTime(BinaryReader reader)
        {
            return System.DateTime.FromBinary(reader.ReadInt64());
        }
    }

    internal class Program
    {
        static private void Connection<T>(string name, Rendezvous.TcpSourceEndpoint? source, Pipeline p, Format<T> deserializer)
        {
            source?.ToTcpSource<T>(p, deserializer, null, true, name).Do((d, e) => { Console.WriteLine($"Recieve {name} data @{e.OriginatingTime} : {d}"); });
        }

        static void Quest2Demo(Pipeline p)
        {
            var host = "192.168.56.1";
            var remoteClock = new RemoteClockExporter(port: 11510);

            bool canStart = false;
            var process = new Rendezvous.Process("Server", new[] { remoteClock.ToRendezvousEndpoint(host) });
            var server = new RendezvousServer();
            server.Rendezvous.TryAddProcess(process);
            server.Rendezvous.ProcessAdded += (_, pr) =>
            {
                Console.WriteLine($"Process {pr.Name}");
                if (pr.Name == "Unity")
                {
                    foreach (var endpoint in pr.Endpoints)
                    {
                        if (endpoint is Rendezvous.TcpSourceEndpoint)
                        {
                            Rendezvous.TcpSourceEndpoint? source = endpoint as Rendezvous.TcpSourceEndpoint;
                            foreach (var stream in endpoint.Streams)
                            {
                                Console.WriteLine($"\tStream {stream.StreamName}");
                                switch (stream.StreamName)
                                {
                                    case "PositionLeft":
                                    case "PositionRight":
                                    case "Player":
                                        Connection<System.Numerics.Matrix4x4>(stream.StreamName, source, p, new PsiFormatMatrix4x4().GetFormat());
                                        break;
                                    case "Time":
                                        Connection<DateTime>(stream.StreamName, source, p, new PsiFormatDateTime().GetFormat());
                                        break;
                                }
                            }
                        }
                        canStart = true;
                    }
                }
            };
            server.Error += (s, e) => { Console.WriteLine(e.Message); Console.WriteLine(e.HResult); };
            server.Start();
            while (!canStart) Thread.Sleep(500);
            Thread.Sleep(500);
        }
        static void Main(string[] args)
        {
            // Create the \psi pipeline
            Pipeline pipeline = Pipeline.Create("Subpipline Removal", enableDiagnostics: false);
            //var store = PsiStore.Create(pipeline, "Diagnostics", $"F:/Stores/Diagnostics/");
            //store.Write(pipeline.Diagnostics, "Pipeline");
            Quest2Demo(pipeline);


            // Start the pipeline running
            pipeline.RunAsync();
            // Waiting for an out key
            Console.WriteLine("Press any key to stop the application.");
            Console.ReadLine();
            // Stop correctly the pipeline.
            pipeline.Dispose();
        }
    }
}

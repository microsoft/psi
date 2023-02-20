using Microsoft.Psi;
using Microsoft.Psi.Remoting;

namespace TestConnection
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create the \psi pipeline
            Pipeline pipeline = Pipeline.Create("TestConnection");

            RemoteExporter exporter = new RemoteExporter(pipeline, 11411, TransportKind.Tcp);
            // Create a timer component that produces a message every second
            var timer = Timers.Timer(pipeline, TimeSpan.FromSeconds(1));

            // For each message created by the timer
            exporter.Exporter.Write<TimeSpan>(timer.Out, "Test");

            // Start the pipeline running
            pipeline.RunAsync();

            // Wainting for an out key
            Console.WriteLine("Press any key to stop the application.");
            Console.ReadLine();
            // Stop correctly the pipeline.
            pipeline.Dispose();
        }
    }
}

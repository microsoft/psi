using Microsoft.Psi;
using Microsoft.Psi.Remoting;

namespace TestConnection
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create the \psi pipeline
            Pipeline pipeline = Pipeline.Create("Subpipline Removal");
            var timer = Timers.Timer(pipeline, TimeSpan.FromSeconds(1));
            timer.Out.Do(t =>
            {
                Console.WriteLine($"\tPipeline timer.");
            });
            // Start the pipeline running
            pipeline.RunAsync();
            Console.WriteLine("Pipeline Run Asynch.");

            Subpipeline sub1 = new Subpipeline(pipeline); 
            var subtimer1 = Timers.Timer(sub1, TimeSpan.FromSeconds(1));
            subtimer1.Out.Do(t =>
            {
                Console.WriteLine($"\tSubpipeline 1 timer.");
            });
            RemoteExporter exporter1 = new RemoteExporter(sub1, 11511, TransportKind.Tcp);
            exporter1.Exporter.Write(subtimer1.Out, "SubExporter1");
            sub1.RunAsync();
            Console.WriteLine("Subpipeline 1 Run Asynch.");
            Thread.Sleep(3000);
            Subpipeline sub2 = new Subpipeline(pipeline);
            var subtimer2 = Timers.Timer(sub2, TimeSpan.FromSeconds(1));
            subtimer2.Out.Do(t =>
            {
                Console.WriteLine($"\tSubpipeline 2 timer.");
            });
            RemoteExporter exporter2 = new RemoteExporter(sub2, 11512, TransportKind.Tcp);
            exporter2.Exporter.Write(subtimer2.Out, "SubExporter2");
            sub2.RunAsync();
            Console.WriteLine("Subpipeline 2 Run Asynch.");
            Thread.Sleep(5000);
            Console.WriteLine("Dispose Subpipeline 1.");
            sub1.Dispose();
            Thread.Sleep(5000);
            Console.WriteLine("Dispose Subpipeline 2.");
            sub2.Dispose();
            Thread.Sleep(5000);
            Subpipeline sub3 = new Subpipeline(pipeline);
            var subtimer3 = Timers.Timer(sub3, TimeSpan.FromSeconds(1));
            subtimer3.Out.Do(t =>
            {
                Console.WriteLine($"\tSubpipeline 3 timer.");
            });
            RemoteExporter exporter3 = new RemoteExporter(sub3, 11513, TransportKind.Tcp);
            exporter3.Exporter.Write(subtimer3.Out, "SubExporter3");
            sub3.RunAsync();
            Console.WriteLine("Subpipeline 3 Run Asynch.");
            // Wainting for an out key
            Console.WriteLine("Press any key to stop the application.");
            Console.ReadLine();
            // Stop correctly the pipeline.
            pipeline.Dispose();
        }
    }
}

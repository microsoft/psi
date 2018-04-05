// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace ArmControlROSSample
{
    using System;
    using System.Threading;
    using Microsoft.Psi;

    public class Program
    {
        private const string RosSlave = "127.0.0.1"; // replace with your dev machine
        private const string RosMaster = "127.0.0.1"; // replace with your ROS machine

        private static UArm uarm = new UArm(RosSlave, RosMaster);

        public static void Main(string[] args)
        {
            Console.WriteLine("UArm Metal Controller");
            Console.WriteLine();
            Console.WriteLine("P - Pump on/off");
            Console.WriteLine("B - Beep");
            Console.WriteLine("U - Up");
            Console.WriteLine("D - Down");
            Console.WriteLine("◀ - Left");
            Console.WriteLine("▶ - Right");
            Console.WriteLine("▲ - Forward");
            Console.WriteLine("▼ - Back");
            Console.WriteLine("Q - Quit");

            // NonPsiVersion();
            PsiVersion();
        }

        private static void NonPsiVersion()
        {
            uarm.Connect();
            uarm.PositionChanged += (_, p) =>
            {
                Console.WriteLine($"Position: x={p.Item1} y={p.Item2} z={p.Item3}");
            };
            var pump = false;

            while (true)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.P:
                        pump = !pump;
                        uarm.Pump(pump);
                        break;
                    case ConsoleKey.B:
                        uarm.Beep(500, 0.1f);
                        break;
                    case ConsoleKey.U:
                        uarm.RelativePosition(0, 0, -10);
                        break;
                    case ConsoleKey.D:
                        uarm.RelativePosition(0, 0, 10);
                        break;
                    case ConsoleKey.LeftArrow:
                        uarm.RelativePosition(0, -10, 0);
                        break;
                    case ConsoleKey.RightArrow:
                        uarm.RelativePosition(0, 10, 0);
                        break;
                    case ConsoleKey.UpArrow:
                        uarm.RelativePosition(-10, 0, 0);
                        break;
                    case ConsoleKey.DownArrow:
                        uarm.RelativePosition(10, 0, 0);
                        break;
                    case ConsoleKey.Q:
                        uarm.Disconnect();
                        return;
                }
            }
        }

        private static void PsiVersion()
        {
            using (var pipeline = Pipeline.Create())
            {
                var arm = new UArmComponent(pipeline, uarm);
                var keys = Generators.Timer(pipeline, TimeSpan.FromMilliseconds(10), (dt, ts) => Console.ReadKey(true).Key);
                var pump = false;
                keys.Where(k => k == ConsoleKey.P).Select(_ => pump = !pump).PipeTo(arm.Pump);
                keys.Where(k => k == ConsoleKey.B).Select(_ => Tuple.Create(500f, 0.1f)).PipeTo(arm.Beep);
                keys.Select(k =>
                {
                    switch (k)
                    {
                        case ConsoleKey.U: return Tuple.Create(0f, 0f, -10f);
                        case ConsoleKey.D: return Tuple.Create(0f, 0f, 10f);
                        case ConsoleKey.LeftArrow: return Tuple.Create(0f, -10f, 0f);
                        case ConsoleKey.RightArrow: return Tuple.Create(0f, 10f, 0f);
                        case ConsoleKey.UpArrow: return Tuple.Create(-10f, 0f, 0f);
                        case ConsoleKey.DownArrow: return Tuple.Create(10f, 0f, 0f);
                        default: return null;
                    }
                }).Where(p => p != null).PipeTo(arm.RelativePosition);
                arm.PositionChanged.Do(p => Console.WriteLine($"Position: x={p.Item1} y={p.Item2} z={p.Item3}"));
                var quit = false;
                keys.Where(k => k == ConsoleKey.Q).Do(_ => quit = true);
                pipeline.RunAsync();
                while (!quit)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}

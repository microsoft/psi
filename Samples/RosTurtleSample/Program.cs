// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace TurtleROSSample
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi;

    /// <summary>
    /// ROS turtle sample program.
    /// </summary>
    public class Program
    {
        private const string RosSlave = "127.0.0.1"; // replace with your dev machine
        private const string RosMaster = "127.0.0.1"; // replace with your ROS machine

        /// <summary>
        /// Main entry point.
        /// </summary>
        public static void Main()
        {
            using (var pipeline = Pipeline.Create())
            {
                var turtle = new TurtleComponent(pipeline, new Turtle(RosSlave, RosMaster));
                turtle.PoseChanged.Do(p => Console.WriteLine($"x={p.Item1} y={p.Item2} theta={p.Item3}"));
                var keys = Generators.Sequence(pipeline, Keys(), TimeSpan.FromMilliseconds(10));
                keys.Select(k =>
                {
                    var linear = k == ConsoleKey.UpArrow ? 1f : k == ConsoleKey.DownArrow ? -1f : 0f;
                    var angular = k == ConsoleKey.LeftArrow ? 1f : k == ConsoleKey.RightArrow ? -1f : 0f;
                    return (linear, angular);
                }).PipeTo(turtle.Velocity);
                pipeline.Run();
            }
        }

        private static IEnumerable<ConsoleKey> Keys()
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;
                yield return key;

                if (key == ConsoleKey.Q)
                {
                    yield break;
                }
            }
        }
    }
}

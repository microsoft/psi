// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi
{
    using System;
    using System.IO;
    using Microsoft.Psi;
    using Microsoft.Psi.Components;

    // This example loads a text file and outputs each line
    // a line is expected to contain a timestamp and a value, which can be either an int or a string
    // e.g.:
    // 1000, 10
    // 2000, this is  a test
    // 3000, of multi-stream generator
    // 4000, 11
    public class GeneratorSample : Generator
    {
        private TextReader reader;

        public GeneratorSample(Pipeline p, string fileName)
            : base(p)
        {
            this.OutInt = p.CreateEmitter<int>(this, nameof(this.OutInt));
            this.OutString = p.CreateEmitter<string>(this, nameof(this.OutString));
            this.reader = File.OpenText(fileName);
        }

        public Emitter<int> OutInt { get; }

        public Emitter<string> OutString { get; }

        // read the file line by line,
        // and post either an int value or a string value to the appropriate output stream
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            string line = this.reader.ReadLine();
            if (line == null)
            {
                return DateTime.MaxValue; // no more data
            }

            // first value in each line is the timestamp (ticks),
            // second is either an int or a string, separated by ','
            var parts = line.Split(new[] { ',' }, 2);

            // parse the originating time.
            // If the data doesn't come with a timestamp, pipeline.GetCurrentTime() can be used instead
            var originatingTime = DateTime.Parse(parts[0].Trim());
            if (int.TryParse(parts[1].Trim(), out int intValue))
            {
                this.OutInt.Post(intValue, originatingTime);
            }
            else
            {
                this.OutString.Post(parts[1], originatingTime);
            }

            return originatingTime;
        }
    }
}

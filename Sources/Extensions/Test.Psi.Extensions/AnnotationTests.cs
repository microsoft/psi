// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi;
    using Microsoft.Psi.Extensions.Annotations;
    using Microsoft.Psi.Extensions.Data;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AnnotationTests
    {
        private string name = "AnnotationTests";
        private string path = Path.GetTempPath();
        private AnnotationSchema booleanSchema;
        private AnnotatedEventDefinition definition;
        private JsonStreamMetadata metadata;

        private enum Transitions
        {
            None,
            BelowToBetween,
            BetweenToAbove,
            AboveToBetween,
            BetweenToBelow
        }

        /// <summary>
        /// Initialization for each unit test.
        /// </summary>
        [TestInitialize]
        public void Initialize()
        {
            this.booleanSchema = new AnnotationSchema("Boolean");
            this.booleanSchema.AddSchemaValue(null, Color.Gray);
            this.booleanSchema.AddSchemaValue("false", Color.Red);
            this.booleanSchema.AddSchemaValue("true", Color.Green);

            this.definition = new AnnotatedEventDefinition("Definition");
            this.definition.Schemas.Add(this.booleanSchema);

            this.metadata = new JsonStreamMetadata("Range", 1, typeof(AnnotatedEvent).AssemblyQualifiedName, this.name, this.path);
        }

        /// <summary>
        /// Test writing and reading an empty Annotation store.
        /// </summary>
        [TestMethod]
        public void EmptyAnnotationStore()
        {
            // write store
            var path = string.Empty;
            var stream = new List<Message<AnnotatedEvent>>();
            using (var writer = new AnnotationSimpleWriter(this.definition))
            {
                writer.CreateStore(this.name, this.path);
                path = writer.Path;
                writer.CreateStream(this.metadata, stream);
                writer.WriteAll(ReplayDescriptor.ReplayAll);

                // read store
                using (var reader = new AnnotationSimpleReader(this.name, this.path))
                {
                    Assert.AreEqual<string>(reader.Name, writer.Name);
                    Assert.AreEqual<string>(reader.Path, writer.Path);
                    Assert.AreEqual(reader.AvailableStreams.Count(), 1);

                    // verify store
                    var metadata = reader.AvailableStreams.First() as JsonStreamMetadata;
                    Assert.IsNotNull(metadata);
                    Assert.AreEqual(metadata.Name, "Range");
                    Assert.AreEqual(metadata.Id, 1);
                    Assert.AreEqual(metadata.TypeName, typeof(AnnotatedEvent).AssemblyQualifiedName);
                    Assert.AreEqual(metadata.PartitionName, this.name);
                    Assert.AreEqual(metadata.PartitionPath, path);

                    Assert.AreEqual(reader.Definition.Name, "Definition");
                    Assert.AreEqual(reader.Definition.Schemas.Count, 1);
                    Assert.AreEqual(reader.Definition.Schemas[0].Name, "Boolean");
                    Assert.AreEqual(reader.Definition.Schemas[0].Values.Count, 3);
                }
            }

            // delete store
            DeleteStore(path);
        }

        /// <summary>
        /// Test writing and reading an empty Annotation store.
        /// </summary>
        [TestMethod]
        public void AnnotationPipeline()
        {
            string path = this.path;
            using (var pipeline = Pipeline.Create(this.name))
            {
                var store = AnnotationStore.Create(pipeline, this.name, path, this.definition);
                path = store.Path;
                var timer = Generators.Timer<double>(pipeline, TimeSpan.FromMilliseconds(10), (dt, ts) => (double)ts.Ticks);
                var sin = timer.Select(l => Math.Sin(l / 10000000d));
                var annotations = sin
                    .Select((d, env) => Tuple.Create(d, env.OriginatingTime))
                    .History(2)
                    .Select((p) => Tuple.Create(p.First(), p.Last(), this.ComputeTransition(p.First().Item1, p.Last().Item1)))
                    .Where((t) => t.Item3 != Transitions.None)
                    .History(2)
                    .Process<IEnumerable<Tuple<Tuple<double, DateTime>, Tuple<double, DateTime>, Transitions>>, AnnotatedEvent>((t, env, s) =>
                    {
                        var first = t.First();
                        var second = t.Last();
                        if (first != second)
                        {
                            var annotatedEvent = this.definition.CreateAnnotatedEvent(first.Item2.Item2, second.Item1.Item2);
                            this.SetAnnotation(annotatedEvent, first.Item3);
                            s.Post(annotatedEvent, env.OriginatingTime);
                        }
                    })
                    .Write("Range", store);
                pipeline.Run(TimeSpan.FromSeconds(10));
            }

            // delete store
            DeleteStore(path);
        }

        private static void DeleteStore(string path)
        {
            Directory.Delete(path, true);
        }

        /// <summary>
        /// Tests the equality of two AnnotatedEvent instances.
        /// </summary>
        /// <param name="reference">Reference AnnotatedEvent.</param>
        /// <param name="actual">Actual AnnotatedEvent.</param>
        private void Equals(AnnotatedEvent reference, AnnotatedEvent actual)
        {
            Assert.IsFalse(actual == null && reference != null);
            if (reference != null)
            {
                Assert.IsNotNull(actual);
                Assert.AreEqual<DateTime>(actual.StartTime, reference.StartTime);
                Assert.AreEqual<DateTime>(actual.EndTime, reference.EndTime);
                Assert.AreEqual<TimeSpan>(actual.Duration, reference.Duration);

                Assert.IsFalse(actual.Annotations == null && reference.Annotations != null);
                if (reference.Annotations != null)
                {
                    Assert.IsNotNull(actual.Annotations);
                    Assert.AreEqual<int>(actual.Annotations.Count, reference.Annotations.Count);
                    for (int i = 0; i < actual.Annotations.Count; i++)
                    {
                        Assert.AreEqual<string>(actual.Annotations[i], reference.Annotations[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Tests the equality of two TimeInterval instances.
        /// </summary>
        /// <param name="reference">Reference TimeInterval.</param>
        /// <param name="actual">Actual TimeInterval.</param>
        private void Equals(TimeInterval reference, TimeInterval actual)
        {
            Assert.AreEqual(actual.LeftEndpoint.Bounded, reference.LeftEndpoint.Bounded);
            Assert.AreEqual(actual.LeftEndpoint.Point, reference.LeftEndpoint.Point);
            Assert.AreEqual(actual.LeftEndpoint.Inclusive, reference.LeftEndpoint.Inclusive);
            Assert.AreEqual(actual.RightEndpoint.Bounded, reference.RightEndpoint.Bounded);
            Assert.AreEqual(actual.RightEndpoint.Point, reference.RightEndpoint.Point);
            Assert.AreEqual(actual.RightEndpoint.Inclusive, reference.RightEndpoint.Inclusive);
        }

        private Transitions ComputeTransition(double first, double second)
        {
            if (first == second)
            {
                if (first >= 0.0)
                {
                    return Transitions.BelowToBetween;
                }

                return Transitions.None;
            }

            if (first <= -0.5 && second > -0.5)
            {
                return Transitions.BelowToBetween;
            }

            if (first < 0.5 && second >= 0.5)
            {
                return Transitions.BetweenToAbove;
            }

            if (first >= 0.5 && second < 0.5)
            {
                return Transitions.AboveToBetween;
            }

            if (first > -0.5 && second <= -0.5)
            {
                return Transitions.BetweenToBelow;
            }

            return Transitions.None;
        }

        private void SetAnnotation(AnnotatedEvent annotatedEvent, Transitions transition)
        {
            string annotation = null;
            if (transition == Transitions.BetweenToAbove)
            {
                annotation = "true";
            }
            else if (transition == Transitions.BetweenToBelow)
            {
                annotation = "false";
            }

            annotatedEvent.SetAnnotation(0, annotation, null);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a set of annotation schemas that can define an annotated event.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotatedEventDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotatedEventDefinition"/> class.
        /// </summary>
        /// <param name="name">The name of the annotation schema.</param>
        public AnnotatedEventDefinition(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.InternalSchemas = new List<AnnotationSchema>();
        }

        /// <summary>
        /// Gets or sets the name of the annotation.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets the collection of annotation schemas.
        /// </summary>
        [IgnoreDataMember]
        public ReadOnlyCollection<AnnotationSchema> Schemas => this.InternalSchemas.AsReadOnly();

        [DataMember(Name = "Schemas")]
        private List<AnnotationSchema> InternalSchemas { get; set; }

        /// <summary>
        /// Adds an annotation schema to the annotated event schemas.
        /// </summary>
        /// <param name="schema">The annotation schema to add.</param>
        public void AddSchema(AnnotationSchema schema)
        {
            this.InternalSchemas.Add(schema);
        }

        /// <summary>
        /// Creates a new annotated event using this defintion.
        /// </summary>
        /// <param name="startTime">The start time of the annotated event.</param>
        /// <param name="endTime">The end time of the annotated event.</param>
        /// <returns>A new instance of the <see cref="AnnotatedEvent"/> class.</returns>
        public AnnotatedEvent CreateAnnotatedEvent(DateTime startTime, DateTime endTime)
        {
            var annotatedEvent = new AnnotatedEvent(startTime, endTime);
            foreach (var schema in this.Schemas)
            {
                annotatedEvent.AddAnnotation(null, schema);
            }

            return annotatedEvent;
        }
    }
}
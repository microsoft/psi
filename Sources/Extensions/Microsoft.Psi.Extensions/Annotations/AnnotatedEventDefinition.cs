// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Extensions.Annotations
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Extensions.Base;

    /// <summary>
    /// Represents a set of annotation schemas that can define an annotated event.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotatedEventDefinition : ObservableObject
    {
        private string name;
        private ObservableCollection<AnnotationSchema> schemas;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotatedEventDefinition"/> class.
        /// </summary>
        /// <param name="name">The name of the annotation schema.</param>
        public AnnotatedEventDefinition(string name)
        {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.schemas = new ObservableCollection<AnnotationSchema>();
        }

        /// <summary>
        /// Gets or sets the name of the annotation.
        /// </summary>
        [DataMember]
        public string Name
        {
            get { return this.name; }
            set { this.Set(nameof(this.Name), ref this.name, value); }
        }

        /// <summary>
        /// Gets the collection of annotation schemas.
        /// </summary>
        [Browsable(false)]
        [DataMember]
        public ObservableCollection<AnnotationSchema> Schemas => this.schemas;

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
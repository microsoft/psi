// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an time-based event that has one or more annotations.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotatedEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotatedEvent"/> class.
        /// </summary>
        /// <param name="startTime">The start time of the annotated event.</param>
        /// <param name="endTime">The end time of the annotated event.</param>
        public AnnotatedEvent(DateTime startTime, DateTime endTime)
        {
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.InternalAnnotations = new List<string>();
            this.InitNew();
        }

        /// <summary>
        /// Gets the collection of annotations.
        /// </summary>
        [IgnoreDataMember]
        public ReadOnlyCollection<string> Annotations => this.InternalAnnotations.AsReadOnly();

        /// <summary>
        /// Gets the duration of the annotated event.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan Duration => this.EndTime - this.StartTime;

        /// <summary>
        /// Gets or sets the end time of the annotated event.
        /// </summary>
        [DataMember]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the start time of the annotated event.
        /// </summary>
        [DataMember]
        public DateTime StartTime { get; set; }

        [DataMember(Name = "Annotations")]
        private List<string> InternalAnnotations { get; set; }

        /// <summary>
        /// Adds a new annotation to the annotated event.
        /// </summary>
        /// <param name="value">Value of the annotation.</param>
        /// <param name="schema">Schema of the annotation.</param>
        public void AddAnnotation(string value, AnnotationSchema schema)
        {
            this.InternalAnnotations.Add(value);
        }

        /// <summary>
        /// Remove the indicated annotation from this annotated event.
        /// </summary>
        /// <param name="index">The index of the annotation to remove.</param>
        public void RemoveAnnotation(int index)
        {
            this.InternalAnnotations.RemoveAt(index);
        }

        /// <summary>
        /// Sets an annotation on the annotated event.
        /// </summary>
        /// <param name="index">The index of the annotation to set.</param>
        /// <param name="value">Value of the annotation.</param>
        /// <param name="schema">Schema of the annotation.</param>
        public void SetAnnotation(int index, string value, AnnotationSchema schema)
        {
            this.InternalAnnotations[index] = value;
        }

        /// <summary>
        /// Overridable method to allow derived object to initialize properties as part of object construction or after deserialization.
        /// </summary>
        protected virtual void InitNew()
        {
            if (this.EndTime < this.StartTime)
            {
                throw new ArgumentException("startTime must preceed endTime.", "startTime");
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.InitNew();
        }
    }
}

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
    /// Represents an time-based event that has one or more annotations.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class AnnotatedEvent : ObservableObject
    {
        private DateTime startTime;
        private DateTime endTime;
        private ObservableCollection<string> internalAnnotations;
        private ReadOnlyObservableCollection<string> annotations;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotatedEvent"/> class.
        /// </summary>
        /// <param name="startTime">The start time of the annotated event.</param>
        /// <param name="endTime">The end time of the annotated event.</param>
        public AnnotatedEvent(DateTime startTime, DateTime endTime)
        {
            this.startTime = startTime;
            this.endTime = endTime;
            this.internalAnnotations = new ObservableCollection<string>();
            this.InitNew();
        }

        /// <summary>
        /// Gets the collection of annotations.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public ReadOnlyObservableCollection<string> Annotations => this.annotations;

        /// <summary>
        /// Gets the duration of the annotated event.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan Duration => this.endTime - this.startTime;

        /// <summary>
        /// Gets or sets the end time of the annotated event.
        /// </summary>
        [DataMember]
        public DateTime EndTime
        {
            get => this.endTime;
            set
            {
                this.Set(nameof(this.EndTime), ref this.endTime, value);
                this.RaisePropertyChanged(nameof(this.Duration));
            }
        }

        /// <summary>
        /// Gets or sets the start time of the annotated event.
        /// </summary>
        [DataMember]
        public DateTime StartTime
        {
            get => this.startTime;
            set
            {
                this.Set(nameof(this.StartTime), ref this.startTime, value);
                this.RaisePropertyChanged(nameof(this.Duration));
            }
        }

        [DataMember(Name = "Annotations")]
        private ObservableCollection<string> InternalAnnotations
        {
            get { return this.internalAnnotations; }
            set { this.Set(nameof(this.Annotations), ref this.internalAnnotations, value); }
        }

        /// <summary>
        /// Adds a new annotation to the annotated event.
        /// </summary>
        /// <param name="value">Value of the annotation.</param>
        /// <param name="schema">Schema of the annotation.</param>
        public void AddAnnotation(string value, AnnotationSchema schema)
        {
            this.internalAnnotations.Add(value);
        }

        /// <summary>
        /// Remove the indicated annotation from this annotated event.
        /// </summary>
        /// <param name="index">The index of the annotation to remove.</param>
        public void RemoveAnnotation(int index)
        {
            this.internalAnnotations.RemoveAt(index);
        }

        /// <summary>
        /// Sets an annotation on the annotated event.
        /// </summary>
        /// <param name="index">The index of the annotation to set.</param>
        /// <param name="value">Value of the annotation.</param>
        /// <param name="schema">Schema of the annotation.</param>
        public void SetAnnotation(int index, string value, AnnotationSchema schema)
        {
            this.internalAnnotations[index] = value;
        }

        /// <summary>
        /// Overridable method to allow derived object to initialize properties as part of object construction or after deserialization.
        /// </summary>
        protected virtual void InitNew()
        {
            if (this.endTime < this.startTime)
            {
                throw new ArgumentException("startTime must preceed endTime.", "startTime");
            }

            this.annotations = new ReadOnlyObservableCollection<string>(this.internalAnnotations);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.InitNew();
        }
    }
}

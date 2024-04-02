// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an input event that represents the detection of a set of objects.
    /// </summary>
    public class ObjectsDetectedInputEvent : IInputEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectsDetectedInputEvent"/> class.
        /// </summary>
        /// <param name="detectedObjects">The set of detected objects.</param>
        public ObjectsDetectedInputEvent(HashSet<string> detectedObjects)
        {
            this.DetectedObjects = detectedObjects;
        }

        /// <summary>
        /// Gets the set of detected objects.
        /// </summary>
        public HashSet<string> DetectedObjects { get; }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Implements helper methods and properties for defining 3D value or interval visualization objects.
    /// </summary>
    internal static class ModelVisual3DVisualizationObjectsHelper
    {
        /// <summary>
        /// Gets the list of properties that should not appear in the property browser if
        /// the visualization object is the child of another visualization object.
        /// </summary>
        private static readonly List<string> HiddenChildProperties = new ()
        {
            nameof(VisualizationObject.CursorEpsilonNegMs),
            nameof(VisualizationObject.CursorEpsilonPosMs),
            nameof(VisualizationObject.Name),
            nameof(StreamVisualizationObject<int>.PartitionName),
            nameof(StreamVisualizationObject<int>.SourceStreamName),
            nameof(StreamVisualizationObject<int>.StreamAdapterTypeDisplayString),
            nameof(StreamVisualizationObject<int>.SummarizerTypeDisplayString),
        };

        /// <summary>
        /// Copy property descriptors from a source collection to a target collection.
        /// </summary>
        /// <param name="source">The source collection of property descriptors.</param>
        /// <param name="target">The target collection of property descriptors.</param>
        internal static void CopyPropertyDescriptors(PropertyDescriptorCollection source, PropertyDescriptorCollection target)
        {
            foreach (PropertyDescriptor propertyDescriptor in source)
            {
                // Do not add the property descriptor if it's already in the collection
                if (target.Find(propertyDescriptor.Name, false) == null)
                {
                    target.Add(propertyDescriptor);
                }
            }
        }

        /// <summary>
        /// Returns a collection of property descriptors by removing hidden child properties.
        /// </summary>
        /// <param name="propertyDescriptors">The set of property descriptors.</param>
        /// <returns>The resulting property descriptor collection.</returns>
        internal static PropertyDescriptorCollection RemoveHiddenChildProperties(PropertyDescriptorCollection propertyDescriptors)
        {
            // TypeDeescriptor.GetProperties returns a readonly collection, so we need to copy
            // the properties to a new collection, skipping those properties we wish to hide.
            var updatedPropertyDescriptors = new PropertyDescriptorCollection(null);

            foreach (PropertyDescriptor propertyDescriptor in propertyDescriptors)
            {
                if (!HiddenChildProperties.Contains(propertyDescriptor.Name))
                {
                    updatedPropertyDescriptors.Add(propertyDescriptor);
                }
            }

            return updatedPropertyDescriptors;
        }

        /// <summary>
        /// Gets the next path segment from a property path.
        /// </summary>
        /// <param name="path">The path to get the next segment from.</param>
        /// <param name="nextSegment">The next path segment.</param>
        /// <param name="pathRemainder">The remainder of the path after the next segment was removed.</param>
        internal static void GetNextPropertyPathSegment(string path, out string nextSegment, out string pathRemainder)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(nameof(path));
            }

            List<string> segments = path.Split('.').ToList();
            nextSegment = segments[0];
            segments.RemoveAt(0);
            pathRemainder = string.Join(".", segments.ToArray());
        }

        /// <summary>
        /// Copies a property value to a destination object.
        /// </summary>
        /// <param name="destination">The destination object.</param>
        /// <param name="path">The property path.</param>
        /// <param name="value">The property value.</param>
        internal static void CopyPropertyValue(object destination, string path, object value)
        {
            // Get the next segment in the path to the property
            GetNextPropertyPathSegment(path, out string nextSegment, out string pathRemainder);

            // Get the property info for the next segment in the path
            var propertyInfo = destination.GetType().GetProperty(nextSegment);

            // If we're at the end of the path, set the value of
            // the property, otherwise drill into the child object
            if (string.IsNullOrWhiteSpace(pathRemainder))
            {
                if (propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(destination, value);
                }
            }
            else
            {
                CopyPropertyValue(propertyInfo.GetValue(destination), pathRemainder, value);
            }
        }

        /// <summary>
        /// Get the child property descriptors for a prototype.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        /// <returns>The child property descriptors.</returns>
        internal static PropertyDescriptorCollection GetChildPropertyDescriptors(ICustomTypeDescriptor prototype)
        {
            // Creates a set of property descriptors for the child object
            var propertyDescriptorCollection = new PropertyDescriptorCollection(null);
            foreach (PropertyDescriptor propertyDescriptor in prototype.GetProperties())
            {
                propertyDescriptorCollection.Add(new ChildVisualizationObjectPropertyDescriptor(prototype, propertyDescriptor));
            }

            return propertyDescriptorCollection;
        }
    }
}

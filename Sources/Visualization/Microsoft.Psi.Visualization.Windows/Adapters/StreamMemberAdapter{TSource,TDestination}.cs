// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from a source stream to a member of that stream.
    /// </summary>
    /// <typeparam name="TSource">The type of messages in the source stream.</typeparam>
    /// <typeparam name="TDestination">The type of the member property or field.</typeparam>
    public class StreamMemberAdapter<TSource, TDestination> : StreamAdapter<TSource, TDestination>, IEquatable<StreamMemberAdapter<TSource, TDestination>>
    {
        // The collection of property infos and field infos that lead from the
        // source value to the member that should be returned by the adapter.
        private readonly List<MemberInfo> pathAccessors = new List<MemberInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamMemberAdapter{TSource, Destination}"/> class.
        /// </summary>
        /// <param name="path">The path to the member.</param>
        public StreamMemberAdapter(string path)
        {
            this.Initialize(path);
        }

        /// <summary>
        /// Determines whether two stream member adapters are equal.
        /// </summary>
        /// <param name="first">The first stream source to compare.</param>
        /// <param name="second">The second stream source to compare.</param>
        /// <returns>True if the stream sources are equal, otherwise false.</returns>
        public static bool operator ==(StreamMemberAdapter<TSource, TDestination> first, StreamMemberAdapter<TSource, TDestination> second)
        {
            // Check for null on left side.
            if (first is null)
            {
                if (second is null)
                {
                    // null == null = true.
                    return true;
                }

                // Only the left side is null.
                return false;
            }

            // Equals handles case of null on right side.
            return first.Equals(second);
        }

        /// <summary>
        /// Determines whether two stream member adapters are equal.
        /// </summary>
        /// <param name="first">The first stream source to compare.</param>
        /// <param name="second">The second stream source to compare.</param>
        /// <returns>True if the stream sources are equal, otherwise false.</returns>
        public static bool operator !=(StreamMemberAdapter<TSource, TDestination> first, StreamMemberAdapter<TSource, TDestination> second)
        {
            return !(first == second);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as StreamMemberAdapter<TSource, TDestination>);
        }

        /// <inheritdoc/>
        public bool Equals(StreamMemberAdapter<TSource, TDestination> other)
        {
            if (other == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.pathAccessors == other.pathAccessors;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.pathAccessors.GetHashCode();
        }

        /// <inheritdoc/>
        public override TDestination GetAdaptedValue(TSource source, Envelope envelope)
        {
            // Walk the path of accessors to get to the final value
            object targetObject = source;
            foreach (var memberInfo in this.pathAccessors)
            {
                if (targetObject != null)
                {
                    if (memberInfo is PropertyInfo propertyInfo)
                    {
                        targetObject = propertyInfo.GetValue(targetObject);
                    }
                    else if (memberInfo is FieldInfo fieldInfo)
                    {
                        targetObject = fieldInfo.GetValue(targetObject);
                    }
                    else
                    {
                        throw new Exception("Unexpected member info.");
                    }
                }
            }

            return (TDestination)targetObject;
        }

        private void Initialize(string path)
        {
            this.ValidatePathSegment(path, path);

            // Split the path up into its individual segments.
            string[] pathSegments = path.Split('.');

            // Walk the path, extracting a member info at each level to call later.
            Type type = typeof(TSource);
            foreach (string pathSegment in pathSegments)
            {
                this.ValidatePathSegment(path, pathSegment);

                PropertyInfo propertyInfo = null;
                FieldInfo fieldInfo = null;

                // Find the public property (if any) that matches the path segment.
                propertyInfo = type.GetProperty(pathSegment, BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo != null && propertyInfo.GetMethod.GetParameters().Any())
                {
                    throw new ArgumentException("StreamMemberAdapter does not support properties that take parameters.");
                }

                // If a public property was not found, then look for a public field that matches the path segment.
                if (propertyInfo == null)
                {
                    fieldInfo = type.GetField(pathSegment, BindingFlags.Public | BindingFlags.Instance);
                }

                // If we found neither a property nor a field, then the path is invalid.
                if ((propertyInfo == null) && (fieldInfo == null))
                {
                    this.ValidatePathSegment(path, null);
                }

                // Add the accessor to the list
                this.pathAccessors.Add(propertyInfo ?? fieldInfo as MemberInfo);

                // Process the next member in the path
                type = propertyInfo != null ? propertyInfo.PropertyType : fieldInfo.FieldType;
            }

            // Check that the final path accessor is of the expected type
            MemberInfo finalAccessor = this.pathAccessors.Last();
            if (finalAccessor is PropertyInfo finalPropertyInfo)
            {
                this.ValidateMemberType(path, finalPropertyInfo.PropertyType);
            }
            else if (finalAccessor is FieldInfo finalFieldInfo)
            {
                this.ValidateMemberType(path, finalFieldInfo.FieldType);
            }
        }

        private void ValidatePathSegment(string path, string pathSegment)
        {
            // Ensure the path segment is not empty
            if (string.IsNullOrWhiteSpace(pathSegment))
            {
                throw new ArgumentException($"The path {path} is not a valid path.");
            }
        }

        private void ValidateMemberType(string path, Type finalMemberType)
        {
            if (!typeof(TDestination).IsAssignableFrom(finalMemberType))
            {
                throw new ArgumentException($"The member of {typeof(TSource)} with the path {path} is of type {finalMemberType} but should be of type {typeof(TDestination)}.");
            }
        }
    }
}

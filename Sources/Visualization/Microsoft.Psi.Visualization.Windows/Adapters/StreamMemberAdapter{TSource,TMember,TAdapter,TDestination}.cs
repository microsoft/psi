// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Implements a stream adapter from a source stream to an adapted version of a member of that stream.
    /// </summary>
    /// <typeparam name="TSource">The type of messages in the source stream.</typeparam>
    /// <typeparam name="TMember">The type of the member in the stream <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="TAdapter">The type of the adapter used to adapt the type <typeparamref name="TMember"/> to the type <typeparamref name="TDestination"/>.</typeparam>
    /// <typeparam name="TDestination">The type of the destination data.</typeparam>
    public class StreamMemberAdapter<TSource, TMember, TAdapter, TDestination> : StreamAdapter<TSource, TDestination>, IEquatable<StreamMemberAdapter<TSource, TMember, TAdapter, TDestination>>
        where TAdapter : StreamAdapter<TMember, TDestination>
    {
        /// <summary>
        /// The adapter from the source data type to the member data type.
        /// </summary>
        private readonly StreamMemberAdapter<TSource, TMember> streamMemberAdapter;

        /// <summary>
        /// The adapter from the member data type to the destination data type.
        /// </summary>
        private readonly TAdapter streamMemberToDestinationAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamMemberAdapter{TSource, TMember, TAdapter, TDestination}"/> class.
        /// </summary>
        /// <param name="path">The path to the member.</param>
        public StreamMemberAdapter(string path)
        {
            // Create the source to stream member adapter
            this.streamMemberAdapter = Activator.CreateInstance(typeof(StreamMemberAdapter<TSource, TMember>), new object[] { path }) as StreamMemberAdapter<TSource, TMember>;

            // Create the stream member to destination adapter.
            this.streamMemberToDestinationAdapter = Activator.CreateInstance<TAdapter>();
        }

        /// <summary>
        /// Determines whether two stream member adapters are equal.
        /// </summary>
        /// <param name="first">The first stream source to compare.</param>
        /// <param name="second">The second stream source to compare.</param>
        /// <returns>True if the stream sources are equal, otherwise false.</returns>
        public static bool operator ==(StreamMemberAdapter<TSource, TMember, TAdapter, TDestination> first, StreamMemberAdapter<TSource, TMember, TAdapter, TDestination> second)
        {
            // Check for null on left side.
            if (object.ReferenceEquals(first, null))
            {
                if (object.ReferenceEquals(second, null))
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
        public static bool operator !=(StreamMemberAdapter<TSource, TMember, TAdapter, TDestination> first, StreamMemberAdapter<TSource, TMember, TAdapter, TDestination> second)
        {
            return !(first == second);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as StreamMemberAdapter<TSource, TMember, TAdapter, TDestination>);
        }

        /// <inheritdoc/>
        public bool Equals(StreamMemberAdapter<TSource, TMember, TAdapter, TDestination> other)
        {
            if (other == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.streamMemberAdapter == other.streamMemberAdapter
                && this.streamMemberToDestinationAdapter == other.streamMemberToDestinationAdapter;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.streamMemberAdapter.GetHashCode() ^ this.streamMemberToDestinationAdapter.GetHashCode();
        }

        /// <inheritdoc/>
        public override TDestination GetAdaptedValue(TSource source, Envelope envelope)
        {
            // Adapt from the source type to the member type.
            TMember memberValue = this.streamMemberAdapter.GetAdaptedValue(source, envelope);

            // Adapt from the member type to the destination type.
            return this.streamMemberToDestinationAdapter.GetAdaptedValue(memberValue, envelope);
        }
    }
}

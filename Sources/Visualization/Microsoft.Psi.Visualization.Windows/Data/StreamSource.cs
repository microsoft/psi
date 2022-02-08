// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Data
{
    using System;
    using System.ComponentModel;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Represents the source of a stream's data.
    /// </summary>
    public class StreamSource : ObservableObject, IEquatable<StreamSource>
    {
        private bool isLive;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamSource"/> class.
        /// </summary>
        /// <param name="partitionViewModel">The partition that is the stream's data source.</param>
        /// <param name="streamReaderType">The type of stream reader that should be used to read data from the store.</param>
        /// <param name="streamName">The name of the stream.</param>
        /// <param name="streamMetadata">The metadata for the stream.</param>
        /// <param name="streamAdapter">The stream adapter to use when reading stream data.</param>
        /// <param name="summarizer">The summarizer to use when reading the stream.</param>
        /// <param name="allocator">The allocator to use when reading data.</param>
        /// <param name="deallocator">The deallocator to use when reading data.</param>
        public StreamSource(
            PartitionViewModel partitionViewModel,
            Type streamReaderType,
            string streamName,
            IStreamMetadata streamMetadata,
            IStreamAdapter streamAdapter,
            ISummarizer summarizer,
            Func<dynamic> allocator,
            Action<dynamic> deallocator)
        {
            this.StoreName = partitionViewModel.StoreName;
            this.StorePath = partitionViewModel.StorePath;
            this.StreamReaderType = streamReaderType;
            this.StreamName = streamName;
            this.StreamMetadata = streamMetadata;
            this.StreamAdapter = streamAdapter;
            this.Summarizer = summarizer;
            this.IsLive = partitionViewModel.IsLivePartition;
            this.Allocator = allocator;
            this.Deallocator = deallocator;

            partitionViewModel.PropertyChanged += this.OnPartitionViewModelPropertyChanged;
        }

        /// <summary>
        /// Gets store name.
        /// </summary>
        public string StoreName { get; private set; }

        /// <summary>
        /// Gets store path.
        /// </summary>
        public string StorePath { get; private set; }

        /// <summary>
        /// Gets the type of stream reader that should be used to read from the store.
        /// </summary>
        public Type StreamReaderType { get; private set; }

        /// <summary>
        /// Gets stream name.
        /// </summary>
        public string StreamName { get; private set; }

        /// <summary>
        /// Gets the metadata for the underlying stream being bound to, or null.
        /// </summary>
        public IStreamMetadata StreamMetadata { get; private set; }

        /// <summary>
        /// Gets stream adapter (if any).
        /// </summary>
        public IStreamAdapter StreamAdapter { get; private set; }

        /// <summary>
        /// Gets summarizer (if any).
        /// </summary>
        public ISummarizer Summarizer { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the source is live.
        /// </summary>
        public bool IsLive
        {
            get => this.isLive;
            private set => this.Set(nameof(this.IsLive), ref this.isLive, value);
        }

        /// <summary>
        /// Gets the stream source allocator.
        /// </summary>
        public Func<dynamic> Allocator { get; }

        /// <summary>
        /// Gets the stream source deallocator.
        /// </summary>
        public Action<dynamic> Deallocator { get; }

        /// <summary>
        /// Determines whether two stream sources are equal.
        /// </summary>
        /// <param name="first">The first stream source to compare.</param>
        /// <param name="second">The second stream source to compare.</param>
        /// <returns>True if the stream sources are equal, otherwise false.</returns>
        public static bool operator ==(StreamSource first, StreamSource second)
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
        /// Determines whether two stream sources are equal.
        /// </summary>
        /// <param name="first">The first stream source to compare.</param>
        /// <param name="second">The second stream source to compare.</param>
        /// <returns>True if the stream sources are equal, otherwise false.</returns>
        public static bool operator !=(StreamSource first, StreamSource second)
        {
            return !(first == second);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as StreamSource);
        }

        /// <inheritdoc/>
        public bool Equals(StreamSource other)
        {
            if (other == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            return this.StoreName == other.StoreName
                && this.StorePath == other.StorePath
                && this.StreamName == other.StreamName
                && this.StreamAdapter == other.StreamAdapter
                && this.Summarizer == other.Summarizer;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.StoreName.GetHashCode()
                ^ this.StorePath.GetHashCode()
                ^ this.StreamName.GetHashCode()
                ^ this.StreamAdapter.GetHashCode()
                ^ this.Summarizer.GetHashCode();
        }

        /// <summary>
        /// Called when a property of the partition has changed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args for the event.</param>
        private void OnPartitionViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PartitionViewModel.IsLivePartition))
            {
                this.IsLive = (sender as PartitionViewModel).IsLivePartition;
            }
        }
    }
}

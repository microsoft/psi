// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Provides an abstract base class for stream visualization objects that show the stream value at cursor.
    /// </summary>
    /// <typeparam name="TData">The type of stream values to visualize.</typeparam>
    public abstract class StreamValueVisualizationObject<TData> : StreamVisualizationObject<TData>
    {
        /// <summary>
        /// The registration token returned when this value visualization object registered
        /// with the data manager to receive notifications when the current data has changed.
        /// </summary>
        private Guid registrationToken = Guid.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamValueVisualizationObject{TData}"/> class.
        /// </summary>
        public StreamValueVisualizationObject()
        {
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            if (e.PropertyName == nameof(this.CursorEpsilon))
            {
                // If we're bound to a stream, notify the data manager about the new value
                if (this.StreamSource != null && this.registrationToken != Guid.Empty)
                {
                    // Un-register
                    DataManager.Instance.UnregisterStreamValueSubscriber<TData>(this.registrationToken);

                    // And re-register with the new cursor epsilon
                    this.registrationToken = DataManager.Instance.RegisterStreamValueSubscriber<TData>(
                        this.StreamSource,
                        this.CursorEpsilon,
                        this.OnValueReceived,
                        this.Navigator.ViewRange.AsTimeInterval);

                    // Force the data manager to re-read the stream values at the cursor.
                    DataManager.Instance.ReadAndPublishStreamValue(this.Navigator.Cursor);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnStreamBound()
        {
            // Register the stream value visualization object with the data manager
            this.registrationToken = DataManager.Instance.RegisterStreamValueSubscriber<TData>(
                this.StreamSource,
                this.CursorEpsilon,
                this.OnValueReceived,
                this.Navigator.ViewRange.AsTimeInterval);

            base.OnStreamBound();
        }

        /// <inheritdoc />
        protected override void OnStreamUnbound()
        {
            // Unregister the stream value visualization object from the data manager
            if (this.registrationToken != Guid.Empty)
            {
                DataManager.Instance.UnregisterStreamValueSubscriber<TData>(this.registrationToken);
                this.registrationToken = Guid.Empty;
                this.SetCurrentValue(null);
            }

            base.OnStreamUnbound();
        }

        /// <summary>
        /// Called when the current value has changed.  This method is called on a worker thread.
        /// </summary>
        /// <param name="dataAvailable">Indicates whether data is available.</param>
        /// <param name="value">The new value.</param>
        /// <param name="originatingTime">The originating time for the new value.</param>
        /// <param name="creationTime">The creation time for the new value.</param>
        private void OnValueReceived(bool dataAvailable, TData value, DateTime originatingTime, DateTime creationTime)
        {
            var message = dataAvailable ?
                new Message<TData>(value, originatingTime, creationTime, 0, 0) :
                default(Message<TData>?);
            this.SetCurrentValue(message);
        }
    }
}

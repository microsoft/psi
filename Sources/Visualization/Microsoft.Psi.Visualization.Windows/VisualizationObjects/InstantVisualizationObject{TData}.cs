// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Helpers;

    /// <summary>
    /// Represents an instant visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the instant visualization.</typeparam>
    public abstract class InstantVisualizationObject<TData> : StreamVisualizationObject<TData>
    {
        /// <summary>
        /// The magnitude of the cursor epsilon (this value can be modified in the properties window).
        /// </summary>
        private int cursorEpsilonMs;

        /// <summary>
        /// Gets or sets the epsilon around the cursor for which we show the instant visualization.
        /// </summary>
        private RelativeTimeInterval cursorEpsilon;

        /// <summary>
        /// The registration token returned when this instant visualization object registered
        /// with the data manager to receive notifications when the current data has changed.
        /// </summary>
        private Guid registrationToken = Guid.Empty;

        /// <summary>
        /// The last instant data changed task queued to the dispatcher.
        /// </summary>
        private DispatcherOperation lastDataChangedTask = null;

        /// <summary>
        /// The last instant data changed task queued to the dispatcher.
        /// </summary>
        private TData lastDataChangedTaskData;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantVisualizationObject{TData}"/> class.
        /// </summary>
        public InstantVisualizationObject()
        {
            this.CursorEpsilonMs = 500;
        }

        /// <summary>
        /// Gets or sets the radius of the cursor epsilon. (This value is exposed in the Properties UI).
        /// </summary>
        [DataMember]
        public int CursorEpsilonMs
        {
            get { return this.cursorEpsilonMs; }

            set
            {
                this.cursorEpsilonMs = value;
                this.CursorEpsilon = new RelativeTimeInterval(-TimeSpan.FromMilliseconds(this.cursorEpsilonMs), TimeSpan.FromMilliseconds(this.cursorEpsilonMs));
            }
        }

        /// <summary>
        /// Gets the cursor epsilon.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelativeTimeInterval CursorEpsilon
        {
            get => this.cursorEpsilon;
            private set
            {
                // Update the value
                this.Set(nameof(this.CursorEpsilon), ref this.cursorEpsilon, value);

                // If we're bound to a stream, notify the data manager about the new value
                if (this.StreamSource != null)
                {
                    this.UpdateCursorEpsilonInDataManager();
                }
            }
        }

        /// <inheritdoc/>
        public override DateTime? GetSnappedTime(DateTime time, SnappingBehavior snappingBehavior)
        {
            return DataManager.Instance.GetOriginatingTimeOfNearestInstantMessage(this.StreamSource, time);
        }

        /// <summary>
        /// Called when the current instant data has changed.  This method is called on a worker thread.
        /// </summary>
        /// <param name="data">The new instant data.</param>
        /// <param name="streamCacheEntry">The index entry that contained the data.</param>
        public void OnInstantDataChanged(object data, StreamCacheEntry streamCacheEntry)
        {
            if (Application.Current != null)
            {
                // If the last update task is still pending, remove it from the dispatcher queue since we have newer data.
                if ((this.lastDataChangedTask != null) && (this.lastDataChangedTask.Status == DispatcherOperationStatus.Pending))
                {
                    // Abort the task
                    if (this.lastDataChangedTask.Abort())
                    {
                        // If the data that was to be used as the current data is shared, then decrement its reference count
                        if (this.IsShared && (this.lastDataChangedTaskData != null))
                        {
                                (this.lastDataChangedTaskData as IDisposable).Dispose();
                        }
                    }
                }

                // Because this method is called on a worker thread and some of our Model3DVisual visualization objects directly touch UI elements
                // when we call SetCurrentValue, we need to invoke the dispatcher, and we do so asynchronously for performance reasons.  If data is
                // a shared object then the code that called this method will release its reference to the shared data object shortly after this
                // method returns.  Consequently we need to add a new reference to the shared data object now instead of the usual pattern where
                // the code in SetCurrentValue adds the new reference.  We therefore pass false as the incrementSharedRefCount parameter to indicate
                // that we've already incremented the reference count and the code in SetCurrentValue should not.  We also squirrel away a reference
                // to data in case we get called again before the dispatcher has had a chance to execute our task.  If this happens, the currently
                // pending dispatcher task is obsolete so we'll cancel it.  If we cancel the task and the data for the task was a shared object then
                // we'll need to dereference the data ourselves to ensure it gets released properly, see code above.
                Message<TData>? newValue =
                    streamCacheEntry != null ?
                    new Message<TData>(
                        (this.IsShared && data != null) ? ((TData)data).DeepClone() : (TData)data,
                        streamCacheEntry.OriginatingTime,
                        streamCacheEntry.CreationTime,
                        0,
                        0) :
                    (Message<TData>?)null;
                this.lastDataChangedTaskData = newValue.HasValue ? newValue.Value.Data : default;
                this.lastDataChangedTask = Application.Current.Dispatcher.BeginInvoke((Action)(() => this.SetCurrentValue(newValue, false)), DispatcherPriority.Render);
            }
        }

        /// <inheritdoc />
        protected override void OnStreamBound()
        {
            // Register the instant visualization object with the data manager
            this.registrationToken = DataManager.Instance.RegisterInstantDataTarget<TData>(this.StreamSource, this.CursorEpsilon, this.OnInstantDataChanged, this.Navigator.ViewRange.AsTimeInterval);

            base.OnStreamBound();
        }

        /// <inheritdoc />
        protected override void OnStreamUnbound()
        {
            // Unregister the instant visualization object from the data manager
            if (this.registrationToken != Guid.Empty)
            {
                DataManager.Instance.UnregisterInstantDataTarget(this.registrationToken);
                this.registrationToken = Guid.Empty;
            }

            base.OnStreamUnbound();
        }

        private void UpdateCursorEpsilonInDataManager()
        {
            if (this.registrationToken != Guid.Empty)
            {
                // Tell the data manager about the new cursor epsilon
                DataManager.Instance.UpdateInstantDataTargetEpsilon(this.registrationToken, this.CursorEpsilon);

                // Force the data manager to re-push data to all instant visualization objects
                DataManager.Instance.ReadInstantData(this.Navigator.Cursor);
            }
        }
    }
}

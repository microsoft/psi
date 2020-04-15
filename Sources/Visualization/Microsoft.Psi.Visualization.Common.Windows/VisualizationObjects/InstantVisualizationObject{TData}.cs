// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Data;

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
                if ((this.StreamBinding != null) && this.StreamBinding.IsBound)
                {
                    this.UpdateCursorEpsilonInDataManager();
                }
            }
        }

        /// <summary>
        /// Called when the current instant data has changed.  This method is called on a worker thread.
        /// </summary>
        /// <param name="data">The new instant data.</param>
        /// <param name="indexEntry">The index entry that contained the data.</param>
        public void OnInstantDataChanged(object data, IndexEntry indexEntry)
        {
            if (Application.Current != null)
            {
                // If the last update task is still pending, remove it from the dispatcher queue since we have newer data.
                if ((this.lastDataChangedTask != null) && (this.lastDataChangedTask.Status == DispatcherOperationStatus.Pending))
                {
                    // Abort the task
                    if (this.lastDataChangedTask.Abort())
                    {
                        // If the data is shared, release the reference
                        if (this.IsShared && (this.lastDataChangedTaskData != null))
                        {
                            (this.lastDataChangedTaskData as IDisposable).Dispose();
                        }
                    }
                }

                // Squirrel away the data object in case we need to abort the task before it gets scheduled
                this.lastDataChangedTaskData = (TData)data;

                // Queue up the task on the UI thread
                this.lastDataChangedTask = Application.Current.Dispatcher.BeginInvoke(
                    (Action)(() =>
                    {
                        // Construct a message for the current value
                        this.CurrentValue = new Message<TData>((TData)data, indexEntry.OriginatingTime, indexEntry.Time, 0, 0);

                        // If the data is shared, release the reference
                        if (this.IsShared && (data != null))
                        {
                            (data as IDisposable).Dispose();
                        }
                    }), DispatcherPriority.Render);
            }
        }

        /// <inheritdoc />
        protected override void OnStreamBound()
        {
            // Register the instant visualization object with the data manager
            this.registrationToken = DataManager.Instance.RegisterInstantDataTarget<TData>(this.StreamBinding, this.CursorEpsilon, this.OnInstantDataChanged, this.Navigator.ViewRange.AsTimeInterval);

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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Persistence;
    using Microsoft.Psi.Visualization.Collections;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Navigation;

    /// <summary>
    /// Represents an instant visualization object.
    /// </summary>
    /// <typeparam name="TData">The type of the instant visualization.</typeparam>
    /// <typeparam name="TConfig">The type of the instant visualization object configuration.</typeparam>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public abstract class InstantVisualizationObject<TData, TConfig> : StreamVisualizationObject<TData, TConfig>
        where TConfig : InstantVisualizationObjectConfiguration, new()
    {
        /// <summary>
        /// The indices read from the stream.
        /// </summary>
        private ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView indices;

        /// <summary>
        /// Gets or sets the indicies.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView Indices
        {
            get => this.indices;
            protected set
            {
                if (this.indices != value)
                {
                    var oldValue = this.indices;
                    this.Set(nameof(this.Indices), ref this.indices, value);
                    this.OnIndiciesChanged(oldValue, this.indices);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnCursorModeChanged(object sender, CursorModeChangedEventArgs cursorModeChangedEventArgs)
        {
            // If we changed from or to live mode, and we're currently bound to a datasource, then reopen the stream in the correct mode
            if (this.IsBound && cursorModeChangedEventArgs.OriginalValue != cursorModeChangedEventArgs.NewValue)
            {
                if ((cursorModeChangedEventArgs.OriginalValue == CursorMode.Live) || (cursorModeChangedEventArgs.NewValue == CursorMode.Live))
                {
                    this.RefreshData();
                }
            }

            base.OnCursorModeChanged(sender, cursorModeChangedEventArgs);
        }

        /// <summary>
        /// Invoked when the <see cref="InstantVisualizationObject{TData, TConfig}.Indices"/> property changes.
        /// </summary>
        /// <param name="oldValue">The old indicies value.</param>
        /// <param name="newValue">The new indicies value.</param>
        protected virtual void OnIndiciesChanged(ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView oldValue, ObservableKeyedCache<DateTime, IndexEntry>.ObservableKeyedView newValue)
        {
            if (oldValue != null)
            {
                oldValue.DetailedCollectionChanged -= this.Indicies_CollectionChanged;
            }

            if (newValue != null)
            {
                newValue.DetailedCollectionChanged += this.Indicies_CollectionChanged;
            }

            this.OnIndiciesCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Invoked when the <see cref="InstantVisualizationObject{TData, TConfig}.Indices"/> collection changes.
        /// </summary>
        /// <param name="e">Data for the event.</param>
        protected virtual void OnIndiciesCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // see if we are still active
            if (this.Container == null)
            {
                return;
            }

            // No indices - use default value
            if (this.Indices == null)
            {
                this.CurrentValue = null;
            }
            else
            {
                // Use index to find current value
                var currentTime = this.Navigator.Cursor;
                if (currentTime >= this.Indices.FirstOrDefault().OriginatingTime && currentTime <= this.Indices.LastOrDefault().OriginatingTime)
                {
                    // we got a new range of indices which covers our desired time
                    this.SetCurrentValue(currentTime);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnStreamBound()
        {
            base.OnStreamBound();
            this.RefreshData();
        }

        /// <inheritdoc />
        protected override void OnStreamUnbound()
        {
            this.Indices = null;
            base.OnStreamUnbound();
        }

        /// <inheritdoc />
        protected override void SetCurrentValue(DateTime currentTime)
        {
            // Check that we're actually bound to a store
            if (this.Configuration.StreamBinding.IsBound)
            {
                if (this.Navigator.CursorMode == CursorMode.Live)
                {
                    TimeInterval interval;
                    if (this.Navigator.Cursor == DateTime.MinValue)
                    {
                        interval = new TimeInterval(DateTime.MinValue, DateTime.MinValue + TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        interval = this.Navigator.Cursor + this.CursorEpsilon;
                    }

                    if (this.CurrentValue.HasValue && !interval.PointIsWithin(this.CurrentValue.Value.OriginatingTime))
                    {
                        this.CurrentValue = null;
                    }
                }
                else
                {
                    int index = this.GetIndexForTime(currentTime, this.Indices?.Count ?? 0, (idx) => this.Indices[idx].OriginatingTime);
                    if (index != -1)
                    {
                        var indexEntry = this.Indices[index];
                        TData data = DataManager.Instance.Read<TData>(this.Configuration.StreamBinding, indexEntry);
                        this.CurrentValue = new Message<TData>(data, indexEntry.OriginatingTime, indexEntry.Time, 0, 0);
                        if (data is IDisposable)
                        {
                            (data as IDisposable).Dispose();
                        }
                    }
                    else
                    {
                        base.SetCurrentValue(currentTime);
                    }
                }
            }
        }

        private void RefreshData()
        {
            if (this.Navigator.CursorMode == CursorMode.Live)
            {
                this.Data = DataManager.Instance.ReadStream<TData>(this.Configuration.StreamBinding, 1);
            }
            else
            {
                this.Indices = DataManager.Instance.ReadIndex<TData>(this.Configuration.StreamBinding, this.Navigator.DataRange.StartTime, this.Navigator.DataRange.EndTime);
            }
        }

        private void Indicies_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnIndiciesCollectionChanged(e);
        }
    }
}

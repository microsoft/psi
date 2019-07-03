// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Threading;

    /// <summary>
    /// Represents a dynamic data cache whose keys are embedded in the values and provides
    /// notifications when items get added, removed, or when the whole list is refreshed.
    ///
    /// The cache supports views onto the data. These views support changes to the underlying data
    /// and being updated by the underlying data.
    ///
    /// In future versions the underlying data will be to be released if there is no view present
    /// for a given data range.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TItem">The type of items in the cache.</typeparam>
    public class ObservableKeyedCache<TKey, TItem> : ObservableSortedCollection<TItem>
    {
        /// <summary>
        /// The implementation of the <see cref="IComparer{T}"/> generic interface to use when comparing keys.
        /// </summary>
        private readonly IComparer<TKey> keyComparer;

        /// <summary>
        /// Function to extract key from item.
        /// </summary>
        private readonly Func<TItem, TKey> getKeyForItem;

        /// <summary>
        /// Number of collection changed events recieved before pruning the cache.
        /// </summary>
        private uint pruneThreshold = ObservableSortedCollection<TItem>.DefaultCapacity;

        /// <summary>
        /// Flag indicating the cache needs to be pruned.
        /// </summary>
        private bool needsPruning = false;

        /// <summary>
        /// Number of collection changed events recieved, since last pruning of the cache.
        /// </summary>
        private uint collectionChangedCount = 0;

        /// <summary>
        /// Dictionary of weakly held views indexed by start and end keys.
        /// </summary>
        private Dictionary<Tuple<TKey, TKey, uint, Func<TKey, TKey>>, WeakReference<ObservableKeyedView>> views;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableKeyedCache{TKey, TItem}" /> class that uses the default comparers.
        /// </summary>
        /// <param name="getKeyForItem">Funtion that returns a key given an item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="getKeyForItem"/> is null.</exception>
        public ObservableKeyedCache(Func<TItem, TKey> getKeyForItem)
            : this(null, null, getKeyForItem)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableKeyedCache{TKey, TItem}" /> class that uses the specified comparer.
        /// </summary>
        /// <param name="keyComparer">
        /// The implementation of the <see cref="IComparer{TKey}"/> interface to use when comparing keys, or null to use
        /// the default comparer for the type of the key, obtained from <see cref="Comparer{TKey}.Default"/>.
        /// </param>
        /// <param name="itemComparer">
        /// The implementation of the <see cref="IComparer{TItem}"/> interface to use when comparing items, or null to use
        /// the default comparer for the type of the item, obtained from <see cref="Comparer{TItem}.Default"/>.
        /// </param>
        /// <param name="getKeyForItem">Function that returns a key given an item.</param>
        /// <exception cref="ArgumentNullException"><paramref name="getKeyForItem"/> is null.</exception>
        public ObservableKeyedCache(IComparer<TKey> keyComparer, IComparer<TItem> itemComparer, Func<TItem, TKey> getKeyForItem)
            : base(itemComparer, null)
        {
            this.keyComparer = keyComparer ?? Comparer<TKey>.Default;
            this.getKeyForItem = getKeyForItem;
            this.views = new Dictionary<Tuple<TKey, TKey, uint, Func<TKey, TKey>>, WeakReference<ObservableKeyedView>>();
        }

        /// <summary>
        /// Gets a list of extents (start and end keys) of views (both live as well as weak views whose items have not yet been purged).
        /// </summary>
        public IList<Tuple<TKey, TKey, uint, Func<TKey, TKey>>> ViewExtents => this.views.Keys.ToList();

        /// <summary>
        /// Gets a dynamic view of the underlying cache based on the parameters given.
        /// </summary>
        /// <param name="mode">View mode.</param>
        /// <param name="startKey">Start key of view.</param>
        /// <param name="endKey">End key of view.</param>
        /// <param name="tailCount">Number of items to include in view.</param>
        /// <param name="tailRange">Tail duration function. Takes last item's key and returns a new startKey.</param>
        /// <returns>An instance of <see cref="ObservableKeyedView"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="startKey"/> must be less than or equal to <paramref name="endKey"/>.</exception>
        public ObservableKeyedView GetView(ObservableKeyedView.ViewMode mode, TKey startKey, TKey endKey, uint tailCount, Func<TKey, TKey> tailRange)
        {
            if (this.keyComparer.Compare(startKey, endKey) > 0)
            {
                throw new ArgumentException($"startKey ({startKey}) must be less than or equal to endKey ({endKey}).");
            }

            var viewKey = Tuple.Create(startKey, endKey, tailCount, tailRange);
            WeakReference<ObservableKeyedView> weakView = null;
            ObservableKeyedView view = null;
            if (this.views.TryGetValue(viewKey, out weakView))
            {
                if (weakView.TryGetTarget(out view))
                {
                    return view;
                }
                else
                {
                    view = new ObservableKeyedView(this, mode, startKey, endKey, tailCount, tailRange);
                    weakView.SetTarget(view);

                    // Sometimes the weak view gets deleted between when we grab it to check
                    // if it has a hard reference and when we actually set the new hard reference
                    // that we just created.  If that happens, then the following code makes sure
                    // the weak view gets put back into the collection.
                    this.views[viewKey] = weakView;
                }
            }
            else
            {
                view = new ObservableKeyedView(this, mode, startKey, endKey, tailCount, tailRange);
                weakView = new WeakReference<ObservableKeyedView>(view);
                this.views.Add(viewKey, weakView);
            }

            return view;
        }

        /// <summary>
        /// Gets a value associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">When this method returns, contains the first value associated with the specified key, if the key is
        /// found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if a value with the specified key was found; otherwise, false.</returns>
        public bool TryGetValue(TKey key, out TItem value)
        {
            int index = this.FindIndex(key, true);
            if (index < this.Count)
            {
                TItem item = this[index];
                if (this.keyComparer.Compare(this.getKeyForItem(item), key) == 0)
                {
                    value = item;
                    return true;
                }
            }

            value = default(TItem);
            return false;
        }

        /// <summary>
        /// Cleanup the cache by removing dead views and pruning the underlying data.
        /// </summary>
        public void PruneDeadViews()
        {
            this.needsPruning = true;
            this.PruneCache();
        }

        /// <inheritdoc />
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);

            // prune collection if number of events received exceeds threshold and size of collection has grown from initial capacity
            if (this.collectionChangedCount++ > this.pruneThreshold && this.Count > ObservableSortedCollection<TKey>.DefaultCapacity)
            {
                this.collectionChangedCount = 0;
                this.needsPruning = true;
                Application.Current.Dispatcher.InvokeAsync(this.PruneCache, DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// Walks views and prunes out dead (GC'd) views and the resulting unreferenced data.
        /// </summary>
        private void PruneCache()
        {
            // More than one call to PruneCache can be queued. Only process the first one.
            if (!this.needsPruning)
            {
                return;
            }

            this.needsPruning = false;

            // track whether we did any pruning to adjust our threshold
            bool didPrune = false;

            // find dead views
            ObservableKeyedView view = null;
            var deadViews = this.views.Where(v => !v.Value.TryGetTarget(out view));

            // remove dead views from views collection
            foreach (var deadView in deadViews.ToList())
            {
                this.views.Remove(deadView.Key);
            }

            // remove cached items that are no longer referenced by a view
            var liveViews = this.views.OrderBy(lv => lv.Key.Item1, this.keyComparer).ThenBy(lv => lv.Key.Item2, this.keyComparer);
            int startIndex = 0;
            foreach (var liveView in liveViews)
            {
                // find exclusive endIndex of range to be removed
                int endIndex = startIndex;
                while (this.Count > endIndex && this.keyComparer.Compare(this.getKeyForItem(this[endIndex]), liveView.Key.Item1) < 0)
                {
                    endIndex++;
                }

                // remove items preceding current live view
                if (endIndex > startIndex)
                {
                    this.RemoveRange(startIndex, endIndex - startIndex);
                    didPrune = true;
                }

                // advance startIndex to end of current live view
                while (this.Count > startIndex && this.keyComparer.Compare(this.getKeyForItem(this[startIndex]), liveView.Key.Item2) < 0)
                {
                    startIndex++;
                }
            }

            // remove items after last live view
            if (startIndex == 0)
            {
                // no live view that contains any data - clear all
                this.Clear();
                didPrune = true;
            }
            else
            {
                // at least one live view that contains data - remove to the end
                if (this.Count > startIndex)
                {
                    this.RemoveRange(startIndex, this.Count - startIndex);
                    didPrune = true;
                }
            }

            // adjust pruning threshold
            this.pruneThreshold = didPrune ?
                Math.Max(this.pruneThreshold >> 1, ObservableSortedCollection<TItem>.DefaultCapacity >> 2) :
                Math.Min(this.pruneThreshold << 1, ObservableSortedCollection<TItem>.DefaultCapacity << 2);
        }

        private int FindIndex(TKey key, bool end)
        {
            int lo = 0;
            int hi = this.Count - 1;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) / 2);
                int res = res = this.keyComparer.Compare(key, this.getKeyForItem(this[mid]));
                if (res < 0)
                {
                    hi = mid - 1;
                }
                else if (res > 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    // found and exact match, now move to the first (or last) exact match
                    if (end)
                    {
                        // if minimize, work backwards
                        while (mid > 0)
                        {
                            res = this.keyComparer.Compare(key, this.getKeyForItem(this[--mid]));
                            if (res != 0)
                            {
                                mid++;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // if !minimize, work forwards
                        while (mid < this.Count - 1)
                        {
                            res = this.keyComparer.Compare(key, this.getKeyForItem(this[++mid]));
                            if (res != 0)
                            {
                                mid--;
                                break;
                            }
                        }
                    }

                    return mid;
                }
            }

            return lo;
        }

        /// <summary>
        /// Represents a dynamic data collection whose keys are embedded in the values and provides
        /// notifications when items get added, removed, or when the whole list is refreshed.
        /// </summary>
        public class ObservableKeyedView : IList<TItem>, IReadOnlyList<TItem>, INotifyCollectionChanged, INotifyPropertyChanged
        {
            /// <summary>
            /// Indexer name.
            /// </summary>
            private const string IndexerName = "Item[]";

            /// <summary>
            /// Underlying cache.
            /// </summary>
            private ObservableKeyedCache<TKey, TItem> cache;

            /// <summary>
            /// End index of the view.
            /// </summary>
            private int endIndex;

            /// <summary>
            /// End key of the view.
            /// </summary>
            private TKey endKey;

            /// <summary>
            /// Start index of the view.
            /// </summary>
            private int startIndex;

            /// <summary>
            /// Start key of the view.
            /// </summary>
            private TKey startKey;

            /// <summary>
            /// Mode of the view.
            /// </summary>
            private ViewMode mode;

            /// <summary>
            /// Tail count.
            /// </summary>
            private uint tailCount;

            /// <summary>
            /// Tail duration function. Takes last item's key and returns a new startKey.
            /// </summary>
            private Func<TKey, TKey> tailRange;

            /// <summary>
            /// Initializes a new instance of the <see cref="ObservableKeyedView"/> class.
            /// </summary>
            /// <param name="cache">Underlying cache.</param>
            /// <param name="mode">View mode.</param>
            /// <param name="startKey">Start key of the view.</param>
            /// <param name="endKey">End key of the view.</param>
            /// <param name="tailCount">Number of items to include in view.</param>
            /// <param name="tailRange">Tail duration function. Takes last item's key and returns a new startKey.</param>
            internal ObservableKeyedView(ObservableKeyedCache<TKey, TItem> cache, ViewMode mode, TKey startKey, TKey endKey, uint tailCount, Func<TKey, TKey> tailRange)
            {
                this.cache = cache;
                this.mode = mode;
                this.startKey = startKey;
                this.endKey = endKey;
                this.tailCount = tailCount;
                this.tailRange = tailRange;

                // Subscribing to cache's DetailedCollectionChanged via WeakEventManager ensures that a strong
                // reference will not be held to the view, allowing it to be collected when no longer referenced.
                WeakEventManager<ObservableKeyedCache<TKey, TItem>, NotifyCollectionChangedEventArgs>.AddHandler(
                    this.cache,
                    nameof(this.cache.DetailedCollectionChanged),
                    this.OnCacheCollectionChanged);

                // update start and end keys
                this.UpdateKeys();

                // update start and end indexes
                this.UpdateIndexes();
            }

            /// <summary>
            /// Occurs when an item is added, removed, changed, moved, or the entire list is refreshed.
            /// This event does not fire range based events for compatability to wpf controls. It turns adds into resets.
            /// </summary>
            public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

            /// <summary>
            /// Occurs when an item is added, removed, changed, moved, or the entire list is refreshed.
            /// This event does fire range based events for efficiency.
            /// </summary>
            public virtual event NotifyCollectionChangedEventHandler DetailedCollectionChanged;

            /// <inheritdoc/>
            public virtual event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Modes the <see cref="ObservableKeyedView"/> operates under.
            /// </summary>
            public enum ViewMode
            {
                /// <summary>
                /// Fixed view mode (default). View will stay fixed on start and end keys.
                /// </summary>
                Fixed,

                /// <summary>
                /// TailCount view mode. View will slide based on number of entries window.
                /// </summary>
                TailCount,

                /// <summary>
                /// TailRange view mode. View will slide based on time window.
                /// </summary>
                TailRange,
            }

            /// <inheritdoc/>
            public int Count => this.endIndex - this.startIndex;

            /// <inheritdoc/>
            public bool IsReadOnly => false;

            /// <inheritdoc/>
            public TItem this[int index]
            {
                get
                {
                    if (index >= this.Count)
                    {
                        throw new ArgumentOutOfRangeException("index");
                    }

                    return this.cache[this.startIndex + index];
                }

                set
                {
                    throw new NotSupportedException();
                }
            }

            /// <inheritdoc/>
            public void Add(TItem item)
            {
                // add to cache - let cache signal and view will pass through to listeners
                var key = this.cache.getKeyForItem(item);
                if (this.cache.keyComparer.Compare(this.startKey, key) <= 0 && this.cache.keyComparer.Compare(key, this.endKey) < 0)
                {
                    this.cache.Add(item);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("item", "Adds can only be performed within the start and end key values.");
                }
            }

            /// <summary>
            /// Adds the elements of the specified collection to the end of the <see cref="ObservableKeyedView"/>.
            /// </summary>
            /// <param name="collection">The collection whose elements should be added to the end of the <see cref="ObservableKeyedView"/>.
            /// The collection itself cannot be null, but it can contain elements that are null, if type <typeparamref name="TItem"/> is a reference type.</param>
            /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
            public void AddRange(IEnumerable<TItem> collection)
            {
                // add to cache - let cache signal and view will pass through to listeners
                this.cache.AddRange(collection);
            }

            /// <inheritdoc/>
            public void Clear()
            {
                // remove those in this view
                while (this.startIndex < this.endIndex)
                {
                    this.cache.RemoveAt(this.endIndex - 1);
                }
            }

            /// <inheritdoc/>
            public bool Contains(TItem item)
            {
                var index = this.cache.IndexOf(item);
                return index >= this.startIndex && index <= this.endIndex;
            }

            /// <inheritdoc/>
            public void CopyTo(TItem[] array, int arrayIndex)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }

                if (arrayIndex < 0 || arrayIndex > array.Length)
                {
                    throw new ArgumentOutOfRangeException("arrayIndex", "Array index must a non-netative number.");
                }

                if (array.Length - arrayIndex < this.Count)
                {
                    throw new ArgumentException("Target array is too small with given index.");
                }

                for (int i = 0; i < this.Count; i++)
                {
                    array[arrayIndex + i] = this[i];
                }
            }

            /// <inheritdoc/>
            public IEnumerator<TItem> GetEnumerator()
            {
                return new Enumerator(this);
            }

            /// <inheritdoc/>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(this);
            }

            /// <inheritdoc/>
            public int IndexOf(TItem item)
            {
                var index = this.cache.IndexOf(item);
                if (index >= this.startIndex && index < this.endIndex)
                {
                    return index - this.startIndex;
                }
                else
                {
                    return -1;
                }
            }

            /// <inheritdoc/>
            public void Insert(int index, TItem item)
            {
                if (index < 0 || index > this.Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                // trace if we not appending
                Debug.WriteLineIf(index != this.Count, $"ObservableKeyedView.Insert - index({index}) != this.Count({this.Count})");
                this.cache.Add(item);
            }

            /// <inheritdoc/>
            public bool Remove(TItem item)
            {
                var index = this.IndexOf(item);
                if (index < 0)
                {
                    return false;
                }

                this.RemoveAt(index);
                return true;
            }

            /// <inheritdoc/>
            public void RemoveAt(int index)
            {
                if (index < this.startIndex || index >= this.endIndex )
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                this.cache.RemoveAt(this.startIndex + index);
            }

            /// <summary>
            /// Raises the <see cref="CollectionChanged" /> event with the provided arguments.
            /// </summary>
            /// <param name="e">Arguments of the event being raised.</param>
            protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
            {
                // convert adds of multiple items to a reset
                this.CollectionChanged?.Invoke(
                    this,
                    e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 1 ? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset) : e);
                this.DetailedCollectionChanged?.Invoke(this, e);
            }

            /// <summary>
            /// Raises the <see cref="PropertyChanged" /> event with the provided arguments.
            /// </summary>
            /// <param name="e">Arguments of the event being raised.</param>
            protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                this.PropertyChanged?.Invoke(this, e);
            }

            private void OnCacheAdd(IList items, int startingIndex)
            {
                if (items.Count > 0)
                {
                    TKey start = this.cache.getKeyForItem((TItem)items[0]);
                    TKey end = this.cache.getKeyForItem((TItem)items[items.Count - 1]);

                    // update start and end indexes
                    this.UpdateIndexes();

                    // in our view
                    if (this.cache.keyComparer.Compare(end, this.startKey) >= 0 && this.cache.keyComparer.Compare(start, this.endKey) < 0)
                    {
                        // find the adjusted ends
                        start = this.cache.keyComparer.Compare(this.startKey, start) < 0 ? start : this.startKey;
                        end = this.cache.keyComparer.Compare(this.endKey, end) > 0 ? end : this.endKey;

                        // find intersecting items
                        var intersectingItems = items.Cast<TItem>().Where(
                            (item) =>
                            {
                                return
                                    this.cache.keyComparer.Compare(this.startKey, this.cache.getKeyForItem(item)) <= 0 &&
                                    this.cache.keyComparer.Compare(this.cache.getKeyForItem(item), this.endKey) < 0;
                            });

                        if (intersectingItems.Count() == 0)
                        {
                            // no intersecting items - ignore.
                            return;
                        }

                        // notify subscribers
                        this.OnPropertyChanged(nameof(this.Count));
                        this.OnPropertyChanged(IndexerName);
                        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, intersectingItems.ToList(), this.IndexOf(intersectingItems.First())));
                    }
                }
            }

            private void OnCacheRemove(IList items, int startingIndex)
            {
                if ((startingIndex + items.Count) > this.startIndex && startingIndex < this.endIndex)
                {
                    // Fixed and in our view
                    int startIndex = Math.Max(this.startIndex, startingIndex);
                    int endIndex = Math.Min(this.endIndex, startingIndex + items.Count);
                    int viewStartIndex = startIndex - this.startIndex;

                    ArrayList removedItems = new ArrayList();
                    for (int i = startIndex - startingIndex; i < endIndex - startingIndex; i++)
                    {
                        removedItems.Add(items[i]);
                    }

                    // update start and end indexes
                    this.UpdateIndexes();

                    // notify subscribers
                    this.OnPropertyChanged(nameof(this.Count));
                    this.OnPropertyChanged(IndexerName);
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, viewStartIndex));
                }
                else
                {
                    // update start and end indexes
                    this.UpdateIndexes();
                }
            }

            private void OnCacheReplace(IList newItems, IList oldItems, int startingIndex)
            {
                if (newItems.Count != oldItems.Count)
                {
                    throw new NotSupportedException("Replacing a range of items with a different number of items is not yet supported.");
                }

                // in our view
                if ((startingIndex + oldItems.Count) > this.startIndex && startingIndex < this.endIndex)
                {
                    int startIndex = Math.Max(this.startIndex, startingIndex);
                    int endIndex = Math.Min(this.endIndex, startingIndex + oldItems.Count);
                    int viewStartIndex = startIndex - this.startIndex;

                    ArrayList replacedItems = new ArrayList();
                    ArrayList replacementItems = new ArrayList();
                    for (int i = startIndex - startingIndex; i < endIndex - startingIndex; i++)
                    {
                        replacedItems.Add(oldItems[i]);
                        replacementItems.Add(newItems[i]);
                    }

                    // notify subscribers
                    this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, replacementItems, replacedItems, viewStartIndex));
                }
            }

            private void OnCacheReset()
            {
                // update start and end indexes
                this.UpdateIndexes();

                // notify subscribers
                this.OnPropertyChanged(nameof(this.Count));
                this.OnPropertyChanged(IndexerName);
                this.OnCollectionReset();
            }

            private void OnCacheCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                this.UpdateKeys();

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        this.OnCacheAdd(e.NewItems, e.NewStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        this.OnCacheRemove(e.OldItems, e.OldStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        this.OnCacheReplace(e.NewItems, e.OldItems, e.NewStartingIndex);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        this.OnCacheReset();
                        break;

                    default:
                        throw new NotSupportedException("ObservalbeKeyedView received a CollectionChanged notification that it does not support.");
                }
            }

            private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
            {
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
            }

            private void OnCollectionReset()
            {
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }

            private void OnPropertyChanged(string propertyName)
            {
                this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
            }

            private void UpdateIndexes()
            {
                this.startIndex = this.cache.FindIndex(this.startKey, true);
                this.endIndex = this.cache.FindIndex(this.endKey, false);
            }

            private void UpdateKeys()
            {
                if (this.mode == ViewMode.Fixed)
                {
                    return;
                }

                if (this.cache.Count > 0)
                {
                    // Update the keys
                    var oldStartKey = this.startKey;
                    if (this.mode == ViewMode.TailCount)
                    {
                        var count = Math.Min(this.cache.Count, (int)this.tailCount);
                        this.startKey = this.cache.getKeyForItem(this.cache[this.cache.Count - count]);
                    }
                    else if (this.mode == ViewMode.TailRange)
                    {
                        var lastKey = this.cache.getKeyForItem(this.cache[this.cache.Count - 1]);
                        this.startKey = this.tailRange(lastKey);
                    }

                    // The cache views needs to be updated, every time the startKey changes
                    if (this.cache.keyComparer.Compare(oldStartKey, this.startKey) != 0)
                    {
                        var viewKey = Tuple.Create(oldStartKey, this.endKey, this.tailCount, this.tailRange);
                        if (this.cache.views.ContainsKey(viewKey))
                        {
                            var view = this.cache.views[viewKey];
                            this.cache.views.Remove(viewKey);

                            // If the view is no longer live, do not add it back
                            ObservableKeyedView reference;
                            if (view.TryGetTarget(out reference))
                            {
                                // Add the view back to the cache views with the new key
                                viewKey = Tuple.Create(this.startKey, this.endKey, this.tailCount, this.tailRange);
                                this.cache.views.Add(viewKey, view);
                            }
                        }
                    }
                }
            }

            private class Enumerator : IEnumerator<TItem>
            {
                private int index;
                private TItem value;
                private ObservableKeyedView view;

                public Enumerator(ObservableKeyedView view)
                {
                    this.view = view;
                    this.index = 0;
                    this.value = default(TItem);
                }

                public TItem Current => this.value;

                object IEnumerator.Current => this.value;

                public void Dispose()
                {
                    this.index = 0;
                    this.value = default(TItem);
                }

                public bool MoveNext()
                {
                    if (this.index < this.view.Count)
                    {
                        this.value = this.view[this.index];
                        this.index++;
                        return true;
                    }

                    this.index = this.view.endIndex;
                    this.value = default(TItem);
                    return false;
                }

                public void Reset()
                {
                    this.index = 0;
                }
            }
        }
    }
}
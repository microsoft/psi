// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Collections
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using C5;

    /// <summary>
    /// Represents a dynamic data collection whose keys are embedded in the values and provides
    /// notifications when items get added, removed, or when the whole list is refreshed.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    public class ObservableSortedCollection<T> : System.Collections.Generic.IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <summary>
        /// Default initial collection capacity.
        /// </summary>
        public const int DefaultCapacity = 1024;

        /// <summary>
        /// Indexer name.
        /// </summary>
        private const string IndexerName = "Item[]";

        /// <summary>
        /// Underlying sorted array of itmes.
        /// </summary>
        private SortedArray<T> items;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSortedCollection{T}"/> class that uses the default comparer.
        /// </summary>
        public ObservableSortedCollection()
            : this(ObservableSortedCollection<T>.DefaultCapacity, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSortedCollection{T}"/> class that uses the default comparer.
        /// </summary>
        /// <param name="comparer">
        /// The implementation of the <see cref="IComparer{T}"/> generic interface to use when comparing items, or null
        /// to use the default comparer for the type of the item, obtained from <see cref="Comparer{T}"/>.Default.
        /// </param>
        /// <param name="equalityComparer">
        /// The implementation of the <see cref="IEqualityComparer{T}"/> generic interface to use when comparing the equality items,
        /// or null to use the default equality comparer for the type of the item, obtained from <see cref="C5.EqualityComparer{T}"/>.Default.
        /// </param>
        public ObservableSortedCollection(IComparer<T> comparer, IEqualityComparer<T> equalityComparer)
            : this(ObservableSortedCollection<T>.DefaultCapacity, comparer, equalityComparer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservableSortedCollection{T}"/> class that uses the specified comparer.
        /// </summary>
        /// <param name="capacity">
        /// The initial capacity.
        /// </param>
        /// <param name="comparer">
        /// The implementation of the <see cref="IComparer{T}"/> generic interface to use when comparing items, or null
        /// to use the default comparer for the type of the item, obtained from <see cref="Comparer{T}"/>.Default.
        /// </param>
        /// <param name="equalityComparer">
        /// The implementation of the <see cref="IEqualityComparer{T}"/> generic interface to use when comparing the equality items,
        /// or null to use the default equality comparer for the type of the item, obtained from <see cref="C5.EqualityComparer{T}"/>.Default.
        /// </param>
        public ObservableSortedCollection(int capacity, IComparer<T> comparer, IEqualityComparer<T> equalityComparer)
        {
            this.Comparer = comparer == null ? Comparer<T>.Default : comparer;
            this.EqualityComparer = equalityComparer == null ? C5.EqualityComparer<T>.Default : equalityComparer;
            this.items = new SortedArray<T>(capacity, this.Comparer, this.EqualityComparer);
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

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        protected virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { this.PropertyChanged += value; }
            remove { this.PropertyChanged -= value; }
        }

        /// <inheritdoc/>
        public int Count => this.items.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => this.items.IsReadOnly;

        /// <summary>
        /// Gets the comparer used by underlying array.
        /// </summary>
        protected IComparer<T> Comparer { get; private set; }

        /// <summary>
        /// Gets the equality comparer used by underlying array.
        /// </summary>
        protected IEqualityComparer<T> EqualityComparer { get; private set; }

        /// <inheritdoc/>
        public T this[int index]
        {
            get
            {
                return this.items[index];
            }

            set { throw new NotSupportedException("ObservableSortedCollection does not support assignemt by index."); }
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="ObservableSortedCollection{T}"/>.
        /// </summary>
        /// <param name="collection">The collection whose elements should be added to the end of the <see cref="ObservableSortedCollection{T}"/>.
        /// The collection itself cannot be null, but it can contain elements that are null, if type <typeparamref name="T"/> is a reference type.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }

            // The items to be added in sorted order
            var changedItems = new SortedArray<T>(this.Comparer);
            changedItems.AddAll(collection);

            if (changedItems.Count != 0)
            {
                // Since changedItems are already sorted, use AddSorted instead of AddAll
                this.items.AddSorted(changedItems);

                var startingIndex = this.IndexOf(changedItems[0]);
                this.OnPropertyChanged(nameof(this.Count));
                this.OnPropertyChanged(IndexerName);
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(changedItems), startingIndex));
            }
        }

        /// <inheritdoc/>
        public void Add(T item)
        {
            this.items.Add(item);
            var index = this.items.IndexOf(item);
            this.OnPropertyChanged(nameof(this.Count));
            this.OnPropertyChanged(IndexerName);
            this.OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }

        /// <summary>
        /// Updates an existing item in the <see cref="ObservableSortedCollection{T}"/> with the specified item.
        /// The item is added if it does not already exist in the collection.
        /// </summary>
        /// <param name="item">The item to update or add.</param>
        public void UpdateOrAdd(T item)
        {
            T oldItem;

            // Try to update existing value with item, otherwise add the item
            bool updated = this.items.UpdateOrAdd(item, out oldItem);

            int index = this.items.IndexOf(item);
            if (updated)
            {
                // Existing item was updated
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
            }
            else
            {
                // Item was added
                this.OnPropertyChanged(nameof(this.Count));
                this.OnPropertyChanged(IndexerName);
                this.OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            this.items.Clear();
            this.OnPropertyChanged(nameof(this.Count));
            this.OnPropertyChanged(IndexerName);
            this.OnCollectionReset();
        }

        /// <inheritdoc/>
        public bool Contains(T item)
        {
            return this.items.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(T[] array, int arrayIndex)
        {
            this.items.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public int IndexOf(T item)
        {
            var index = this.items.IndexOf(item);
            return index < 0 ? -1 : index;
        }

        /// <inheritdoc/>
        public void Insert(int index, T item)
        {
            if (index != this.Count)
            {
                throw new NotSupportedException("ObservableSortedCollection only supports insertion at the end of the collection.");
            }

            this.Add(item);
        }

        /// <inheritdoc/>
        public bool Remove(T item)
        {
            var index = this.items.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            this.InternalRemove(index, 1);
            return true;
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            this.InternalRemove(index, 1);
        }

        /// <summary>
        /// Removes a contiguous range of items beginning at the specified index.
        /// </summary>
        /// <param name="index">The zero-based starting index of the items to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        public void RemoveRange(int index, int count)
        {
            this.InternalRemove(index, count);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        /// <summary>
        /// Raises the <see cref="ObservableSortedCollection{T}.CollectionChanged" /> event with the provided arguments.
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
        /// Raises the <see cref="ObservableSortedCollection{T}.PropertyChanged" /> event with the provided arguments.
        /// </summary>
        /// <param name="e">Arguments of the event being raised.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, e);
        }

        private void InternalRemove(int index, int count)
        {
            // Make a copy of the range that is being removed
            T[] removedItems = this.items[index, count].ToArray();

            // Do the actual remove and notifications
            this.items.RemoveInterval(index, count);
            this.OnPropertyChanged(nameof(this.Count));
            this.OnPropertyChanged(IndexerName);
            this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems, index);
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
        }

        private void OnCollectionChanged(NotifyCollectionChangedAction action, IList items, int index)
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, items, index));
        }

        private void OnCollectionReset()
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }
    }
}
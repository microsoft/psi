// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Collections
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// The purpose of ObservableDataCollection is to optimize exposing non-bindable lists to XAML binding. It does this in two ways:
    /// 1) It surfaces as an ObservableCollection which minimizes item removals and resets.
    /// 2) It wraps non IPropertyChangeNotify objects so that they can be updated wholesale (via indirection in a ObservableDataItem) so that updates don't cause a collection change to force bindings to update.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public class ObservableDataCollection<T> : ObservableCollection<ObservableDataItem<T>>
    {
        private ObservableCollection<T> observableSource;

        /// <summary>
        /// Sets the obvervable source.
        /// </summary>
        /// <param name="source">Souce collection to observe.</param>
        public void SetSource(IList<T> source)
        {
            if (this.observableSource != null)
            {
                this.observableSource.CollectionChanged -= this.Source_CollectionChanged;
                this.observableSource = null;
            }

            this.observableSource = source as ObservableCollection<T>;
            if (this.observableSource != null)
            {
                this.observableSource.CollectionChanged += this.Source_CollectionChanged;
            }

            this.SyncCollection(source);
        }

        private void Source_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    {
                        for (int i = 0; i < e.NewItems.Count; i++)
                        {
                            this.Insert(e.NewStartingIndex + i, new ObservableDataItem<T>((T)e.NewItems[i]));
                        }

                        break;
                    }

                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    {
                        for (int i = 0; i < e.OldItems.Count; i++)
                        {
                            this.RemoveAt(e.OldStartingIndex);
                        }

                        break;
                    }

                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                default:
                    this.SyncCollection(this.observableSource);
                    break;
            }
        }

        private void SyncCollection(IList<T> source)
        {
            if (source == null)
            {
                this.Clear();
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                if (this.Count > i)
                {
                    this[i].Data = source[i];
                }
                else
                {
                    this.Add(new ObservableDataItem<T>(source[i]));
                }
            }

            // remove extras
            for (int i = source.Count; i < this.Count; i++)
            {
                this.RemoveAt(i);
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents a dictionary of 3D visualization objects.
    /// </summary>
    /// <typeparam name="TVisObj">The type of visualization object in the dictionary.</typeparam>
    /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
    /// <typeparam name="TData">The type of data being represented.</typeparam>
    public class UpdatableModelVisual3DVisualizationObjectDictionary<TVisObj, TKey, TData> : ModelVisual3DVisualizationObjectCollectionBase<TVisObj, Dictionary<TKey, TData>>
        where TVisObj : ModelVisual3DVisualizationObject<TData>, new()
    {
        private readonly Dictionary<TKey, TVisObj> visuals = new Dictionary<TKey, TVisObj>();

        // The index of the current item (only valid during an update operation)
        private List<TKey> updatedKeys = new List<TKey>();

        // Specifies whether we're currently inside a BeginUpdate/EndUpdate operation.
        private bool isUpdating = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatableModelVisual3DVisualizationObjectDictionary{TVisObj, TKey, TData}"/> class.
        /// </summary>
        public UpdatableModelVisual3DVisualizationObjectDictionary()
        {
            this.Items = this.visuals.Values;
        }

        /// <summary>
        /// Gets the collection of keys in the children collection.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Dictionary<TKey, TVisObj>.KeyCollection Keys => this.visuals.Keys;

        /// <summary>
        /// Gets the visual at the specified index.  If no visual yet exists at the index one
        /// will be created and the newVisualHandler method will be called to initialize it.
        /// </summary>
        /// <param name="key">The key of the visual to return.</param>
        /// <returns>The visual at the specified index.</returns>
        public TVisObj this[TKey key]
        {
            get
            {
                if (!this.isUpdating)
                {
                    throw new InvalidOperationException("BeginUpdate() must be called before accessing the collection");
                }

                // If no visual yet exists at this index, create it
                if (!this.visuals.ContainsKey(key))
                {
                    // Create a new child TVisObj.  It will already be
                    // initialized with all the properties of the prototype.
                    TVisObj visual = this.CreateNew();

                    // Add the visual to the collection and to the model visual
                    this.visuals[key] = visual;
                    this.ModelView.Children.Add(visual.ModelView);
                }

                if (!this.updatedKeys.Contains(key))
                {
                    this.updatedKeys.Add(key);
                }

                return this.visuals[key];
            }
        }

        /// <summary>
        /// Begins an update of the elements of the collection.  Once all required elements have been updated call EndUpdate() to purge any surplus visuals.
        /// </summary>
        public void BeginUpdate()
        {
            if (this.isUpdating)
            {
                throw new InvalidOperationException("BeginUpdate() may not be called until the previous update operation has been completed by calling EndUpdate().");
            }

            this.updatedKeys.Clear();
            this.isUpdating = true;
        }

        /// <summary>
        /// Called when updates to the collection are completed.  Any
        /// extra child visuals will be removed from the collection.
        /// </summary>
        public void EndUpdate()
        {
            if (!this.isUpdating)
            {
                throw new InvalidOperationException("EndUpdate() may not be called before BeginUpdate() is called.");
            }

            // Remove all visuals that were not accessed during the update
            foreach (TKey key in this.visuals.Keys.Where(eid => !this.updatedKeys.Contains(eid)).ToList())
            {
                this.ModelView.Children.Remove(this.visuals[key].ModelView);
                this.visuals.Remove(key);
            }

            this.isUpdating = false;
        }

        /// <inheritdoc/>
        public override void UpdateData(Dictionary<TKey, TData> currentData, DateTime originatingTime)
        {
            if (currentData == null)
            {
                this.RemoveAll();
            }
            else
            {
                this.BeginUpdate();

                foreach (var datum in currentData)
                {
                    this[datum.Key].UpdateData(datum.Value, originatingTime);
                }

                this.EndUpdate();
            }
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            base.NotifyPropertyChanged(propertyName);
        }

        private void RemoveAll()
        {
            this.visuals.Clear();
            this.ModelView.Children.Clear();
        }
    }
}

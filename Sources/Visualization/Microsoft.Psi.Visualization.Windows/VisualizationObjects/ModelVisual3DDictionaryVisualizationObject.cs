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
    /// Implements a dictionary of 3D visualization objects.
    /// </summary>
    /// <typeparam name="TVisualizationObject">The type of visualization object in the dictionary.</typeparam>
    /// <typeparam name="TKey">The type of dictionary keys.</typeparam>
    /// <typeparam name="TData">The type of data being represented.</typeparam>
    public class ModelVisual3DDictionaryVisualizationObject<TVisualizationObject, TKey, TData> :
        ModelVisual3DCollectionVisualizationObject<TVisualizationObject, Dictionary<TKey, TData>>
        where TVisualizationObject : ModelVisual3DVisualizationObject<TData>, new()
    {
        private readonly Dictionary<TKey, TVisualizationObject> visuals = new ();

        // The index of the current item (only valid during an update operation)
        private readonly List<TKey> updatedKeys = new ();

        // Specifies whether we're currently inside a BeginUpdate/EndUpdate operation.
        private bool isUpdating = false;

        // Specifies a predicate that determines the visibility of the individual items.
        private Predicate<TKey> visibilityPredicate = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelVisual3DDictionaryVisualizationObject{TVisualizationObject, TKey, TData}"/> class.
        /// </summary>
        public ModelVisual3DDictionaryVisualizationObject()
        {
            this.Items = this.visuals.Values;
        }

        /// <summary>
        /// Gets the collection of keys in the children collection.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Dictionary<TKey, TVisualizationObject>.KeyCollection Keys => this.visuals.Keys;

        /// <summary>
        /// Gets the visual at the specified index.  If no visual yet exists at the index one
        /// will be created and the newVisualHandler method will be called to initialize it.
        /// </summary>
        /// <param name="key">The key of the visual to return.</param>
        /// <returns>The visual at the specified index.</returns>
        public TVisualizationObject this[TKey key]
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
                    TVisualizationObject visual = this.CreateNew();

                    // Add the visual to the collection and to the model visual
                    this.visuals[key] = visual;
                    if (this.visibilityPredicate != null)
                    {
                        this.visuals[key].Visible = this.visibilityPredicate(key);
                    }

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
        public override void UpdateData()
        {
            if (this.CurrentData == null)
            {
                this.RemoveAll();
            }
            else
            {
                this.BeginUpdate();

                foreach (var datum in this.CurrentData)
                {
                    this[datum.Key].SetCurrentValue(this.SynthesizeMessage(datum.Value));
                }

                this.EndUpdate();
            }
        }

        /// <summary>
        /// Set the visibility of the 3D visualization objects based on a specified predicate.
        /// </summary>
        /// <param name="visibilityPredicate">A predicate that determines whether the visualization object corresponding to a given key is visible.</param>
        public void SetVisibility(Predicate<TKey> visibilityPredicate)
        {
            this.visibilityPredicate = visibilityPredicate;
            foreach (var key in this.visuals.Keys)
            {
                this.visuals[key].Visible = visibilityPredicate(key);
            }
        }

        private void RemoveAll()
        {
            this.visuals.Clear();
            this.ModelView.Children.Clear();
        }
    }
}

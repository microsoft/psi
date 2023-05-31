// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Implements a visualization object that contains a dictionary of 3D visuals.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TVisual3D">The type of visual in the dictionary.</typeparam>
    public class UpdatableVisual3DDictionary<TKey, TVisual3D> : ModelVisual3D
        where TVisual3D : Visual3D, new()
    {
        // The collection of child visuals.
        private readonly Dictionary<TKey, TVisual3D> visuals = new ();

        // The method that will be called when we need to initialize a new visual.
        private readonly NewVisualHandler newVisualHandler;

        // The index of the current item (only valid during an update operation)
        private readonly List<TKey> updatedKeys = new ();

        // Specifies whether we're currently inside a BeginUpdate/EndUpdate operation.
        private bool isUpdating = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatableVisual3DDictionary{TKey,TVisual}"/> class.
        /// </summary>
        /// <param name="newVisualHandler">The delegate that identifies the method to call when a new TVisual
        /// needs to be initialized.  This parameter can be null if no initialization is required.</param>
        public UpdatableVisual3DDictionary(NewVisualHandler newVisualHandler)
        {
            this.newVisualHandler = newVisualHandler;
        }

        /// <summary>
        /// The callback that get invoked when this dictionary wishes to initialize a new instance of TVisual.
        /// </summary>
        /// <param name="newVisual">The newly created TVisual.</param>
        /// <param name="newKey">The newly created TKey.</param>
        public delegate void NewVisualHandler(TVisual3D newVisual, TKey newKey);

        /// <summary>
        /// Gets the collection of visuals in the dictionary.
        /// </summary>
        public Dictionary<TKey, TVisual3D>.ValueCollection Values => this.visuals.Values;

        /// <summary>
        /// Gets the visual at the specified index.  If no visual yet exists at the index one
        /// will be created and the newVisualHandler method will be called to initialize it.
        /// </summary>
        /// <param name="key">The key of the visual to return.</param>
        /// <returns>The visual at the specified index.</returns>
        public TVisual3D this[TKey key]
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
                    // Create the new visual.
                    var visual = new TVisual3D();

                    // Initialize the visual if an initializer method was specified.
                    this.newVisualHandler?.Invoke(visual, key);

                    // Add the visual to the collection and to the model visual
                    this.visuals[key] = visual;
                    this.Children.Add(visual);
                }

                if (!this.updatedKeys.Contains(key))
                {
                    this.updatedKeys.Add(key);
                }

                return this.visuals[key];
            }
        }

        /// <summary>
        /// Try to get a visual that already exists for the given key, without updating anything.
        /// </summary>
        /// <param name="key">The key to try and get the visual for.</param>
        /// <param name="visual">The visual corresponding to the given key, or null if there is no such visual.</param>
        /// <returns>True if a visual exists for the key.</returns>
        public bool TryGetVisual(TKey key, out TVisual3D visual)
        {
            if (this.visuals.ContainsKey(key))
            {
                visual = this.visuals[key];
                return true;
            }
            else
            {
                visual = null;
                return false;
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
                this.Children.Remove(this.visuals[key]);
                this.visuals.Remove(key);
            }

            this.isUpdating = false;
        }
    }
}

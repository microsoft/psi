// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Media.Media3D;

    /// <summary>
    /// Represents a visualization object that contains a collection of 3D visuals.
    /// </summary>
    /// <typeparam name="TVisual">The type of visual in the list.</typeparam>
    public class UpdatableVisual3DList<TVisual> : ModelVisual3D
        where TVisual : Visual3D, new()
    {
        // The collection of child visuals.
        private List<TVisual> visuals = new List<TVisual>();

        // The method that will be called when we need to initialize a new visual.
        private NewVisualHandler newVisualHandler;

        // The index of the current item (only valid during an update operation)
        private int currentIndex = -1;

        // Specifies whether we're currently inside a BeguinUpdate/EndUpdate operation.
        private bool isUpdating = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdatableVisual3DList{TVisual}"/> class.
        /// </summary>
        /// <param name="newVisualHandler">The delegate that identifies the method to call whne a new TVisual
        /// needs to be initialized.  This parameter can be null if no initialization is required.</param>
        public UpdatableVisual3DList(NewVisualHandler newVisualHandler)
        {
            this.newVisualHandler = newVisualHandler;
        }

        /// <summary>
        /// The callback that get invoked when this collection wishes to initialize a new instance of TVisual.
        /// </summary>
        /// <param name="newVisual">The newly created TVisual.</param>
        public delegate void NewVisualHandler(TVisual newVisual);

        /// <summary>
        /// Begins an update of the elements of the collection.  Once all required elements have been updated call EndUpdate() to purge any surplus visuals.
        /// </summary>
        public void BeginUpdate()
        {
            if (this.isUpdating)
            {
                throw new InvalidOperationException("BeginUpdate() may not be called until the previous update operation has been completed by calling EndUpdate().");
            }

            this.currentIndex = -1;
            this.isUpdating = true;
        }

        /// <summary>
        /// Gets the visual at the current index.  This method will throw an exception if there
        /// is no current visual, or if this method is called outside of an update operation.
        /// </summary>
        /// <returns>The current visual.</returns>
        public TVisual GetCurrent()
        {
            if (!this.isUpdating)
            {
                throw new InvalidOperationException("BeginUpdate() must be called before calling GetCurrent()");
            }

            if (this.currentIndex < 0)
            {
                throw new InvalidOperationException("The is no current visual to return");
            }

            return this.visuals[this.currentIndex];
        }

        /// <summary>
        /// Advances to the next item in the collection (creating it if required) and returns it.
        /// This method will throw an exception if it is called outside of an update operation.
        /// </summary>
        /// <returns>The next visual in the collection.</returns>
        public TVisual GetNext()
        {
            if (!this.isUpdating)
            {
                throw new InvalidOperationException("BeginUpdate() must be called before calling GetNext()");
            }

            // Move to the index of the next visual
            this.currentIndex++;

            // If no visual yet exists at this index, create it
            while (this.visuals.Count <= this.currentIndex)
            {
                // Create the new visual.
                TVisual visual = new TVisual();

                // Initialize the visual if an initializer method was specified.
                if (this.newVisualHandler != null)
                {
                    this.newVisualHandler(visual);
                }

                // Add the visual to the collection and to the model visual
                this.visuals.Add(visual);
                this.Children.Add(visual);
            }

            return this.visuals[this.currentIndex];
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

            // Remove all visuals that were not touched by the indexer during the update
            while (this.visuals.Count > this.currentIndex + 1)
            {
                this.visuals.RemoveAt(this.visuals.Count - 1);
                this.Children.RemoveAt(this.Children.Count - 1);
            }

            this.currentIndex = -1;
            this.isUpdating = false;
        }
    }
}

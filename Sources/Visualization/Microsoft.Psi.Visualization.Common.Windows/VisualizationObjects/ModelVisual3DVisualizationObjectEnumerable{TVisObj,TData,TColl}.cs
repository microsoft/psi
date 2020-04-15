// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a list of 3D model visualization objects.
    /// </summary>
    /// <typeparam name="TVisObj">The type of visualization objects in the collection.</typeparam>
    /// <typeparam name="TData">The type of data of each item in the collection.</typeparam>
    /// <typeparam name="TColl">The type of of the collection.</typeparam>
    public abstract class ModelVisual3DVisualizationObjectEnumerable<TVisObj, TData, TColl> : ModelVisual3DVisualizationObjectCollectionBase<TVisObj, TColl>
        where TVisObj : ModelVisual3DVisualizationObject<TData>, new()
        where TColl : IEnumerable<TData>
    {
        // The collection of child visualization objects.
        private List<TVisObj> children = new List<TVisObj>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelVisual3DVisualizationObjectEnumerable{TVisObj, TData, TColl}"/> class.
        /// </summary>
        public ModelVisual3DVisualizationObjectEnumerable()
        {
            this.Items = this.children;
        }

        /// <inheritdoc/>
        public override void UpdateData(TColl currentData, DateTime originatingTime)
        {
            if (currentData != null)
            {
                int index = 0;
                foreach (TData datum in currentData)
                {
                    // If we don't have enought visualization objects, create a new one
                    while (index >= this.ModelView.Children.Count)
                    {
                        this.AddNew(datum);
                    }

                    // Get the child visualization object to update itself
                    this.children[index].UpdateData(datum, originatingTime);

                    index++;
                }

                // If we have more visualization objects than data, remove the extras
                while (index < this.ModelView.Children.Count)
                {
                    this.Remove(index);
                }
            }
            else
            {
                // No data, remove everything
                this.RemoveAll();
            }
        }

        private void AddNew(TData datum)
        {
            // Create a new child TVisObj.  It will already be
            // initialized with all the properties of the prototype.
            TVisObj child = this.CreateNew();

            // Add it to the collection
            this.children.Add(child);

            // Ad the new visualization object's model view as a child of our model view
            this.ModelView.Children.Add(child.ModelView);
        }

        private void Remove(int index)
        {
            // Remove the visualization object's model view from our model view
            this.ModelView.Children.RemoveAt(index);

            // remove the visualization object
            this.children.RemoveAt(index);
        }

        private void RemoveAll()
        {
            this.ModelView.Children.Clear();
            this.children.Clear();
        }
    }
}

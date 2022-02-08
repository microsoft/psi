// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a list of 3D model visualization objects.
    /// </summary>
    /// <typeparam name="TVisualizationObject">The type of visualization objects in the collection.</typeparam>
    /// <typeparam name="TData">The type of data of each item in the collection.</typeparam>
    public abstract class ModelVisual3DListVisualizationObject<TVisualizationObject, TData> :
        ModelVisual3DCollectionVisualizationObject<TVisualizationObject, List<TData>>
        where TVisualizationObject : ModelVisual3DVisualizationObject<TData>, new()
    {
        // The collection of child visualization objects.
        private readonly List<TVisualizationObject> children = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelVisual3DListVisualizationObject{TVisualizationObject, TData}"/> class.
        /// </summary>
        public ModelVisual3DListVisualizationObject()
        {
            this.Items = this.children;
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            if (this.CurrentData != null)
            {
                int index = 0;
                foreach (TData datum in this.CurrentData)
                {
                    // If we don't have enough visualization objects, create a new one
                    while (index >= this.ModelView.Children.Count)
                    {
                        this.AddNew();
                    }

                    // Get the child visualization object to update itself
                    this.children[index].SetCurrentValue(this.SynthesizeMessage(datum));

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

        private void AddNew()
        {
            // Create a new child TVisObj.  It will already be
            // initialized with all the properties of the prototype.
            TVisualizationObject child = this.CreateNew();

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

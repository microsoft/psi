// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a model visual 3d visualization object.
    /// </summary>
    public interface IModelVisual3DVisualizationObject
    {
        /// <summary>
        /// Called by a parent IModelVisual3DVisualizationObject on a child to register
        /// for notifications from the child when one of its properties changes.
        /// </summary>
        /// <param name="parent">The parent of this IModelVisual3DVisualizationObject.</param>
        /// <param name="childName">The parent's name for this child IModelVisual3DVisualizationObject.</param>
        void RegisterChildPropertyChangedNotifications(IModelVisual3DVisualizationObject parent, string childName);

        /// <summary>
        /// Called when a child object wishes to notify its parent that one of its properties has changed.
        /// </summary>
        /// <param name="path">The path from the current visualization object to the property that was changed.</param>
        /// <param name="value">The new value of the property.</param>
        void OnChildPropertyChanged(string path, object value);

        /// <summary>
        /// Called when we need to notify the child visualization objects that a proeprty has changed.
        /// </summary>
        /// <param name="path">The path from the current visualization object to the property to set.</param>
        /// <param name="newValue">The new value for the proeprty.</param>
        void SetDescendantProperty(string path, object newValue);
    }
}

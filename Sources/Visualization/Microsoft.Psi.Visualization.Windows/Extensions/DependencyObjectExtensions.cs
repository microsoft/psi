// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Extensions
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Extension methods for use with <see cref="DependencyObject"/> instances.
    /// </summary>
    public static class DependencyObjectExtensions
    {
        /// <summary>
        /// Finds a child of a given item in the visual tree.
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="name">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter.
        /// If not matching item can be found, a null parent is being returned.</returns>
        public static T FindChild<T>(this DependencyObject parent, string name)
            where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                T childType = child as T;
                if (childType != null)
                {
                    // cursor child is type we are looking for, no compare name (if given)
                    if (!string.IsNullOrEmpty(name))
                    {
                        // If the child's name is set for search
                        var frameworkElement = child as FrameworkElement;
                        if (frameworkElement != null && frameworkElement.Name == name)
                        {
                            // if the child's name is of the request name
                            return (T)child;
                        }
                    }
                    else
                    {
                        // child element found.
                        return (T)child;
                    }
                }

                // recursively drill down the tree
                var found = child.FindChild<T>(name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}

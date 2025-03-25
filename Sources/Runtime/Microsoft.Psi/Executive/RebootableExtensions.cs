// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// Extensions for activating the possibility of rebooting and modifing pipeline structure.
namespace Microsoft.Psi
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class containing rebootable extensions for the executive subsystem.
    /// </summary>
    public static class RebootableExtensions
    {
        /// <summary>
        /// Retrieve the specified components from the given type.
        /// </summary>
        /// <typeparam name="T">The type of component to retrieve.</typeparam>
        /// <param name="pipeline">The pipeline to get elements from.</param>
        /// <returns>The list of the components found inside the pipeline.</returns>
        public static List<T> GetElementsOfType<T>(this Pipeline pipeline)
        {
            List<T> components = new List<T>();
            foreach (var component in pipeline.Components)
            {
                if (component.StateObject is T element)
                {
                    components.Add(element);
                }
            }

            return components;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma
{
    /// <summary>
    /// Represents the state of the gem.
    /// </summary>
    public class GemState
    {
        private GemState(string pointingToObject)
        {
            this.ObjectName = pointingToObject;
        }

        /// <summary>
        /// Gets the name of the object the gem is pointing to.
        /// </summary>
        public string ObjectName { get; }

        /// <summary>
        /// Creates a new gem state in which the gem is pointing to an object.
        /// </summary>
        /// <param name="objectName">The name of the object.</param>
        /// <returns>The gem state.</returns>
        public static GemState PointingToObject(string objectName)
            => new (objectName);

        /// <summary>
        /// Creates a new gem state in which the gem is at the user interface.
        /// </summary>
        /// <returns>The gem state.</returns>
        public static GemState AtUserInterface()
            => new (null);

        /// <summary>
        /// Gets a value indicating whether the gem is at the user interface.
        /// </summary>
        /// <returns>True if the gem is at the user interface.</returns>
        public bool IsAtUserInterface()
            => string.IsNullOrEmpty(this.ObjectName);

        /// <summary>
        /// Gets a value indicating whether the gem is pointing to an object.
        /// </summary>
        /// <param name="objectName">The name of the object.</param>
        /// <returns>True if the gem is pointing to an object.</returns>
        public bool IsPointingToObject(out string objectName)
        {
            objectName = this.ObjectName;
            return !string.IsNullOrEmpty(this.ObjectName);
        }
    }
}

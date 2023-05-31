// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;

    /// <summary>
    /// Represents the event args passed by the <see cref="VisualizationContext.RequestDisplayObjectProperties"/> event.
    /// </summary>
    public class RequestDisplayObjectPropertiesEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestDisplayObjectPropertiesEventArgs"/> class.
        /// </summary>
        /// <param name="requestingObject">The object that is requesting its proeprties be displayed.</param>
        public RequestDisplayObjectPropertiesEventArgs(object requestingObject)
        {
            this.Object = requestingObject;
        }

        /// <summary>
        /// Gets or sets the object that is requesting its properties be displayed.
        /// </summary>
        public object Object { get; set; }
    }
}

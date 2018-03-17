// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Server
{
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Reference counted object base.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    [ComVisible(true)]
    public class ReferenceCountedObject : ComObservableObject, INotifyPropertyChanged, INotifyPropertyChanging
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceCountedObject"/> class.
        /// </summary>
        public ReferenceCountedObject()
        {
            // Increment the lock count of objects in the COM server.
            ComServer.Instance.Lock();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ReferenceCountedObject"/> class.
        /// </summary>
        ~ReferenceCountedObject()
        {
            // Decrement the lock count of objects in the COM server.
            ComServer.Instance.Unlock();
        }
    }
}

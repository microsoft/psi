// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Base
{
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;

    /// <summary>
    /// Observable object base that replaces the standard common language runtime (CLR) free-threaded marshaler with the standard OLE STA marshaler.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    [ComVisible(true)]
    public class ComObservableObject : StandardOleMarshalObject, INotifyPropertyChanged, INotifyPropertyChanging
    {
        /// <inheritdoc />
        public event PropertyChangingEventHandler PropertyChanging;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the property changing event.
        /// </summary>
        /// <param name="propertyName">The name of the property whose value is changing.</param>
        protected void RaisePropertyChanging(string propertyName)
        {
            this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the poerty changed event.
        /// </summary>
        /// <param name="propertyName">The name of the property whose value has changed.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the value of the named property and raises the changing and changed events.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The name of the property whose value is being set.</param>
        /// <param name="member">The property to set the value on.</param>
        /// <param name="value">The new value of the property.</param>
        protected void Set<T>(string property, ref T member, T value)
        {
            if ((member != null && !member.Equals(value)) || value != null)
            {
                this.RaisePropertyChanging(property);
                member = value;
                this.RaisePropertyChanged(property);
            }
        }
    }
}

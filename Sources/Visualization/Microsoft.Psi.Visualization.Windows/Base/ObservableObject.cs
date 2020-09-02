// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Base
{
    using System.ComponentModel;
    using System.Runtime.Serialization;

    /// <summary>
    /// Provides an implementation of <see cref="INotifyPropertyChanged"/> and <see cref="INotifyPropertyChanging"/> for use in implementing bindable objects.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class ObservableObject : INotifyPropertyChanged, INotifyPropertyChanging
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public event PropertyChangingEventHandler PropertyChanging;

        /// <summary>
        /// Raises the property changed event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the property changing event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that is about to be changed.</param>
        protected void RaisePropertyChanging(string propertyName)
        {
            this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the value of the specified property and raises the appropriate events.
        /// </summary>
        /// <typeparam name="T">The type of the property to be set.</typeparam>
        /// <param name="propertyName">The name of the property to be set.</param>
        /// <param name="property">A reference to the property to be set.</param>
        /// <param name="value">The value to set the property to.</param>
        /// <returns>True if the value changed, otherwise false.</returns>
        protected bool Set<T>(string propertyName, ref T property, T value)
        {
            if ((property == null) && (value == null))
            {
                return false;
            }

            if ((property != null) && (value != null) && property.Equals(value))
            {
                return false;
            }

            this.RaisePropertyChanging(propertyName);
            property = value;
            this.RaisePropertyChanged(propertyName);
            return true;
        }
    }
}

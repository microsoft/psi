// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Server;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Base class for visualization objects
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(Guids.RemoteVisualizationObjectCLSIDString)]
    [ComVisible(false)]
    public abstract class VisualizationObject : ReferenceCountedObject, IRemoteVisualizationObject
    {
        private Dictionary<uint, INotifyRemoteConfigurationChanged> advises;
        private uint nextAdviseCookie;

        /// <summary>
        /// The top level visualization container this visualization object is parented under.
        /// </summary>
        private VisualizationContainer container;

        /// <summary>
        /// The visualization panel this visualization object is parented under.
        /// </summary>
        private VisualizationPanel panel;

        /// <summary>
        /// The navigator for for the container
        /// </summary>
        private Navigator navigator;

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationObject"/> class.
        /// </summary>
        public VisualizationObject()
        {
            this.InitNew();
        }

        /// <summary>
        /// Gets the default DataTemplate that is used within a VisualizationPanel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public abstract DataTemplate DefaultViewTemplate { get; }

        /// <summary>
        /// Gets the parent VisualizationContainer.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public VisualizationContainer Container => this.container;

        /// <summary>
        /// Gets the parent VisualizationPanel.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public VisualizationPanel Panel => this.panel;

        /// <summary>
        /// Gets the navigator this object is bound to.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public Navigator Navigator => this.navigator;

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        IRemoteNavigator IRemoteVisualizationObject.Navigator => this.Navigator;

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        IRemoteVisualizationContainer IRemoteVisualizationObject.Container => this.Container;

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        IRemoteVisualizationPanel IRemoteVisualizationObject.Panel => this.Panel;

        /// <inheritdoc />
        public uint Advise(INotifyRemoteConfigurationChanged notifyVisualizationObjectChanged)
        {
            uint cookie = this.nextAdviseCookie++;
            this.advises.Add(cookie, notifyVisualizationObjectChanged);
            return cookie;
        }

        /// <inheritdoc />
        public void BringToFront()
        {
            this.panel.BringToFront(this);
        }

        /// <inheritdoc />
        public void Close()
        {
            this.panel.RemoveVisualizationObject(this);
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method should not be directly called or overriden by implementors. It is for internal use only.
        /// </remarks>
        public abstract string GetConfiguration();

        /// <inheritdoc />
        public void SendToBack()
        {
            this.panel.SendToBack(this);
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method should not be directly called or overriden by implementors. It is for internal use only.
        /// </remarks>
        public abstract void SetConfiguration(string jsonConfiguration);

        /// <inheritdoc />
        public void Unadvise(uint cookie)
        {
            if (!this.advises.Remove(cookie))
            {
                Marshal.ThrowExceptionForHR(ComNative.E_POINTER);
            }
        }

        /// <summary>
        /// Called internally by the VisualizationPanel to connect the parent chain.
        /// </summary>
        /// <param name="panel">Panel to connect this visualization object to.</param>
        internal void SetParentPanel(VisualizationPanel panel)
        {
            if (this.panel != null)
            {
                this.OnDisconnect();
                this.navigator.CursorChanged -= this.OnCursorChanged;
                this.panel = null;
                this.container = null;
                this.navigator = null;
            }

            if (panel != null)
            {
                this.panel = panel;
                this.container = this.panel.Container;
                this.navigator = this.container.Navigator;
                this.navigator.CursorChanged += this.OnCursorChanged;
                this.OnConnect();
            }
        }

        /// <summary>
        /// Overridable method to allow derived VisualzationObject to initialize properties as part of object construction or after deserialization.
        /// </summary>
        protected virtual void InitNew()
        {
            this.advises = new Dictionary<uint, INotifyRemoteConfigurationChanged>();
            this.nextAdviseCookie = 1;
        }

        /// <summary>
        /// Notifiy client application when configuration has changed.
        /// </summary>
        protected void NotifyConfigurationChanged()
        {
            string jsonConfiguration = this.GetConfiguration();
            List<uint> orphanedAdvises = new List<uint>();
            foreach (var advise in this.advises)
            {
                try
                {
                    advise.Value.OnRemoteConfigurationChanged(jsonConfiguration);
                }
                catch (COMException)
                {
                    orphanedAdvises.Add(advise.Key);
                }
            }

            foreach (var orphanedAdvise in orphanedAdvises)
            {
                this.advises.Remove(orphanedAdvise);
            }
        }

        /// <summary>
        /// Overridable method to allow derived VisualzationObject to react whenever the Configuration property has changed.
        /// </summary>
        protected virtual void OnConfigurationChanged()
        {
            this.NotifyConfigurationChanged();
        }

        /// <summary>
        /// Overridable method to allow derived VisualzationObject to react whenever a property on the Configuration property has changed.
        /// </summary>
        /// <param name="propertyName">Name of property that has changed.</param>
        protected virtual void OnConfigurationPropertyChanged(string propertyName)
        {
        }

        /// <summary>
        /// Overideable method to allow derived VisualizationObjects to react to being loaded.
        /// </summary>
        protected virtual void OnConnect()
        {
        }

        /// <summary>
        /// Overideable method to allow derived VisualizationObjects to react to being unloaded.
        /// </summary>
        protected virtual void OnDisconnect()
        {
        }

        /// <summary>
        /// Overideable method to allow derived VisualizationObjects to react to cursor changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An object that contains navigator changed event data.</param>
        protected virtual void OnCursorChanged(object sender, NavigatorTimeChangedEventArgs e)
        {
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext c)
        {
            this.InitNew();
        }
    }
}

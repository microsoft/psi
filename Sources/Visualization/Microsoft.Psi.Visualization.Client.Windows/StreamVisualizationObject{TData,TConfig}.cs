// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Client
{
    using System;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Newtonsoft.Json;

    /// <summary>
    /// Class implements a generic client proxy Microsoft.Psi.Visualization.VisualizationObjects.StreamVisualizationObject{TData, TConfig}.
    /// </summary>
    /// <typeparam name="TData">The type of underlying data of the stream visualization object.</typeparam>
    /// <typeparam name="TConfig">The type of configuration of the stream visualization object</typeparam>
    public abstract class StreamVisualizationObject<TData, TConfig> : VisualizationObject<TConfig>
        where TConfig : StreamVisualizationObjectConfiguration, new()
    {
        private IRemoteStreamVisualizationObject streamVisualizationObject;

        /// <summary>
        /// Gets the remote stream visualization object.
        /// </summary>
        internal IRemoteStreamVisualizationObject RemoteStreamVisualizationObject
        {
            get
            {
                if (this.streamVisualizationObject == null)
                {
                    this.streamVisualizationObject = (IRemoteStreamVisualizationObject)this.IVisualizationObject;
                }

                return this.streamVisualizationObject;
            }
        }

        /// <summary>
        /// Closes the stream if one has been opened.
        /// </summary>
        public void CloseStream()
        {
            this.RemoteStreamVisualizationObject.CloseStream();
        }

        /// <summary>
        /// Opens a stream given a stream binding. Will close an open stream if needed.
        /// </summary>
        /// <param name="streamBinding">Stream binding inidicting which stream to open.</param>
        public void OpenStream(StreamBinding streamBinding)
        {
            string jsonStreamBinding = JsonConvert.SerializeObject(streamBinding);
            this.RemoteStreamVisualizationObject.OpenStream(jsonStreamBinding);
        }
    }
}

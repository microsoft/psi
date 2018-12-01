// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.Command;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Class implements a plot visualization object view model
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class PlotVisualizationObject : PlotVisualizationObject<PlotVisualizationObjectConfiguration>
    {
        private RelayCommand snapToStreamCommand;

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(PlotVisualizationObjectView));

        /// <summary>
        /// Gets the delete visualization command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand SnapToStreamCommand
        {
            get
            {
                if (this.snapToStreamCommand == null)
                {
                    this.snapToStreamCommand = new RelayCommand(
                        () =>
                        {
                            // If this is already the visualization object being snapped to, then
                            // reset snap to stream, otherwise set it to this visualization object.
                            // If another object was previously snapped, then ask it to unsnap itself
                            // so that the correct property changed event gets raised.
                            if (this.Container.SnapToVisualizationObject == null)
                            {
                                this.SnapToStream(true);
                            }
                            else if (this.Container.SnapToVisualizationObject == this)
                            {
                                this.SnapToStream(false);
                            }
                            else
                            {
                                this.Container.SnapToVisualizationObject.SnapToStream(false);
                                this.SnapToStream(true);
                            }
                        });
                }

                return this.snapToStreamCommand;
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool CanSnapToStream => true;

        /// <inheritdoc/>
        [Browsable(false)]
        [IgnoreDataMember]
        public override bool IsSnappedToStream => this.Container.SnapToVisualizationObject == this;

        /// <inheritdoc/>
        [Browsable(false)]
        public override void SnapToStream(bool snapToStream)
        {
            this.RaisePropertyChanging(nameof(this.IsSnappedToStream));
            this.Container.SnapToVisualizationObject = snapToStream ? this : null;
            this.RaisePropertyChanged(nameof(this.IsSnappedToStream));
        }
    }
}

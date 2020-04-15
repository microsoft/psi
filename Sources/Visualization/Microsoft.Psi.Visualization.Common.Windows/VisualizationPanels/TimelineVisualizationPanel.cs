// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.Controls;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Navigation;
    using Microsoft.Psi.Visualization.Views;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a visualization panel that time based visualizers can be rendered in.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimelineVisualizationPanel : VisualizationPanel
    {
        private bool showLegend = false;
        private bool showTimeTicks = false;

        private RelayCommand zoomToSessionExtentsCommand;
        private RelayCommand zoomToSelectionCommand;
        private RelayCommand showHideLegendCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonDownCommand;
        private Point lastMouseLeftButtonDownPoint = new Point(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineVisualizationPanel"/> class.
        /// </summary>
        public TimelineVisualizationPanel()
        {
            this.Name = "Timeline Panel";
            this.Height = 70;
            this.VisualizationObjects.CollectionChanged += this.VisualizationObjects_CollectionChanged;
        }

        /// <summary>
        /// Gets the Mouse Position the last time the user clicked in this panel.
        /// </summary>
        [Browsable(false)]
        public Point LastMouseLeftButtonDownPoint => this.lastMouseLeftButtonDownPoint;

        /// <summary>
        /// Gets the show/hide legend command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ShowHideLegendCommand
        {
            get
            {
                if (this.showHideLegendCommand == null)
                {
                    this.showHideLegendCommand = new RelayCommand(() => this.ShowLegend = !this.ShowLegend);
                }

                return this.showHideLegendCommand;
            }
        }

        /// <summary>
        /// Gets the zoom to selection command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToSelectionCommand
        {
            get
            {
                if (this.zoomToSelectionCommand == null)
                {
                    this.zoomToSelectionCommand = new RelayCommand(
                        () => this.Container.Navigator.ZoomToSelection(),
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.Container.Navigator.CursorMode != CursorMode.Live);
                }

                return this.zoomToSelectionCommand;
            }
        }

        /// <summary>
        /// Gets the zoom to session extents command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand ZoomToSessionExtentsCommand
        {
            get
            {
                if (this.zoomToSessionExtentsCommand == null)
                {
                    this.zoomToSessionExtentsCommand = new RelayCommand(
                        () => this.Container.Navigator.ZoomToDataRange(),
                        () => VisualizationContext.Instance.IsDatasetLoaded() && this.Container.Navigator.CursorMode != CursorMode.Live);
                }

                return this.zoomToSessionExtentsCommand;
            }
        }

        /// <summary>
        /// Gets the mouse left button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public override RelayCommand<MouseButtonEventArgs> MouseLeftButtonDownCommand
        {
            get
            {
                if (this.mouseLeftButtonDownCommand == null)
                {
                    this.mouseLeftButtonDownCommand = new RelayCommand<MouseButtonEventArgs>(
                        e =>
                        {
                            this.lastMouseLeftButtonDownPoint = e.GetPosition(e.Source as TimelineScroller);

                            // Set the current panel on click
                            if (!this.IsCurrentPanel)
                            {
                                this.Container.CurrentPanel = this;
                            }

                            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                            {
                                DateTime time = this.GetTimeAtMousePointer(e);
                                this.Navigator.SelectionRange.SetRange(time, this.Navigator.SelectionRange.EndTime);
                                e.Handled = true;
                            }
                        });
                }

                return this.mouseLeftButtonDownCommand;
            }
        }

        /// <summary>
        /// Gets the mouse right button down command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand<MouseButtonEventArgs> MouseRightButtonDownCommand
        {
            get
            {
                if (this.mouseRightButtonDownCommand == null)
                {
                    this.mouseRightButtonDownCommand = new RelayCommand<MouseButtonEventArgs>(
                        e =>
                        {
                            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                            {
                                DateTime time = this.GetTimeAtMousePointer(e);
                                this.Navigator.SelectionRange.SetRange(this.Navigator.SelectionRange.StartTime, time);
                                e.Handled = true;
                            }
                        });
                }

                return this.mouseRightButtonDownCommand;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the legend should be shown.
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [Description("Show the legend for the visualization panel.")]
        public bool ShowLegend
        {
            get { return this.showLegend; }
            set { this.Set(nameof(this.ShowLegend), ref this.showLegend, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the time ticks should be shown.
        /// </summary>
        [DataMember]
        [PropertyOrder(3)]
        [Description("Display an additional timeline along the bottom of the visualization panel.")]
        public bool ShowTimeTicks
        {
            get { return this.showTimeTicks; }
            set { this.Set(nameof(this.ShowTimeTicks), ref this.showTimeTicks, value); }
        }

        /// <inheritdoc />
        public override bool ShowZoomToPanelMenuItem => true;

        /// <inheritdoc />
        public override bool CanZoomToPanel => this.VisualizationObjects.Count > 0;

        /// <summary>
        /// Gets the context menu for the visualization panel.
        /// </summary>
        public ContextMenu ContextMenu
        {
            get
            {
                ContextMenu contextMenu = new ContextMenu();
                contextMenu.Items.Add(this.CreateMenuItem(IconSourcePath.Legend, this.ShowLegend ? "Hide Legend" : "Show Legend", this.ShowHideLegendCommand));
                contextMenu.Items.Add(this.CreateMenuItem(IconSourcePath.RemovePanel, "Remove Panel", this.RemovePanelCommand));
                contextMenu.Items.Add(this.CreateMenuItem(IconSourcePath.ClearPanel, "Clear", this.ClearPanelCommand));

                // Get the visualization object currently being snapped to (if any)
                VisualizationObject snappedVisualizationObject = this.Container.SnapToVisualizationObject;

                // Work out how many visualization objects we could potentially snap to.  If one of
                // this panel's visualization objects is currenlty being snapped to, then this total
                // is actually one fewer, and we'll also need to add an "unsnap" menu item.
                int snappableVisualizationObjectsCount = this.VisualizationObjects.Count;
                if ((snappedVisualizationObject != null) && this.VisualizationObjects.Contains(snappedVisualizationObject))
                {
                    snappableVisualizationObjectsCount--;
                    contextMenu.Items.Add(this.CreateMenuItem(IconSourcePath.Stream, string.Format("Unsnap from {0}", this.Container.SnapToVisualizationObject.Name), new VisualizationCommand((v) => this.Container.SnapToVisualizationObject.ToggleSnapToStream())));
                }

                // If there's only 1 snappable visualization object in this panel, then create a
                // direct menu, if there's more than 1 then create a cascading menu.
                if (snappableVisualizationObjectsCount == 1)
                {
                    VisualizationObject snappableVisualizationObject = this.VisualizationObjects.First(vo => vo != this.Container.SnapToVisualizationObject);
                    contextMenu.Items.Add(this.CreateMenuItem(IconSourcePath.SnapToStream, string.Format("Snap to {0}", snappableVisualizationObject.Name), new VisualizationCommand((v) => snappableVisualizationObject.ToggleSnapToStream())));
                }
                else if (snappableVisualizationObjectsCount > 1)
                {
                    // Create the top-level menu item
                    MenuItem snapMenuItem = this.CreateMenuItem(IconSourcePath.SnapToStream, "Snap to...", null);

                    // create the child menu items for each visualization object.
                    foreach (VisualizationObject visualizationObject in this.VisualizationObjects)
                    {
                        if (visualizationObject != this.Container.SnapToVisualizationObject)
                        {
                            snapMenuItem.Items.Add(this.CreateMenuItem(IconSourcePath.SnapToStream, visualizationObject.Name, new VisualizationCommand((v) => visualizationObject.ToggleSnapToStream())));
                        }
                    }

                    contextMenu.Items.Add(snapMenuItem);
                }

                contextMenu.Items.Add(this.CreateMenuItem(IconSourcePath.ZoomToSelection, "Zoom to Selection", this.ZoomToSelectionCommand));
                contextMenu.Items.Add(this.CreateMenuItem(IconSourcePath.ZoomToSession, "Zoom to Session Extents", this.ZoomToSessionExtentsCommand));

                return contextMenu;
            }
        }

        /// <inheritdoc/>
        protected override void OnContainerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Changes to the stream that's currently being snapped to will change the content of the context menu.
            if (e.PropertyName == nameof(this.Container.SnapToVisualizationObject))
            {
                this.RaisePropertyChanged(nameof(this.ContextMenu));
            }

            base.OnContainerPropertyChanged(sender, e);
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(TimelineVisualizationPanelView));
        }

        private MenuItem CreateMenuItem(string iconSourcePath, string commandText, ICommand command)
        {
            // Create the bitmap for the icon
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(iconSourcePath, UriKind.RelativeOrAbsolute);
            bitmapImage.EndInit();

            // Create the icon
            Image icon = new Image();
            icon.Height = 16;
            icon.Width = 16;
            icon.Margin = new Thickness(4, 0, 0, 0);
            icon.Source = bitmapImage;

            // Create the menuitem
            MenuItem menuItem = new MenuItem();
            menuItem.Height = 25;
            menuItem.Icon = icon;
            menuItem.Header = commandText;
            menuItem.Command = command;

            return menuItem;
        }

        private DateTime GetTimeAtMousePointer(MouseEventArgs e)
        {
            TimelineScroller root = this.FindTimelineScroller(e.Source);
            if (root != null)
            {
                Point point = e.GetPosition(root);
                double percent = point.X / root.ActualWidth;
                var viewRange = this.Navigator.ViewRange;
                DateTime time = viewRange.StartTime + TimeSpan.FromTicks((long)((double)viewRange.Duration.Ticks * percent));

                // If we're currently snapping to some Visualization Object, adjust the time to the timestamp of the nearest message
                DateTime? snappedTime = null;
                if (VisualizationContext.Instance.VisualizationContainer.SnapToVisualizationObject is IStreamVisualizationObject snapToVisualizationObject)
                {
                    snappedTime = snapToVisualizationObject.GetSnappedTime(time);
                }

                return snappedTime ?? time;
            }

            Debug.WriteLine("TimelineVisualizationPanel.GetTimeAtMousePointer() - Could not find the TimelineScroller in the tree");
            return DateTime.UtcNow;
        }

        private TimelineScroller FindTimelineScroller(object sourceElement)
        {
            // Walk up the visual tree until we either find the
            // Timeline Scroller or fall off the top of the tree
            FrameworkElement target = sourceElement as FrameworkElement;
            while (target != null && !(target is TimelineScroller))
            {
                target = target.Parent as FrameworkElement;
            }

            return target as TimelineScroller;
        }

        private void VisualizationObjects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.RaisePropertyChanged(nameof(this.CanZoomToPanel));
            this.RaisePropertyChanged(nameof(this.ContextMenu));
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Input;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Controls;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Represents a visualization panel that time based visualizers can be rendered in.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimelineVisualizationPanel : VisualizationPanel<TimelineVisualizationPanelConfiguration>
    {
        private RelayCommand showHideLegendCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonDownCommand;
        private Point lastMouseLeftButtonDownPoint = new Point(0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="TimelineVisualizationPanel"/> class.
        /// </summary>
        public TimelineVisualizationPanel()
        {
            this.VisualizationObjects.CollectionChanged += this.VisualizationObjects_CollectionChanged;
        }

        /// <summary>
        /// Gets the Mouse Position the last time the user clicked in this panel.
        /// </summary>
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
                    this.showHideLegendCommand = new RelayCommand(() => this.Configuration.ShowLegend = !this.Configuration.ShowLegend);
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
                return PsiStudioContext.Instance.ZoomToSelectionCommand;
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
                return PsiStudioContext.Instance.ZoomToSessionExtentsCommand;
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

        /// <inheritdoc />
        public override bool ShowZoomToPanelMenuItem => true;

        /// <inheritdoc />
        public override bool CanZoomToPanel => this.VisualizationObjects.Count > 0;

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(TimelineVisualizationPanelView));
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
                PlotVisualizationObject snapToVisualizationObject = this.Container.SnapToVisualizationObject as PlotVisualizationObject;
                if (snapToVisualizationObject != null)
                {
                    snappedTime = snapToVisualizationObject.GetTimeOfNearestMessage(time, snapToVisualizationObject.SummaryData?.Count ?? 0, (idx) => snapToVisualizationObject.SummaryData[idx].OriginatingTime);
                }

                return snappedTime ?? time;
            }

            Console.WriteLine("TimelineVisualizationPanel.GetTimeAtMousePointer() - Could not find the TimelineScroller in the tree");
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
        }
    }
}
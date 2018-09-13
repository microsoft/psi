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
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Controls;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;

    /// <summary>
    /// Represents a visualization panel that time based visualizers can be rendered in.
    /// </summary>
    [DataContract(Namespace = "http://www.microsoft.com/psi")]
    public class TimelineVisualizationPanel : VisualizationPanel<TimelineVisualizationPanelConfiguration>
    {
        private RelayCommand showHideLegendCommand;
        private RelayCommand zoomToSelectionCommand;
        private RelayCommand zoomToSessionExtentsCommand;
        private RelayCommand<MouseButtonEventArgs> mouseLeftButtonDownCommand;
        private RelayCommand<MouseButtonEventArgs> mouseRightButtonDownCommand;
        private Point lastMouseLeftButtonDownPoint = new Point(0, 0);

        /// <inheritdoc />
        [Browsable(false)]
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(TimelineVisualizationPanelView));

        /// <summary>
        /// Gets the Mouse Position the last time the user clicked in this panel
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
                if (this.zoomToSelectionCommand == null)
                {
                    this.zoomToSelectionCommand = new RelayCommand(() => this.Container.Navigator.ZoomToSelection());
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
                    this.zoomToSessionExtentsCommand = new RelayCommand(() => this.Container.Navigator.ZoomToDataRange());
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

        /// <inheritdoc />
        protected override void InitNew()
        {
            base.InitNew();
            this.Configuration.Name = "Timeline Panel";
            this.Configuration.Height = 70;
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
                return time;
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
    }
}
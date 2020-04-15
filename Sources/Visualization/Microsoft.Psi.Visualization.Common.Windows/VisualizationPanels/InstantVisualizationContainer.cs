// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Newtonsoft.Json;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Represents a visualization panel that can contain instant visualization panels.
    /// </summary>
    public class InstantVisualizationContainer : VisualizationPanel, IInstantVisualizationContainer
    {
        /// <summary>
        /// The maximum number of cells the container may contain.
        /// </summary>
        public const int MaxCells = 5;

        /// <summary>
        /// The default number of cells in the container.
        /// </summary>
        public const int DefaultCellCount = 1;

        private int cells;

        private ObservableCollection<VisualizationPanel> panels = new ObservableCollection<VisualizationPanel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantVisualizationContainer"/> class.
        /// </summary>
        /// <param name="initialCellCount">The number of placeholder cells the panel should initially contain.</param>
        [JsonConstructor]
        public InstantVisualizationContainer(int initialCellCount)
            : base()
        {
            this.Name = "Instant Panel";

            // Add the requested number of child placeholder panels
            for (int index = 0; index < initialCellCount; index++)
            {
                this.AddChildVisualizationPanel(typeof(InstantVisualizationPlaceholderPanel));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantVisualizationContainer"/> class.
        /// </summary>
        /// <param name="firstChildPanel">A visualization panel that should be contained in the first cell, or null if all cells should initially be placeholders.</param>
        public InstantVisualizationContainer(VisualizationPanel firstChildPanel)
            : this(DefaultCellCount)
        {
            if (firstChildPanel != null)
            {
                this.ReplaceChildVisualizationPanel(this.Panels[0], firstChildPanel);
            }
        }

        /// <summary>
        /// Gets or sets the number of rows in the panel.
        /// </summary>
        [DataMember]
        [PropertyOrder(2)]
        [Description("The number of cells in the visualization container.")]
        public int Cells
        {
            get
            {
                return this.cells;
            }

            set
            {
                if ((value > 0) && (value <= MaxCells))
                {
                    this.Set(nameof(this.Cells), ref this.cells, value);
                    this.UpdateChildPanels();
                }
            }
        }

        /// <inheritdoc/>
        [DataMember]
        [Browsable(false)]
        public ObservableCollection<VisualizationPanel> Panels
        {
            get { return this.panels; }
            private set { this.Set(nameof(this.Panels), ref this.panels, value); }
        }

        /// <summary>
        /// Replaces an instant visualization placeholder panel with an instant visualization panel.
        /// </summary>
        /// <param name="oldPanel">The visualization panel to replace.</param>
        /// <param name="newPanel">The visualization panel to replace it with.</param>
        public void ReplaceChildVisualizationPanel(VisualizationPanel oldPanel, VisualizationPanel newPanel)
        {
            if (oldPanel == null)
            {
                throw new ArgumentNullException(nameof(oldPanel));
            }

            if (newPanel == null)
            {
                throw new ArgumentNullException(nameof(newPanel));
            }

            // Make sure the placeholder panel being replaced is really a child of this container
            int placeholderPanelIndex = this.Panels.IndexOf(oldPanel);
            if (placeholderPanelIndex < 0)
            {
                throw new ArgumentException("placeholderPanel is not a member of the Panels collection");
            }

            // Disconnect the placeholder panel
            oldPanel.PropertyChanged -= this.ChildVisualizationPanel_PropertyChanged;
            oldPanel.SetParentContainer(null);

            // Wire up the new panel
            newPanel.SetParentContainer(this.Container);
            newPanel.ParentPanel = this;
            newPanel.Width = oldPanel.Width;
            newPanel.PropertyChanged += this.ChildVisualizationPanel_PropertyChanged;

            // Replace the panel
            this.Panels[placeholderPanelIndex] = newPanel;
            this.UpdateChildPanelMargins();
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            while (this.Panels.Count > 0)
            {
                this.RemoveChildPanel(this.Panels[0]);
            }
        }

        /// <inheritdoc/>
        public override void RemoveChildPanel(VisualizationPanel childPanel)
        {
            childPanel.PropertyChanged -= this.ChildVisualizationPanel_PropertyChanged;
            childPanel.Clear();
            this.Panels.Remove(childPanel);
            this.Cells = this.Panels.Count;
        }

        /// <inheritdoc/>
        public override void SetParentContainer(VisualizationContainer container)
        {
            base.SetParentContainer(container);
            foreach (VisualizationPanel panel in this.Panels)
            {
                panel.SetParentContainer(container);
                panel.ParentPanel = this;
            }
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate()
        {
            return XamlHelper.CreateTemplate(this.GetType(), typeof(InstantVisualizationContainerView));
        }

        private void UpdateChildPanels()
        {
            int cellCount = this.Cells;

            while (this.Panels.Count < cellCount)
            {
                this.AddChildVisualizationPanel(typeof(InstantVisualizationPlaceholderPanel));
            }

            while (this.Panels.Count > cellCount)
            {
                this.RemoveChildPanel(this.Panels[this.Panels.Count - 1]);
            }

            this.UpdateChildPanelMargins();
        }

        private void UpdateChildPanelMargins()
        {
            // The first child panel should have zero margins, subsequent panels
            // should have a 2 pixel left margin to ensure correct spacings.
            for (int index = 0; index < this.Panels.Count; index++)
            {
                if (index == 0)
                {
                    this.Panels[index].VisualMargin = new Thickness(0);
                }
                else
                {
                    this.Panels[index].VisualMargin = new Thickness(2.0d, 0.0d, 0.0d, 0.0d);
                }
            }
        }

        private void AddChildVisualizationPanel(Type visualizationPanelType)
        {
            VisualizationPanel panel = Activator.CreateInstance(visualizationPanelType) as VisualizationPanel;
            panel.SetParentContainer(this.Container);
            panel.ParentPanel = this;
            this.panels.Add(panel);
            panel.PropertyChanged += this.ChildVisualizationPanel_PropertyChanged;
            this.Cells = this.Panels.Count;
        }

        private void ChildVisualizationPanel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // If the child panel has no more visualizations in it, replace it with a placeholder panel
            if (e.PropertyName == nameof(VisualizationPanel.VisualizationObjects))
            {
                VisualizationPanel childPanel = sender as VisualizationPanel;
                if (childPanel.VisualizationObjects.Count <= 0)
                {
                    this.ReplaceChildVisualizationPanel(childPanel, new InstantVisualizationPlaceholderPanel());
                }
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            this.Cells = this.Panels.Count;
        }
    }
}
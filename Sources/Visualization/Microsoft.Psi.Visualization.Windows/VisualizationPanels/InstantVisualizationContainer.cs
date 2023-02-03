// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.ViewModels;
    using Microsoft.Psi.Visualization.Views;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Newtonsoft.Json;

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

        private ObservableCollection<VisualizationPanel> panels = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="InstantVisualizationContainer"/> class.
        /// </summary>
        /// <param name="initialCellCount">The number of placeholder cells the panel should initially contain.</param>
        [JsonConstructor]
        public InstantVisualizationContainer(int initialCellCount)
            : base()
        {
            this.Name = "Instant Visualization Container";

            // Add the requested number of child placeholder panels
            for (int index = 0; index < initialCellCount; index++)
            {
                this.IncreaseCellCount(0);
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
        /// Occurs when the relative width of a child visualization panel has changed.
        /// </summary>
        public event EventHandler ChildVisualizationPanelWidthChanged;

        /// <inheritdoc/>
        [DataMember]
        [Browsable(false)]
        public ObservableCollection<VisualizationPanel> Panels
        {
            get { return this.panels; }
            private set { this.Set(nameof(this.Panels), ref this.panels, value); }
        }

        /// <inheritdoc/>
        public override List<VisualizationPanelType> CompatiblePanelTypes => new ();

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
            => new ()
            {
                new ContextMenuItemInfo(IconSourcePath.InstantContainerAddCellLeft, $"Insert Cell to the Left", this.InsertCellCommand(true)),
                new ContextMenuItemInfo(IconSourcePath.InstantContainerAddCellRight, $"Insert Cell to the Right", this.InsertCellCommand(false)),
                new ContextMenuItemInfo(null, $"Remove Cell", this.CreateRemoveCellCommand(null)),
                new ContextMenuItemInfo(IconSourcePath.InstantContainerRemoveCell, $"Remove {this.Name}", this.RemovePanelCommand),
            };

        /// <summary>
        /// Inserts a cell in the visualization container view to the left or to the right of the current panel.
        /// </summary>
        /// <param name="insertOnLeft">True if the panel should be inserted to the left of panel, otherwise false.</param>
        /// <returns>The complete relay command.</returns>
        public PsiCommand InsertCellCommand(bool insertOnLeft)
        {
            var currentPanelIndex = this.Panels.IndexOf(this.Panels.First(p => p.IsCurrentPanel));
            return new (
                () => this.IncreaseCellCount(insertOnLeft ? currentPanelIndex : currentPanelIndex + 1),
                this.Panels.Count < MaxCells);
        }

        /// <summary>
        /// Gets the decrease cell count command.
        /// </summary>
        /// <param name="panel">The child panel to remove.</param>
        /// <returns>The complete relay command.</returns>
        public PsiCommand CreateRemoveCellCommand(VisualizationPanel panel) =>
            new (() => this.RemoveCell(panel), true);

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
            oldPanel.PropertyChanged -= this.OnChildVisualizationPanelPropertyChanged;
            oldPanel.SetParentContainer(null);

            // Wire up the new panel
            newPanel.SetParentContainer(this.Container);
            newPanel.SetParentPanel(this);
            newPanel.Width = oldPanel.Width;

            // Transfer default properties to the new panel
            if (newPanel is InstantVisualizationPanel newInstantPanel &&
                oldPanel is InstantVisualizationPanel oldInstantPanel)
            {
                newInstantPanel.DefaultCursorEpsilonNegMs = oldInstantPanel.DefaultCursorEpsilonNegMs;
                newInstantPanel.DefaultCursorEpsilonPosMs = oldInstantPanel.DefaultCursorEpsilonPosMs;
            }

            newPanel.PropertyChanged += this.OnChildVisualizationPanelPropertyChanged;

            // Replace the panel
            this.Panels[placeholderPanelIndex] = newPanel;
            this.UpdateChildPanelMargins();

            // If the current visualization panel is the one we just replaced,
            // then set it instead to the new panel we replaced it with.
            if (this.Container?.CurrentPanel == oldPanel)
            {
                newPanel.IsTreeNodeSelected = true;
            }
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            while (this.Panels.Count > 0)
            {
                this.RemoveCell(this.Panels[0]);
            }
        }

        /// <inheritdoc/>
        public override void SetParentContainer(VisualizationContainer container)
        {
            base.SetParentContainer(container);
            foreach (VisualizationPanel panel in this.Panels)
            {
                panel.SetParentContainer(container);
                panel.SetParentPanel(this);
            }
        }

        /// <inheritdoc/>
        public override List<IStreamVisualizationObject> GetDerivedStreamVisualizationObjects()
        {
            var streamMemberVisualizers = new List<IStreamVisualizationObject>();

            foreach (VisualizationPanel visualizationPanel in this.Panels)
            {
                streamMemberVisualizers.AddRange(visualizationPanel.GetDerivedStreamVisualizationObjects());
            }

            return streamMemberVisualizers;
        }

        /// <summary>
        /// Removes a cell specified by the panel it contains.
        /// </summary>
        /// <param name="panel">The panel whose cell should be removed.</param>
        public void RemoveCell(VisualizationPanel panel)
        {
            // Get the index of the panel to remove.
            int panelIndex = this.Panels.IndexOf(panel);

            // Unhook and remove the child panel
            panel.PropertyChanged -= this.OnChildVisualizationPanelPropertyChanged;
            panel.Clear();
            this.Panels.Remove(panel);
            this.UpdateChildPanelMargins();
        }

        /// <inheritdoc/>
        public override void UpdateStreamSources(SessionViewModel sessionViewModel)
        {
            foreach (var instantVisualizationPanel in this.Panels)
            {
                instantVisualizationPanel.UpdateStreamSources(sessionViewModel);
            }
        }

        /// <inheritdoc />
        protected override DataTemplate CreateDefaultViewTemplate() =>
            XamlHelper.CreateTemplate(this.GetType(), typeof(InstantVisualizationContainerView));

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

        private void IncreaseCellCount(int index)
        {
            // Increases the cell count by adding a new empty placeholder visualization panel at the specified index
            VisualizationPanel panel = new InstantVisualizationPlaceholderPanel(this);
            panel.SetParentContainer(this.Container);
            panel.SetParentPanel(this);
            this.panels.Insert(index, panel);
            panel.PropertyChanged += this.OnChildVisualizationPanelPropertyChanged;
            this.UpdateChildPanelMargins();
        }

        private void OnChildVisualizationPanelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstantVisualizationPlaceholderPanel.RelativeWidth) ||
                e.PropertyName == nameof(VisualizationPanel.Visible))
            {
                this.ChildVisualizationPanelWidthChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            foreach (var panel in this.panels)
            {
                panel.PropertyChanged += this.OnChildVisualizationPanelPropertyChanged;
            }

            this.UpdateChildPanelMargins();
        }
    }
}
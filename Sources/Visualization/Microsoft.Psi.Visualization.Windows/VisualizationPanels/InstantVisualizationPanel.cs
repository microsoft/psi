// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Windows;
    using GalaSoft.MvvmLight.CommandWpf;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Represents the base class that instant visualization panels derive from.
    /// </summary>
    public abstract class InstantVisualizationPanel : VisualizationPanel
    {
        private int relativeWidth = 100;
        private int defaultCursorEpsilonPosMs = 0;
        private int defaultCursorEpsilonNegMs = 500;

        /// <summary>
        /// Gets or sets the default cursor epsilon for the panel.
        /// </summary>
        [DataMember]
        [DisplayName("Default Cursor Epsilon Past (ms)")]
        [Description("The default past cursor epsilon for the panel.")]
        public int DefaultCursorEpsilonNegMs
        {
            get { return this.defaultCursorEpsilonNegMs; }
            set { this.Set(nameof(this.DefaultCursorEpsilonNegMs), ref this.defaultCursorEpsilonNegMs, value); }
        }

        /// <summary>
        /// Gets or sets the default cursor epsilon for the panel.
        /// </summary>
        [DataMember]
        [DisplayName("Default Cursor Epsilon Future (ms)")]
        [Description("The default future cursor epsilon for the panel.")]
        public int DefaultCursorEpsilonPosMs
        {
            get { return this.defaultCursorEpsilonPosMs; }
            set { this.Set(nameof(this.DefaultCursorEpsilonPosMs), ref this.defaultCursorEpsilonPosMs, value); }
        }

        /// <summary>
        /// Gets or sets the name of the relative width for the panel.
        /// </summary>
        [DataMember]
        [DisplayName("Relative Width")]
        [Description("The relative width for the panel.")]
        public int RelativeWidth
        {
            get { return this.relativeWidth; }
            set { this.Set(nameof(this.RelativeWidth), ref this.relativeWidth, value); }
        }

        /// <inheritdoc/>
        public override List<ContextMenuItemInfo> ContextMenuItemsInfo()
        {
            var items = new List<ContextMenuItemInfo>();

            // Add Set Cursor Epsilon menu with sub-menu items
            var setCursorEpsilonItems = new ContextMenuItemInfo("Set Default Cursor Epsilon");

            setCursorEpsilonItems.SubItems.Add(
                new ContextMenuItemInfo(
                    null,
                    "Infinite Past",
                    new RelayCommand(() => this.UpdateDefaultCursorEpsilon("Infinite Past", int.MaxValue, 0), true)));
            setCursorEpsilonItems.SubItems.Add(
                new ContextMenuItemInfo(
                    null,
                    "Last 5 seconds",
                    new RelayCommand(() => this.UpdateDefaultCursorEpsilon("Last 5 seconds", 5000, 0), true)));
            setCursorEpsilonItems.SubItems.Add(
                new ContextMenuItemInfo(
                    null,
                    "Last 1 second",
                    new RelayCommand(() => this.UpdateDefaultCursorEpsilon("Last 1 second", 1000, 0), true)));
            setCursorEpsilonItems.SubItems.Add(
                new ContextMenuItemInfo(
                    null,
                    "Last 50 milliseconds",
                    new RelayCommand(() => this.UpdateDefaultCursorEpsilon("Last 50 milliseconds", 50, 0), true)));

            items.Add(setCursorEpsilonItems);

            items.AddRange(base.ContextMenuItemsInfo());
            return items;
        }

        /// <inheritdoc />
        public override void AddVisualizationObject(VisualizationObject visualizationObject)
        {
            base.AddVisualizationObject(visualizationObject);

            visualizationObject.CursorEpsilonNegMs = this.defaultCursorEpsilonNegMs;
            visualizationObject.CursorEpsilonPosMs = this.defaultCursorEpsilonPosMs;
        }

        private void UpdateDefaultCursorEpsilon(string name, int negMs, int posMs)
        {
            this.DefaultCursorEpsilonNegMs = negMs;
            this.DefaultCursorEpsilonPosMs = posMs;

            var anyVisualizersWithDifferentCursorEpsilon =
                this.VisualizationObjects.Any(vo => vo.CursorEpsilonNegMs != 50 || vo.CursorEpsilonPosMs != 0);

            if (anyVisualizersWithDifferentCursorEpsilon)
            {
                var result = new MessageBoxWindow(
                    Application.Current.MainWindow,
                    "Update visualizers?",
                    $"Some of the visualizers in this panel have a cursor epsilon that is different from {name}. Would you also like to change the cursor epsilon for these visualizers to {name}?",
                    "Yes",
                    "No").ShowDialog();
                if (result == true)
                {
                    foreach (var visualizationObject in this.VisualizationObjects)
                    {
                        visualizationObject.CursorEpsilonNegMs = negMs;
                        visualizationObject.CursorEpsilonPosMs = posMs;
                    }
                }
            }
        }
    }
}
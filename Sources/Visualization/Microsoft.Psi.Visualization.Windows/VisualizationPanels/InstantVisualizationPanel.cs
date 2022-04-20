// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationPanels
{
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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

        /// <inheritdoc />
        public override void AddVisualizationObject(VisualizationObject visualizationObject)
        {
            base.AddVisualizationObject(visualizationObject);

            visualizationObject.CursorEpsilonNegMs = this.defaultCursorEpsilonNegMs;
            visualizationObject.CursorEpsilonPosMs = this.defaultCursorEpsilonPosMs;
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Psi.Diagnostics;
    using Microsoft.Psi.Serialization;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for DiagnosticsVisualizationObjectView.xaml.
    /// </summary>
    public partial class PipelineDiagnosticsVisualizationModel
    {
        private Stack<int> navStack = new Stack<int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineDiagnosticsVisualizationModel"/> class.
        /// </summary>
        public PipelineDiagnosticsVisualizationModel()
        {
            RegisterKnownSerializationTypes();
            this.Reset();
        }

        /// <summary>
        /// Gets or sets visualization object.
        /// </summary>
        public PipelineDiagnosticsVisualizationObject VisualizationObject { get; set; }

        /// <summary>
        /// Gets or sets diagnostics graph.
        /// </summary>
        public PipelineDiagnostics Graph { get; set; }

        /// <summary>
        /// Gets navigation stack of graphs/subgraphs.
        /// </summary>
        public Stack<int> NavStack => this.navStack;

        /// <summary>
        /// Gets or sets the ID of the selected graph edge.
        /// </summary>
        public int SelectedEdgeId { get; set; }

        /// <summary>
        /// Gets or sets details of selected edge.
        /// </summary>
        public string SelectedEdgeDetails { get; set; }

        /// <summary>
        /// Register known types for serialization.
        /// </summary>
        public static void RegisterKnownSerializationTypes()
        {
            KnownSerializers.Default.Register<Queue<TimeSpan>>("System.Collections.Generic.Queue`1[[System.TimeSpan, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Collections, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            KnownSerializers.Default.Register<Queue<int>>("System.Collections.Generic.Queue`1[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Collections, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            KnownSerializers.Default.Register<Dictionary<int, PipelineDiagnostics>>("System.Collections.Generic.Dictionary`2[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Microsoft.Psi.Diagnostics.PipelineDiagnostics, Microsoft.Psi, Version=0.7.57.2, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            KnownSerializers.Default.Register<Dictionary<int, PipelineDiagnostics.PipelineElementDiagnostics>>("System.Collections.Generic.Dictionary`2[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Microsoft.Psi.Diagnostics.PipelineDiagnostics+PipelineElementDiagnostics, Microsoft.Psi, Version=0.7.57.2, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            KnownSerializers.Default.Register<Dictionary<int, PipelineDiagnostics.ReceiverDiagnostics>>("System.Collections.Generic.Dictionary`2[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Microsoft.Psi.Diagnostics.PipelineDiagnostics+ReceiverDiagnostics, Microsoft.Psi, Version=0.7.57.2, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            KnownSerializers.Default.Register<Dictionary<int, PipelineDiagnostics.EmitterDiagnostics>>("System.Collections.Generic.Dictionary`2[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[Microsoft.Psi.Diagnostics.PipelineDiagnostics+EmitterDiagnostics, Microsoft.Psi, Version=0.7.57.2, Culture=neutral, PublicKeyToken=null]], System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
            KnownSerializers.Default.Register(TypeResolutionHelper.GetVerifiedType("System.Collections.Generic.GenericEqualityComparer`1[System.Int32]"), "System.Collections.Generic.GenericEqualityComparer`1[[System.Int32, System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e");
        }

        /// <summary>
        /// Reset model state.
        /// </summary>
        public void Reset()
        {
            this.Graph = null;
            this.navStack.Clear();
            this.SelectedEdgeId = -1;
            this.SelectedEdgeDetails = string.Empty;
        }
    }
}

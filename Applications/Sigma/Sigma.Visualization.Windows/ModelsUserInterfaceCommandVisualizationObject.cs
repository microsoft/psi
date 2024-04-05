// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Sigma.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Implements a visualization object that can display the Sigma app holograms.
    /// </summary>
    [VisualizationObject("Sigma Holograms")]
    public class ModelsUserInterfaceCommandVisualizationObject : ModelVisual3DValueVisualizationObject<Dictionary<string, ModelUserInterfaceCommand>>
    {
        private readonly Dictionary<string, PosedModelFromFileVisualizationObject> allModelVisualizers = new ();
        private readonly Dictionary<string, string> allModelPaths = new ();
        private string modelPathsFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelsUserInterfaceCommandVisualizationObject"/> class.
        /// </summary>
        public ModelsUserInterfaceCommandVisualizationObject()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating which file to load for getting model file paths.
        /// </summary>
        [DataMember]
        [Description("The file containing all model file paths by name.")]
        public string ModelPathsFile
        {
            get { return this.modelPathsFile; }
            set { this.Set(nameof(this.ModelPathsFile), ref this.modelPathsFile, value); }
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.ModelPathsFile))
            {
                this.LoadModels();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        /// <inheritdoc/>
        public override void UpdateVisual3D()
        {
            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        private void UpdateVisuals()
        {
            // Hide visualizers for any models that are not in the current data.
            foreach (var kvp in this.allModelVisualizers)
            {
                if (this.CurrentData == null || !this.CurrentData.ContainsKey(kvp.Key))
                {
                    kvp.Value.Visible = false;
                }
            }

            // Add or update visualizers for any models that are in the current data.
            if (this.CurrentData != null)
            {
                foreach (var kvp in this.CurrentData)
                {
                    var modelName = kvp.Key;
                    var modelSpecs = kvp.Value;

                    if (!this.allModelVisualizers.ContainsKey(modelName))
                    {
                        // Add a new visualizer for this model, and set its model file path if known.
                        this.allModelVisualizers.Add(modelName, new PosedModelFromFileVisualizationObject());
                        if (this.allModelPaths.ContainsKey(modelName))
                        {
                            this.allModelVisualizers[modelName].ModelFile = this.allModelPaths[modelName];
                        }
                    }

                    // Update pose and visibility.
                    this.allModelVisualizers[modelName].SetCurrentValue(this.SynthesizeMessage(modelSpecs.Pose));
                    this.allModelVisualizers[modelName].Visible = modelSpecs.Visible && this.allModelPaths.ContainsKey(modelName);
                }
            }
        }

        private void LoadModels()
        {
            this.allModelPaths.Clear();
            try
            {
                // Parse the file for model file paths.
                string[] lines = File.ReadAllLines(this.modelPathsFile);
                for (int i = 0; i < lines.Length; i++)
                {
                    string[] values = lines[i].Split('\t');
                    if (values.Length == 2)
                    {
                        this.allModelPaths.Add(values[0], values[1]);
                        if (this.allModelVisualizers.ContainsKey(values[0]))
                        {
                            this.allModelVisualizers[values[0]].ModelFile = values[1];
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Display an error message to the user.
                    new MessageBoxWindow(Application.Current.MainWindow, "Error Loading Models", e.Message, "Close", null).ShowDialog();
                }));

                throw;
            }

            this.UpdateVisuals();
        }

        private void UpdateVisibility()
        {
            foreach (var modelVisualizer in this.allModelVisualizers.Values)
            {
                this.UpdateChildVisibility(modelVisualizer.Visual3D, this.Visible && modelVisualizer.Visible);
            }
        }
    }
}
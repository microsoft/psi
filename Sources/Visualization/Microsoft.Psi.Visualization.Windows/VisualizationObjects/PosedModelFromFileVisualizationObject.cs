// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Media3D;
    using HelixToolkit.Wpf;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Visualization.Extensions;
    using Microsoft.Psi.Visualization.Windows;
    using Vector3D = System.Windows.Media.Media3D.Vector3D;

    /// <summary>
    /// Implements a visualization object for rendering a 3D model at a <see cref="CoordinateSystem"/> pose.
    /// </summary>
    /// <remarks>
    /// The desired model geometry is loaded from a file specified in the <see cref="ModelFile"/> property.
    /// Supported file formats include .obj, .stl, .3ds, .lwo, .objz, .off, and .ply.
    /// </remarks>
    [VisualizationObject("Posed Model (from file)")]
    public class PosedModelFromFileVisualizationObject : ModelVisual3DVisualizationObject<CoordinateSystem>
    {
        private readonly ModelImporter modelImporter = new ();
        private readonly SolidColorBrush materialBrush = new ();

        private ModelVisual3D modelVisual;
        private string modelFile;
        private Color color = Colors.White;
        private double opacity = 100;
        private double scale = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="PosedModelFromFileVisualizationObject"/> class.
        /// </summary>
        public PosedModelFromFileVisualizationObject()
        {
            this.modelImporter.DefaultMaterial = new DiffuseMaterial(this.materialBrush);
            this.UpdateColor();
            this.UpdateOpacity();
            this.ResetModel();
        }

        /// <summary>
        /// Gets or sets a value indicating which file to load for the model mesh.
        /// </summary>
        [DataMember]
        [Description("The model file to load (full path).")]
        public string ModelFile
        {
            get { return this.modelFile; }
            set { this.Set(nameof(this.ModelFile), ref this.modelFile, value); }
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        [DataMember]
        [Description("The color of the model.")]
        public Color Color
        {
            get { return this.color; }
            set { this.Set(nameof(this.Color), ref this.color, value); }
        }

        /// <summary>
        /// Gets or sets the opacity.
        /// </summary>
        [DataMember]
        [Description("The opacity of the model.")]
        public double Opacity
        {
            get { return this.opacity; }
            set { this.Set(nameof(this.Opacity), ref this.opacity, value); }
        }

        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        [DataMember]
        [Description("The scale of the model.")]
        public double Scale
        {
            get { return this.scale; }
            set { this.Set(nameof(this.Scale), ref this.scale, value); }
        }

        /// <inheritdoc/>
        public override void NotifyPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.ModelFile))
            {
                this.LoadModel();
            }
            else if (propertyName == nameof(this.Color))
            {
                this.UpdateColor();
            }
            else if (propertyName == nameof(this.Opacity))
            {
                this.UpdateOpacity();
            }
            else if (propertyName == nameof(this.Scale))
            {
                this.UpdateVisuals();
            }
            else if (propertyName == nameof(this.Visible))
            {
                this.UpdateVisibility();
            }
        }

        /// <inheritdoc/>
        public override void UpdateData()
        {
            this.UpdateVisuals();
            this.UpdateVisibility();
        }

        private void UpdateVisuals()
        {
            if (this.CurrentData != null)
            {
                var currentPose = this.CurrentData.GetMatrix3D();
                currentPose.ScalePrepend(new Vector3D(this.scale, this.scale, this.scale));
                this.modelVisual.Transform = new MatrixTransform3D(currentPose);
            }
        }

        private void UpdateVisibility()
        {
            this.UpdateChildVisibility(this.modelVisual, this.Visible && this.CurrentData is not null);
        }

        private void LoadModel()
        {
            try
            {
                this.modelVisual.Content = this.modelImporter.Load(this.modelFile);
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Display an error message to the user.
                    new MessageBoxWindow(Application.Current.MainWindow, "Error Loading Model", e.Message, "Close", null).ShowDialog();
                }));

                this.ResetModel();
                throw;
            }

            this.UpdateVisuals();
        }

        private void ResetModel()
        {
            if (this.ModelView.Children.Contains(this.modelVisual))
            {
                this.ModelView.Children.Remove(this.modelVisual);
            }

            this.modelVisual = new SphereVisual3D();
            this.UpdateVisibility();
        }

        private void UpdateOpacity()
        {
            this.materialBrush.Opacity = Math.Max(0, Math.Min(1.0, this.opacity / 100.0));
        }

        private void UpdateColor()
        {
            this.materialBrush.Color = this.color;
        }
    }
}
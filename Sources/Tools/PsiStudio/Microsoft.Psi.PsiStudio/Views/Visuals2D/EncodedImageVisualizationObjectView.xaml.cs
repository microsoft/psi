// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Visualization.Common;
    using Microsoft.Psi.Visualization.VisualizationObjects;

    /// <summary>
    /// Interaction logic for EncodedImageVisualizationObjectView.xaml
    /// </summary>
    public partial class EncodedImageVisualizationObjectView : UserControl
    {
        private int lastKnownImageWidth = 100;
        private int lastKnownImageHeight = 100;
        private Imaging.PixelFormat lastKnownPixelFormat = Imaging.PixelFormat.BGR_24bpp;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImageVisualizationObjectView"/> class.
        /// </summary>
        public EncodedImageVisualizationObjectView()
        {
            this.InitializeComponent();
            this.DisplayImage = new DisplayImage();
            this.DataContextChanged += this.EncodedImageVisualizationObjectView_DataContextChanged;
            this.Unloaded += this.EncodedImageVisualizationObjectView_Unloaded;
        }

        /// <summary>
        /// Gets display image.
        /// </summary>
        public DisplayImage DisplayImage { get; private set; }

        /// <summary>
        /// Gets encoded image visualization object.
        /// </summary>
        public EncodedImageVisualizationObject EncodedImageVisualizationObject { get; private set; }

        private void EncodedImageVisualizationObjectView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.EncodedImageVisualizationObject = (EncodedImageVisualizationObject)this.DataContext;
            this.EncodedImageVisualizationObject.PropertyChanged += this.EncodedImageVisualizationObject_PropertyChanged;
        }

        private void EncodedImageVisualizationObjectView_Unloaded(object sender, RoutedEventArgs e)
        {
            this.EncodedImageVisualizationObject.PropertyChanged -= this.EncodedImageVisualizationObject_PropertyChanged;
        }

        private void EncodedImageVisualizationObject_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.EncodedImageVisualizationObject.CurrentValue))
            {
                Imaging.Image psiImage = null;
                if (this.EncodedImageVisualizationObject.CurrentValue.HasValue)
                {
                    var resource = this.EncodedImageVisualizationObject.CurrentValue.Value.Data.Resource;
                    this.lastKnownImageWidth = resource.Width;
                    this.lastKnownImageHeight = resource.Height;
                    this.lastKnownPixelFormat = resource.GetPixelFormat();
                    psiImage = new Imaging.Image(this.lastKnownImageWidth, this.lastKnownImageHeight, this.lastKnownPixelFormat);
                    resource.DecodeTo(psiImage);
                }
                else
                {
                    psiImage = new Imaging.Image(this.lastKnownImageWidth, this.lastKnownImageHeight, this.lastKnownPixelFormat);
                }

                if (this.EncodedImageVisualizationObject.Configuration.HorizontalFlip)
                {
                    if (psiImage != null)
                    {
                        var bitmap = psiImage.ToManagedImage(true);
                        bitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX);
                        this.DisplayImage.UpdateImage(Imaging.Image.FromManagedImage(bitmap));
                    }
                    else
                    {
                        this.DisplayImage.UpdateImage((Imaging.Image)null);
                    }
                }
                else
                {
                    this.DisplayImage.UpdateImage(psiImage);
                }
            }
        }
    }
}

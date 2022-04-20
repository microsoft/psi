// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Spatial.Euclidean;
    using StereoKit;

    /// <summary>
    /// Component that visually renders an encoded image on a 3D rectangle.
    /// </summary>
    public class EncodedImageRectangle3DStereoKitRenderer : Rectangle3DStereoKitRenderer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImageRectangle3DStereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public EncodedImageRectangle3DStereoKitRenderer(Pipeline pipeline, bool visible = true, string name = nameof(EncodedImageRectangle3DStereoKitRenderer))
            : base(pipeline, System.Drawing.Color.White, false, visible, name)
        {
            this.Image = pipeline.CreateReceiver<Shared<EncodedImage>>(this, this.UpdateImage, nameof(this.Image));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedImageRectangle3DStereoKitRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="rectangle3D">Rectangle to render.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public EncodedImageRectangle3DStereoKitRenderer(Pipeline pipeline, Rectangle3D rectangle3D, bool visible = true, string name = nameof(EncodedImageRectangle3DStereoKitRenderer))
            : base(pipeline, rectangle3D, System.Drawing.Color.White, false, visible, name)
        {
            this.Image = pipeline.CreateReceiver<Shared<EncodedImage>>(this, this.UpdateImage, nameof(this.Image));
        }

        /// <summary>
        /// Gets the receiver for encoded images to map onto the rectangle surface.
        /// </summary>
        public Receiver<Shared<EncodedImage>> Image { get; private set; }

        /// <summary>
        /// Gets the receiver for the rectangle to draw the image on.
        /// </summary>
        public Receiver<Rectangle3D> Rectangle => this.In;

        private void UpdateImage(Shared<EncodedImage> image)
        {
            this.Material[MatParamName.DiffuseTex] = Tex.FromMemory(image.Resource.GetBuffer());
        }
    }
}

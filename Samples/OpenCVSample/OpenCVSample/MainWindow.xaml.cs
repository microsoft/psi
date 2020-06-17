// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning disable SA1402 // SA1402MustContainSingleType

namespace Microsoft.Psi.Samples.OpenCV
{
    using System;
    using System.Windows;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Media;

    /// <summary>
    /// OpenCV operations.
    /// </summary>
    public static class OpenCV
    {
        /// <summary>
        /// Helper to wrap a Psi Image into an ImageBuffer suitable for passing to our C++ interop layer.
        /// </summary>
        /// <param name="source">Image to wrap.</param>
        /// <returns>A Psi image wrapped as an ImageBuffer.</returns>
        public static ImageBuffer ToImageBuffer(this Shared<Image> source)
        {
            return new ImageBuffer(source.Resource.Width, source.Resource.Height, source.Resource.ImageData, source.Resource.Stride);
        }

        /// <summary>
        /// Here we define an Psi extension. This extension will take a stream of images (source)
        /// and create a new stream of converted images.
        /// </summary>
        /// <param name="source">Our source producer (source stream of image samples).</param>
        /// <param name="deliveryPolicy">Our delivery policy (null means use the default).</param>
        /// <returns>The new stream of converted images.</returns>
        public static IProducer<Shared<Image>> ToGrayViaOpenCV(this IProducer<Shared<Image>> source, DeliveryPolicy<Shared<Image>> deliveryPolicy = null)
        {
            // Process informs the pipeline that we want to call our lambda ("(srcImage, env, e) =>{...}") with each image
            // from the stream.
            return source.Process<Shared<Image>, Shared<Image>>(
                (srcImage, env, e) =>
                {
                    // Our lambda here is called with each image sample from our stream and calls OpenCV to convert
                    // the image into a grayscale image. We then post the resulting gray scale image to our event queue
                    // so that the Psi pipeline will send it to the next component.

                    // Have Psi allocate a new image. We will convert the current image ('srcImage') into this new image.
                    using (var destImage = ImagePool.GetOrCreate(srcImage.Resource.Width, srcImage.Resource.Height, PixelFormat.Gray_8bpp))
                    {
                        // Call into our OpenCV wrapper to convert the source image ('srcImage') into the newly created image ('destImage')
                        // Note: since srcImage & destImage are Shared<> we need to access the Microsoft.Psi.Imaging.Image data via the Resource member
                        OpenCVMethods.ToGray(srcImage.ToImageBuffer(), destImage.ToImageBuffer());
                        e.Post(destImage, env.OriginatingTime);
                    }
                }, deliveryPolicy);
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Define our Psi Pipeline object
        private Pipeline pipeline;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            this.DispImage = new DisplayImage();
            this.DataContext = this;
            this.DoConvert = true;
            this.Closing += this.MainWindow_Closing;

            // Setup our Psi pipeline
            this.SetupPsi();
        }

        /// <summary>
        /// Gets or sets DisplayImage so that WPF can access it.
        /// </summary>
        public DisplayImage DispImage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to convert.
        /// </summary>
        public bool DoConvert { get; set; }

        /// <summary>
        /// SetupPsi() is called at application startup. It is responsible for
        /// building and starting the Psi pipeline.
        /// </summary>
        public void SetupPsi()
        {
            // First create the pipeline object.
            this.pipeline = Pipeline.Create();

            // Next register an event handler to catch pipeline errors
            this.pipeline.PipelineExceptionNotHandled += this.Pipeline_PipelineException;

            // Create our webcam
            MediaCapture webcam = new MediaCapture(this.pipeline, 1280, 720, 30);

            // Bind the webcam's output to our display image.
            // The "Do" operator is executed on each sample from the stream (webcam.Out), which are the images coming from the webcam
            webcam.Out.Where((img, e) => { return this.DoConvert; }).ToGrayViaOpenCV().Do(
                (img, e) =>
                {
                    this.DispImage.UpdateImage(img);
                });
            webcam.Out.Where((img, e) => { return !this.DoConvert; }).Do(
                (img, e) =>
                {
                    this.DispImage.UpdateImage(img);
                });

            // Finally start the pipeline running
            try
            {
                this.pipeline.RunAsync();
            }
            catch (AggregateException exp)
            {
                MessageBox.Show("Error! " + exp.InnerException.Message);
            }
        }

        /// <summary>
        /// <see cref="Pipeline_PipelineException"/> is called to handle exceptions thrown during pipeline execution.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Pipeline exception event arguments. Primarily used to get any pipeline errors back.</param>
        private void Pipeline_PipelineException(object sender, PipelineExceptionNotHandledEventArgs e)
        {
            MessageBox.Show("Error! " + e.Exception.Message);
        }

        /// <summary>
        /// Button_Click is the callback from WPF for handling when the user clicks the "ToRGB"/"ToGray" button.
        /// </summary>
        /// <param name="sender">Object that sent this event.</param>
        /// <param name="e">Event arguments (unused).</param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DoConvert = !this.DoConvert;
            this.ConvertButton.Content = this.DoConvert ? "ToRGB" : "ToGray";
        }

        /// <summary>
        /// Called when main window is closed.
        /// </summary>
        /// <param name="sender">window that we are closing.</param>
        /// <param name="e">args for closing event.</param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Dispose of the pipeline to shut it down and exit clean
            this.pipeline?.Dispose();
            this.pipeline = null;
        }
    }
}

#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning restore SA1402 // SA1402MustContainSingleType

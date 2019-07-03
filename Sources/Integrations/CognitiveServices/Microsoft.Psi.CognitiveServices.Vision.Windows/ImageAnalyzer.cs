// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Vision
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
    using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Component that performs image analysis via <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>.
    /// </summary>
    /// <remarks>A <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a>
    /// subscription key is required to use this component. For more information, see the full direct API for.
    /// <a href="https://azure.microsoft.com/en-us/services/cognitive-services/computer-vision/">Microsoft Cognitive Services Vision API</a></remarks>
    public sealed class ImageAnalyzer
    {
        private readonly ComputerVisionClient computerVisionClient;
        private readonly ImageAnalyzerConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageAnalyzer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The image analyzer configuration.</param>
        public ImageAnalyzer(Pipeline pipeline, ImageAnalyzerConfiguration configuration = null)
        {
            this.configuration = configuration ?? new ImageAnalyzerConfiguration();
            this.Out = pipeline.CreateEmitter<ImageAnalysis>(this, nameof(this.Out));
            this.In = pipeline.CreateAsyncReceiver<Shared<Image>>(this, this.ReceiveAsync, nameof(this.In));

            this.computerVisionClient = this.CreateClient();
        }

        /// <summary>
        /// Gets the input stream of images.
        /// </summary>
        public Receiver<Shared<Image>> In { get; }

        /// <summary>
        /// Gets the output stream of analysis results.
        /// </summary>
        public Emitter<ImageAnalysis> Out { get; }

        #region Static methods for parsing results to strings

        /// <summary>
        /// Parse Analysis Result into a string.
        /// </summary>
        /// <param name="result">Analysis Result.</param>
        /// <returns>Returns a string corresponding to the analysis result.</returns>
        internal static string AnalysisResultToString(ImageAnalysis result)
        {
            string resultString = string.Empty;

            if (result == null)
            {
                return resultString;
            }

            if (result.Metadata != null)
            {
                resultString = $"Image Format : {result.Metadata.Format}\nImage Dimensions : {result.Metadata.Width} x {result.Metadata.Height}\n";
            }

            resultString = $"{resultString}{DescriptionToString(result.Description)}{FacesToString(result.Faces)}{TagsToString(result.Tags)}{CategoriesToString(result.Categories)}" +
                $"{ColorToString(result.Color)}{AdultToString(result.Adult)}{ImageTypeToString(result.ImageType)}";

            return resultString;
        }

        private static string FacesToString(IList<FaceDescription> faces)
        {
            string resultString = string.Empty;

            if (faces != null && faces.Count > 0)
            {
                resultString = "Faces : \n";
                foreach (var face in faces)
                {
                    resultString = $"{resultString}\tAge : {face.Age}; Gender : {face.Gender}\n";
                }
            }

            return resultString;
        }

        private static string TagsToString(IList<ImageTag> tags)
        {
            string resultString = string.Empty;

            if (tags != null)
            {
                resultString = "Tags : \n";
                foreach (var tag in tags)
                {
                    resultString = $"{resultString}\tName : {tag.Name}; Confidence : {tag.Confidence}; Hint : {tag.Hint}\n";
                }
            }

            return resultString;
        }

        private static string CategoriesToString(IList<Category> categories)
        {
            string resultString = string.Empty;

            if (categories != null && categories.Count > 0)
            {
                resultString = $"Categories : \n";
                foreach (var category in categories)
                {
                    resultString = $"{resultString}\tName : {category.Name}; Score : {category.Score}\n";
                }
            }

            return resultString;
        }

        private static string ImageTypeToString(ImageType imageType)
        {
            string resultString = string.Empty;

            if (imageType != null)
            {
                string clipArtType;
                switch (imageType.ClipArtType)
                {
                    case 0:
                        clipArtType = "0 Non-clipart";
                        break;
                    case 1:
                        clipArtType = "1 ambiguous";
                        break;
                    case 2:
                        clipArtType = "2 normal-clipart";
                        break;
                    case 3:
                        clipArtType = "3 good-clipart";
                        break;
                    default:
                        clipArtType = "Unknown";
                        break;
                }

                resultString = $"Clip Art Type : {clipArtType}\n";

                string lineDrawingType;
                switch (imageType.LineDrawingType)
                {
                    case 0:
                        lineDrawingType = "0 Non-LineDrawing";
                        break;
                    case 1:
                        lineDrawingType = "1 LineDrawing";
                        break;
                    default:
                        lineDrawingType = "Unknown";
                        break;
                }

                resultString = $"{resultString}Line Drawing Type : {lineDrawingType}\n";
            }

            return resultString;
        }

        private static string AdultToString(AdultInfo adult)
        {
            string resultString = string.Empty;

            if (adult != null)
            {
                resultString = $"Is Adult Content : {adult.IsAdultContent}\nAdult Score : {adult.AdultScore}\n" +
                    $"Is Racy Content : {adult.IsRacyContent}\nRacy Score : {adult.RacyScore}\n";
            }

            return resultString;
        }

        private static string DescriptionToString(ImageDescriptionDetails description)
        {
            string resultString = string.Empty;

            if (description != null)
            {
                resultString = "Description : \n";
                foreach (var caption in description.Captions)
                {
                    resultString = $"{resultString}\tCaption : {caption.Text}; Confidence : {caption.Confidence}\n";
                }

                string tags = "\tTags : ";
                foreach (var tag in description.Tags)
                {
                    tags += tag + ", ";
                }

                resultString = $"{resultString}{tags}\n";
            }

            return resultString;
        }

        private static string ColorToString(ColorInfo color)
        {
            string resultString = string.Empty;

            if (color != null)
            {
                resultString = $"AccentColor : {color.AccentColor}\n";
                resultString = $"{resultString}Dominant Color Background : {color.DominantColorBackground}\n";
                resultString = $"{resultString}Dominant Color Foreground : {color.DominantColorForeground}\n";

                if (color.DominantColors != null && color.DominantColors.Count > 0)
                {
                    string colors = "Dominant Colors : ";
                    foreach (var c in color.DominantColors)
                    {
                        colors += c + " ";
                    }

                    resultString = $"{resultString}{colors}\n";
                }
            }

            return resultString;
        }

        #endregion

        private async Task ReceiveAsync(Shared<Image> data, Envelope e)
        {
            var analysisResult = default(ImageAnalysis);

            if (data != null)
            {
                using (Stream imageFileStream = new MemoryStream())
                {
                    // convert image to a stream and send to service
                    data.Resource.ToManagedImage(false).Save(imageFileStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    imageFileStream.Seek(0, SeekOrigin.Begin);
                    try
                    {
                        analysisResult = await this.computerVisionClient?.AnalyzeImageInStreamAsync(imageFileStream, this.configuration.VisualFeatures);
                    }
                    catch
                    {
                        // automatically swallow exceptions
                    }
                }
            }

            this.Out.Post(analysisResult, e.OriginatingTime);
        }

        private ComputerVisionClient CreateClient()
        {
            return new ComputerVisionClient(
                new ApiKeyServiceClientCredentials(this.configuration.SubscriptionKey),
                new System.Net.Http.DelegatingHandler[] { })
            {
                Endpoint = "https://westus.api.cognitive.microsoft.com/",
            };
        }
    }
}
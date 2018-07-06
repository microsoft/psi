// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // File name must match first type name.
#pragma warning disable SA1402 // File may only contain a single class.

namespace Microsoft.Psi.Visualization.Client
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media.Media3D;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Serialization;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.AnnotatedEventVisualizationObject />.
    /// </summary>
    public sealed class AnnotatedEventVisualizationObject : StreamVisualizationObject<AnnotatedEvent, AnnotatedEventVisualizationObjectConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.AnnotatedEventVisualizationObject";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.AudioVisualizationObject />.
    /// </summary>
    public sealed class AudioVisualizationObject : StreamVisualizationObject<double, AudioVisualizationObjectConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.AudioVisualizationObject";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.EncodedImageVisualizationObject />.
    /// </summary>
    public sealed class EncodedImageVisualizationObject : StreamVisualizationObject<Shared<EncodedImage>, ImageVisualizationObjectBaseConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.EncodedImageVisualizationObject";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.ImageVisualizationObject />.
    /// </summary>
    public sealed class ImageVisualizationObject : StreamVisualizationObject<Shared<Image>, ImageVisualizationObjectBaseConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.ImageVisualizationObject";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.KinectBodies3DVisualizationObject />.
    /// </summary>
    public sealed class KinectBodies3DVisualizationObject : StreamVisualizationObject<List<KinectBody>, KinectBodies3DVisualizationObjectConfiguration>
    {
        private IContractResolver contractResolver = new Instant3DVisualizationObjectContractResolver();

        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.KinectBodies3DVisualizationObject";

        /// <inheritdoc />
        protected override IContractResolver ContractResolver => this.contractResolver;
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.PlotVisualizationObject />.
    /// </summary>
    public sealed class PlotVisualizationObject : StreamVisualizationObject<double, PlotVisualizationObjectConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.PlotVisualizationObject";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.Points3DVisualizationObject />.
    /// </summary>
    public sealed class Points3DVisualizationObject : StreamVisualizationObject<List<System.Windows.Media.Media3D.Point3D>, Points3DVisualizationObjectConfiguration>
    {
        private IContractResolver contractResolver = new Instant3DVisualizationObjectContractResolver();

        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.Points3DVisualizationObject";

        /// <inheritdoc />
        protected override IContractResolver ContractResolver => this.contractResolver;
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.ScatterRect3DVisualizationObject />.
    /// </summary>
    public sealed class ScatterRect3DVisualizationObject : StreamVisualizationObject<List<(CoordinateSystem, Rect3D)>, ScatterRect3DVisualizationObjectConfiguration>
    {
        private IContractResolver contractResolver = new Instant3DVisualizationObjectContractResolver();

        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.ScatterRect3DVisualizationObject";

        /// <inheritdoc />
        protected override IContractResolver ContractResolver => this.contractResolver;
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.ScatterCoordinateSystemsVisualizationObject />.
    /// </summary>
    public sealed class ScatterCoordinateSystemsVisualizationObject : StreamVisualizationObject<List<CoordinateSystem>, ScatterCoordinateSystemsVisualizationObjectConfiguration>
    {
        private IContractResolver contractResolver = new Instant3DVisualizationObjectContractResolver();

        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.ScatterCoordinateSystemsVisualizationObject";

        /// <inheritdoc />
        protected override IContractResolver ContractResolver => this.contractResolver;
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.ScatterPlotVisualizationObject />.
    /// </summary>
    public sealed class ScatterPlotVisualizationObject : StreamVisualizationObject<List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.ScatterPlotVisualizationObject";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.ScatterRectangleVisualizationObject />.
    /// </summary>
    public sealed class ScatterRectangleVisualizationObject : StreamVisualizationObject<List<Tuple<System.Drawing.Rectangle, string>>, ScatterRectangleVisualizationObjectConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.ScatterRectangleVisualizationObject";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.SpeechRecognitionVisualizationObject />.
    /// </summary>
    public sealed class SpeechRecognitionVisualizationObject : StreamVisualizationObject<IStreamingSpeechRecognitionResult, SpeechRecognitionVisualizationObjectConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.SpeechRecognitionVisualizationObject";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.StringWithDurationVisualizationObject />.
    /// </summary>
    public sealed class StringWithDurationVisualizationObject : StreamVisualizationObject<Tuple<string, TimeSpan>, StringWithDurationVisualizationObjectConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.StringWithDurationVisualizationObject";
    }

    /// <summary>
    /// Class implements a client proxy for Microsoft.Psi.Visualization.VisualizationObjects.TimeIntervalVisualizationObject />.
    /// </summary>
    public sealed class TimeIntervalVisualizationObject : StreamVisualizationObject<Tuple<DateTime, DateTime>, TimeIntervalVisualizationObjectConfiguration>
    {
        /// <inheritdoc/>
        public override string TypeName => "Microsoft.Psi.Visualization.VisualizationObjects.TimeIntervalVisualizationObject";
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Client
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Extensions.Annotations;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Extension methods that simplify visualization client usage.
    /// </summary>
    public static class VisualizationClientExtensions
    {
        /// <summary>
        /// Adds a new timeline visualization panel.
        /// </summary>
        /// <param name="vc">The visualization client.</param>
        /// <returns>The newly created timeline visualization panel.</returns>
        public static TimelineVisualizationPanel AddTimelinePanel(this VisualizationClient vc)
        {
            return vc.AddPanel<TimelineVisualizationPanel>();
        }

        /// <summary>
        /// Adds a new XY visualization panel.
        /// </summary>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created XY visualization panel.</returns>
        public static XYVisualizationPanel AddXYPanel(this VisualizationClient vc)
        {
            return vc.AddPanel<XYVisualizationPanel>();
        }

        /// <summary>
        /// Adds a new XYZ visualization panel.
        /// </summary>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created XYZ visualization panel.</returns>
        public static XYZVisualizationPanel AddXYZPanel(this VisualizationClient vc)
        {
            return vc.AddPanel<XYZVisualizationPanel>();
        }

        /// <summary>
        /// Clears the entire visualization container.
        /// </summary>
        /// <param name="vc">The visuzalization client.</param>
        public static void ClearAll(this VisualizationClient vc)
        {
            vc.CurrentContainer.Clear();
        }

        /// <summary>
        /// Sets the current rendering mode to <see cref="RemoteNavigationMode.Live"/>.
        /// </summary>
        /// <param name="vc">The visuzalization client.</param>
        /// <param name="start">The starting position of the navigator range. Default sets to <see cref="DateTime.UtcNow"/>.</param>
        /// <param name="duration">The duration of the navigator range. Default sets to 10 seconds.</param>
        public static void SetLiveMode(this VisualizationClient vc, DateTime start = default(DateTime), TimeSpan duration = default(TimeSpan))
        {
            if (start == default(DateTime))
            {
                start = DateTime.UtcNow;
            }

            if (duration == default(TimeSpan))
            {
                duration = TimeSpan.FromSeconds(10);
            }

            var navigator = vc.CurrentContainer.RemoteNavigator;
            var end = start + duration;
            navigator.ViewRange.SetRange(start, end);
            navigator.SelectionRange.SetRange(start, end);
            navigator.DataRange.SetRange(start, end);
            navigator.NavigationMode = RemoteNavigationMode.Live;
        }

        /// <summary>
        /// Sets the navigator extents to specified time interval.
        /// </summary>
        /// <param name="vc">The visuzalization client.</param>
        /// <param name="timeInterval">Time interval to set navigator extents to.</param>
        public static void SetNavigatorToExtents(this VisualizationClient vc, TimeInterval timeInterval)
        {
            vc.CurrentContainer.RemoteNavigator.ViewRange.SetRange(timeInterval.Left, timeInterval.Right);
            vc.CurrentContainer.RemoteNavigator.SelectionRange.SetRange(timeInterval.Left, timeInterval.Right);
            vc.CurrentContainer.RemoteNavigator.DataRange.SetRange(timeInterval.Left, timeInterval.Right);
            vc.CurrentContainer.RemoteNavigator.Cursor = timeInterval.Left;
        }

        /// <summary>
        /// Sets the navigator based on current time with a 6 second range.
        /// </summary>
        /// <param name="vc">The visuzalization client.</param>
        public static void SetNavigatorToNow(this VisualizationClient vc)
        {
            vc.SetNavigatorToNow(TimeSpan.FromSeconds(6));
        }

        /// <summary>
        /// Sets the navigator based on current time and a provided range.
        /// </summary>
        /// <param name="vc">The visuzalization client.</param>
        /// <param name="delta">Range of time to include in view.</param>
        public static void SetNavigatorToNow(this VisualizationClient vc, TimeSpan delta)
        {
            var now = DateTime.Now;
            var navigator = vc.CurrentContainer.RemoteNavigator;
            navigator.ViewRange.SetRange(now, now + delta);
            navigator.SelectionRange.SetRange(navigator.ViewRange.StartTime, navigator.ViewRange.EndTime);
            navigator.DataRange.SetRange(navigator.ViewRange.StartTime - delta, navigator.ViewRange.EndTime + delta);
        }

        /// <summary>
        /// Shows the specified stream in an <see cref="XYZVisualizationPanel"/>
        /// </summary>
        /// <typeparam name="TObject">The type of client stream visualization object (proxy) to create.</typeparam>
        /// <typeparam name="TData">The type of underlying data of the stream visualization object.</typeparam>
        /// <typeparam name="TConfig">The type of configuration of the stream visualization object</typeparam>
        /// <param name="vc">The visuzalization client.</param>
        /// <param name="stream">Stream to show.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static TObject Show3D<TObject, TData, TConfig>(this VisualizationClient vc, IProducer<TData> stream)
            where TObject : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            return vc.Show<TObject, TData, TConfig, XYZVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows the specified stream in an appropriate visualization panel, creating a new one if needed.
        /// </summary>
        /// <typeparam name="TObject">The type of client stream visualization object (proxy) to create.</typeparam>
        /// <typeparam name="TData">The type of underlying data of the stream visualization object.</typeparam>
        /// <typeparam name="TConfig">The type of configuration of the stream visualization object</typeparam>
        /// <typeparam name="TPanel">The type of visauzalition panel required to show the stream.</typeparam>
        /// <param name="vc">The visuzalization client.</param>
        /// <param name="stream">Stream to show.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static TObject Show<TObject, TData, TConfig, TPanel>(this VisualizationClient vc, IProducer<TData> stream)
            where TObject : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
            where TPanel : VisualizationPanel, new()
        {
            PsiStreamMetadata streamMetadata;
            if (!Store.TryGetMetadata(stream, out streamMetadata))
            {
                throw new ApplicationException("Stream not found.");
            }

            var streamBinding = new StreamBinding(streamMetadata.Name, streamMetadata.PartitionName, streamMetadata.PartitionName, streamMetadata.PartitionPath, typeof(SimpleReader));
            return vc.Show<TObject, TData, TConfig, TPanel>(streamBinding);
        }

        /// <summary>
        /// Shows the specified stream in an <see cref="XYVisualizationPanel"/>
        /// </summary>
        /// <typeparam name="TObject">The type of client stream visualization object (proxy) to create.</typeparam>
        /// <typeparam name="TData">The type of underlying data of the stream visualization object.</typeparam>
        /// <typeparam name="TConfig">The type of configuration of the stream visualization object</typeparam>
        /// <param name="stream">Stream to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static TObject Show<TObject, TData, TConfig>(this IProducer<TData> stream, VisualizationClient vc)
            where TObject : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            return vc.Show<TObject, TData, TConfig, XYVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows an audio stream.
        /// </summary>
        /// <param name="stream">Aduio stream to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <param name="channel">The audio channel to show. Default is 0.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static AudioVisualizationObject Show(this IProducer<AudioBuffer> stream, VisualizationClient vc, int channel = 0)
        {
            PsiStreamMetadata streamMetadata;
            if (!Store.TryGetMetadata(stream, out streamMetadata))
            {
                throw new ApplicationException("Stream not found.");
            }

            var streamBinding = new StreamBinding(
                streamMetadata.Name,
                streamMetadata.PartitionName,
                streamMetadata.PartitionName,
                streamMetadata.PartitionPath,
                typeof(SimpleReader),
                null,
                "Microsoft.Psi.Visualization.Summarizers.AudioSummarizer",
                new object[] { channel });
            return vc.Show<AudioVisualizationObject, double, AudioVisualizationObjectConfiguration, TimelineVisualizationPanel>(streamBinding);
        }

        /// <summary>
        /// Shows an encoded image stream.
        /// </summary>
        /// <param name="stream">Encoded image stream to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static EncodedImageVisualizationObject Show(this IProducer<Shared<EncodedImage>> stream, VisualizationClient vc)
        {
            return vc.Show<EncodedImageVisualizationObject, Shared<EncodedImage>, ImageVisualizationObjectBaseConfiguration, XYVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows an image stream.
        /// </summary>
        /// <param name="stream">Image stream to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static ImageVisualizationObject Show(this IProducer<Shared<Image>> stream, VisualizationClient vc)
        {
            return vc.Show<ImageVisualizationObject, Shared<Image>, ImageVisualizationObjectBaseConfiguration, XYVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows a Kinect bodies stream in an XYZ visualization panel.
        /// </summary>
        /// <param name="stream">Kinect bodies stream to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static KinectBodies3DVisualizationObject Show(this IProducer<List<KinectBody>> stream, VisualizationClient vc)
        {
            return vc.Show<KinectBodies3DVisualizationObject, List<KinectBody>, KinectBodies3DVisualizationObjectConfiguration, XYZVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows a list of 3D points stream in an XYZ visualization panel.
        /// </summary>
        /// <param name="stream">A list of 3D points  stream to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static Points3DVisualizationObject ShowScatterPlot3D(this IProducer<List<System.Windows.Media.Media3D.Point3D>> stream, VisualizationClient vc)
        {
            return vc.Show<Points3DVisualizationObject, List<System.Windows.Media.Media3D.Point3D>, Points3DVisualizationObjectConfiguration, XYZVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows a stream of doubles.
        /// </summary>
        /// <param name="stream">A stream of doubles to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static PlotVisualizationObject Show(this IProducer<double> stream, VisualizationClient vc)
        {
            PsiStreamMetadata streamMetadata;
            if (!Store.TryGetMetadata(stream, out streamMetadata))
            {
                throw new ApplicationException("Stream not found.");
            }

            var streamBinding = new StreamBinding(
                streamMetadata.Name,
                streamMetadata.PartitionName,
                streamMetadata.PartitionName,
                streamMetadata.PartitionPath,
                typeof(SimpleReader),
                null,
                "Microsoft.Psi.Visualization.Summarizers.RangeSummarizer");
            return vc.Show<PlotVisualizationObject, double, PlotVisualizationObjectConfiguration, TimelineVisualizationPanel>(streamBinding);
        }

        /// <summary>
        /// Shows a stream of booleans.
        /// </summary>
        /// <param name="stream">A stream of booleans to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static PlotVisualizationObject Show(this IProducer<bool> stream, VisualizationClient vc)
        {
            PsiStreamMetadata streamMetadata;
            if (!Store.TryGetMetadata(stream, out streamMetadata))
            {
                throw new ApplicationException("Stream not found.");
            }

            var streamBinding = new StreamBinding(
                streamMetadata.Name,
                streamMetadata.PartitionName,
                streamMetadata.PartitionName,
                streamMetadata.PartitionPath,
                typeof(SimpleReader),
                "Microsoft.Psi.Visualization.Adapters.BoolAdapter",
                "Microsoft.Psi.Visualization.Summarizers.RangeSummarizer");
            var boolPlot = vc.Show<PlotVisualizationObject, double, PlotVisualizationObjectConfiguration, TimelineVisualizationPanel>(streamBinding);
            boolPlot.Configuration.YMin = -0.2;
            boolPlot.Configuration.YMax = 1.2;
            return boolPlot;
        }

        /// <summary>
        /// Shows a stream of a list of coordinate systems.
        /// </summary>
        /// <param name="stream">A stream of a list of coordinate systems to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static ScatterCoordinateSystemsVisualizationObject Show(this IProducer<List<CoordinateSystem>> stream, VisualizationClient vc)
        {
            return vc.Show<ScatterCoordinateSystemsVisualizationObject, List<CoordinateSystem>, ScatterCoordinateSystemsVisualizationObjectConfiguration, XYZVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows a stream of a list of named points.
        /// </summary>
        /// <param name="stream">A stream of a list of named points to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static ScatterPlotVisualizationObject Show(this IProducer<List<Tuple<Point, string>>> stream, VisualizationClient vc)
        {
            return vc.Show<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration, XYVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows a stream of a list of rectangles.
        /// </summary>
        /// <param name="stream">A stream of a list of rectangles to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static ScatterRectangleVisualizationObject Show(this IProducer<List<Tuple<System.Drawing.Rectangle, string>>> stream, VisualizationClient vc)
        {
            return vc.Show<ScatterRectangleVisualizationObject, List<Tuple<System.Drawing.Rectangle, string>>, ScatterRectangleVisualizationObjectConfiguration, XYVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows a stream of speech recognition results.
        /// </summary>
        /// <param name="stream">A stream of speech recognition results to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static SpeechRecognitionVisualizationObject Show(this IProducer<IStreamingSpeechRecognitionResult> stream, VisualizationClient vc)
        {
            return vc.Show<SpeechRecognitionVisualizationObject, IStreamingSpeechRecognitionResult, SpeechRecognitionVisualizationObjectConfiguration, TimelineVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows a stream of strings with durations.
        /// </summary>
        /// <param name="stream">A stream of strings with durations to show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static StringWithDurationVisualizationObject Show(this IProducer<Tuple<string, TimeSpan>> stream, VisualizationClient vc)
        {
            return vc.Show<StringWithDurationVisualizationObject, Tuple<string, TimeSpan>, StringWithDurationVisualizationObjectConfiguration, TimelineVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows a stream of annotations
        /// </summary>
        /// <param name="stream">A stream of annotations show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static AnnotatedEventVisualizationObject ShowAnnotations(this IProducer<AnnotatedEvent> stream, VisualizationClient vc)
        {
            return vc.Show<AnnotatedEventVisualizationObject, AnnotatedEvent, AnnotatedEventVisualizationObjectConfiguration, TimelineVisualizationPanel>(stream);
        }

        /// <summary>
        /// Shows a derived stream of message latencies for any other stream.
        /// </summary>
        /// <typeparam name="T">The type of the stream.</typeparam>
        /// <param name="stream">A stream of annotations show.</param>
        /// <param name="vc">The visuzalization client.</param>
        /// <returns>The newly created <see cref="StreamVisualizationObject{TData, TConfig}"/> used to show the stream.</returns>
        public static TimeIntervalVisualizationObject ShowLatency<T>(this IProducer<T> stream, VisualizationClient vc)
        {
            var latencyStream = stream.Select((data, env) => Tuple.Create(env.OriginatingTime, env.Time));
            return vc.Show<TimeIntervalVisualizationObject, Tuple<DateTime, DateTime>, TimeIntervalVisualizationObjectConfiguration, TimelineVisualizationPanel>(latencyStream);
        }
    }
}

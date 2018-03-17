// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Windows;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi.Audio;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Extensions.Annotations;
    using Microsoft.Psi.Extensions.Base;
    using Microsoft.Psi.Extensions.Data;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;
    using Microsoft.Psi.Serialization;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Visualization.Adapters;
    using Microsoft.Psi.Visualization.Base;
    using Microsoft.Psi.Visualization.Config;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Summarizers;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Data context for PsiStudio.
    /// </summary>
    public class PsiStudioContext : ObservableObject
    {
        private VisualizationContainer visualizationContainer;
        private SimpleReader dataStore;
        private Dataset dataset;

        private Pipeline audioPlaybackPipeline;
        private RelayCommand playCommand;
        private string playbackSpeed = "1.0";

        private List<TypeKeyedActionCommand> typeVisualizerActions = new List<TypeKeyedActionCommand>();

        static PsiStudioContext()
        {
            PsiStudioContext.Instance = new PsiStudioContext();
        }

        private PsiStudioContext()
        {
            this.InitVisualizeStreamCommands();

            var booleanSchema = new AnnotationSchema("Boolean");
            booleanSchema.AddSchemaValue(null, System.Drawing.Color.Gray);
            booleanSchema.AddSchemaValue("false", System.Drawing.Color.Red);
            booleanSchema.AddSchemaValue("true", System.Drawing.Color.Green);
            AnnotationSchemaRegistry.Default.Register(booleanSchema);

            this.Dataset = new Dataset();
            this.Datasets = new ObservableCollection<Dataset> { this.dataset };
        }

        /// <summary>
        /// Gets the PsiStudioContext singleton.
        /// </summary>
        public static PsiStudioContext Instance { get; private set; }

        /// <summary>
        /// Gets the annotation schema registry.
        /// </summary>
        public AnnotationSchemaRegistry AnnotationSchemaRegistry => AnnotationSchemaRegistry.Default;

        /// <summary>
        /// Gets the collection of datasets.
        /// </summary>
        public ObservableCollection<Dataset> Datasets { get; private set; }

        /// <summary>
        /// Gets or sets the current dataset.
        /// </summary>
        public Dataset Dataset
        {
            get => this.dataset;
            set
            {
                this.dataset = value;
                this.RaisePropertyChanged(nameof(this.Dataset));
            }
        }

        /// <summary>
        /// Gets or sets the visualization container.
        /// </summary>
        public VisualizationContainer VisualizationContainer
        {
            get => this.visualizationContainer;
            set
            {
                this.visualizationContainer = value;
                this.RaisePropertyChanged(nameof(this.VisualizationContainer));
            }
        }

        /// <summary>
        /// Gets the data stroe.
        /// </summary>
        public SimpleReader DataStore
        {
            get => this.dataStore;
            private set { this.Set(nameof(this.DataStore), ref this.dataStore, value); }
        }

        /// <summary>
        /// Gets or sets the playback speed.
        /// </summary>
        public string PlaybackSpeed
        {
            get => this.playbackSpeed;
            set { this.Set(nameof(this.PlaybackSpeed), ref this.playbackSpeed, value); }
        }

        /// <summary>
        /// Gets the play command.
        /// </summary>
        [Browsable(false)]
        [IgnoreDataMember]
        public RelayCommand PlayCommand
        {
            get
            {
                if (this.playCommand == null)
                {
                    this.playCommand = new RelayCommand(
                        o => this.Play(),
                        o => this.VisualizationContainer.Navigator.NavigationMode == Visualization.Navigation.NavigationMode.Playback);
                }

                return this.playCommand;
            }
        }

        /// <summary>
        /// Display the add annotation dialog.
        /// </summary>
        /// <param name="owner">The window that will own this dialog.</param>
        public void AddAnnotation(Window owner)
        {
            AddAnnotationWindow dlg = new AddAnnotationWindow(AnnotationSchemaRegistry.Default.Schemas);
            dlg.Owner = owner;
            dlg.StorePath = string.IsNullOrWhiteSpace(this.Dataset.FileName) ? Environment.CurrentDirectory : Path.GetDirectoryName(this.Dataset.FileName);
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                // test for overwrite
                var path = Path.Combine(dlg.StorePath, dlg.StoreName + ".pas");
                if (File.Exists(path))
                {
                    var overwrite = MessageBox.Show(
                        owner,
                        $"The annotation file ({dlg.StoreName + ".pas"}) already exists in {dlg.StorePath}. Overwrite?",
                        "Overwrite Annotation File",
                        MessageBoxButton.OKCancel,
                        MessageBoxImage.Warning,
                        MessageBoxResult.Cancel);
                    if (overwrite == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                // create a new panel for the annotations - don't make it the current panel
                var panel = new TimelineVisualizationPanel();
                panel.Configuration.Name = dlg.PartitionName;
                panel.Configuration.Height = 22;
                this.VisualizationContainer.AddPanel(panel);

                // create a new annotated event visualization object and add to the panel
                var annotations = new AnnotatedEventVisualizationObject();
                annotations.Configuration.Name = dlg.AnnotationName;
                panel.AddVisualizationObject(annotations);

                // create a new annotation definition and store
                var definition = new AnnotatedEventDefinition(dlg.StreamName);
                definition.Schemas.Add(dlg.AnnotationSchema);
                this.Dataset.CurrentSession.CreateAnnotationPartition(dlg.StoreName, dlg.StorePath, definition);

                // open the stream for visualization (NOTE: if the selection extents were MinTime/MaxTime, no event will be created)
                annotations.OpenStream(new StreamBinding(dlg.StreamName, dlg.PartitionName, dlg.StoreName, dlg.StorePath, typeof(AnnotationSimpleReader)));
            }
        }

        /// <summary>
        /// Opens a previously persisted layout file.
        /// </summary>
        /// <param name="filename">Fully qualified path to layout file.</param>
        public void OpenLayout(string filename)
        {
            this.VisualizationContainer.Clear();
            this.VisualizationContainer = VisualizationContainer.Load(filename);

            // zoom into the current session
            var session = this.Dataset.CurrentSession;
            var timeInterval = session?.OriginatingTimeInterval;
            timeInterval = timeInterval ?? new TimeInterval(DateTime.MinValue, DateTime.MaxValue);
            this.VisualizationContainer.ZoomToRange(timeInterval);

            // set the data range to the dataset
            this.VisualizationContainer.Navigator.DataRange.SetRange(this.Dataset.OriginatingTimeInterval);

            // update store bindings
            this.VisualizationContainer.UpdateStoreBindings(session == null ? new List<IPartition>() : session.Partitions.ToList());
        }

        /// <summary>
        /// Opens a previously persisted dataset.
        /// </summary>
        /// <param name="filename">Fully qualified path to dataset file.</param>
        public void OpenDataset(string filename)
        {
            var fileInfo = new FileInfo(filename);
            if (fileInfo.Extension == ".psi")
            {
                var name = fileInfo.Name.Split('.')[0];
                this.Dataset = Dataset.CreateFromExistingStore(name, fileInfo.DirectoryName);
            }
            else
            {
                this.Dataset = Dataset.Load(filename);
            }

            this.Datasets.Clear();
            this.Datasets.Add(this.Dataset);

            // set the data range to the dataset
            this.VisualizationContainer.Navigator.DataRange.SetRange(this.Dataset.OriginatingTimeInterval);

            // zoom into the current session
            var timeInterval = this.Dataset.CurrentSession?.OriginatingTimeInterval;
            timeInterval = timeInterval ?? new TimeInterval(DateTime.MinValue, DateTime.MaxValue);
            this.VisualizationContainer.ZoomToRange(timeInterval);
        }

        /// <summary>
        /// Inovke playback of audio stream.
        /// </summary>
        public void Play()
        {
            double speed = 1.0;
            double.TryParse(this.playbackSpeed, out speed);
            ReplayDescriptor replayDescriptor = null;

            if (this.audioPlaybackPipeline != null)
            {
                this.audioPlaybackPipeline.Dispose();
                this.audioPlaybackPipeline = null;
            }

            this.audioPlaybackPipeline = Pipeline.Create("AudioPlayer");
            replayDescriptor = new ReplayDescriptor(this.VisualizationContainer.Navigator.SelectionRange.StartTime, this.VisualizationContainer.Navigator.SelectionRange.EndTime, false, true, (float)(1.0 / speed));
            var partition = this.Dataset.CurrentSession.Partitions.First();
            var importer = Store.Open(this.audioPlaybackPipeline, partition.StoreName, partition.StorePath);

            // Find first stream that contains audio.
            string audioBufferTypeName = typeof(AudioBuffer).AssemblyQualifiedName;
            var audioStream = importer.AvailableStreams.FirstOrDefault(s => s.TypeName == audioBufferTypeName);

            // Play the audio stream, if found.
            if (audioStream != null)
            {
                var audioPlayer = new AudioPlayer(this.audioPlaybackPipeline, new AudioPlayerConfiguration());
                var stream = importer.OpenStream<AudioBuffer>(audioStream.Name);
                stream.PipeTo(audioPlayer.In);
                this.audioPlaybackPipeline.RunAsync(replayDescriptor);
            }

            this.VisualizationContainer.Navigator.Play(speed);
        }

        /// <summary>
        /// Stop playback of audio stream.
        /// </summary>
        public void StopPlaying()
        {
            this.VisualizationContainer.Navigator.StopPlaying();
            if (this.audioPlaybackPipeline != null)
            {
                this.audioPlaybackPipeline.Dispose();
                this.audioPlaybackPipeline = null;
            }
        }

        /// <summary>
        /// Gets the list of visualization stream commands for a given stream tree node.
        /// </summary>
        /// <param name="streamTreeNode">Stream tree node.</param>
        /// <returns>List of visualization stream commands.</returns>
        internal List<TypeKeyedActionCommand> GetVisualizeStreamCommands(IStreamTreeNode streamTreeNode)
        {
            List<TypeKeyedActionCommand> result = null;
            if (streamTreeNode != null && streamTreeNode.TypeName != null)
            {
                // Get the Type from the loaded assemblies that matches the stream type
                var streamType = Type.GetType(streamTreeNode.TypeName, this.AssemblyResolver, null) ?? Type.GetType(streamTreeNode.TypeName.Split(',')[0], this.AssemblyResolver, null);

                if (streamType != null)
                {
                    // Get the list of commands
                    result = this.typeVisualizerActions.Where(a => a.TypeKey.AssemblyQualifiedName == streamType?.AssemblyQualifiedName).ToList();

                    // generate generic Plot Latency
                    var genericPlotLatency = typeof(PsiStudioContext).GetMethod("PlotLatency", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(streamType);
                    var plotLatencyAction = new Action<IStreamTreeNode>(s => genericPlotLatency.Invoke(this, new object[] { s, false }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { "Plot Latency", plotLatencyAction }) as TypeKeyedActionCommand);

                    // generate generic View Messages
                    var genericPlotMessages = typeof(PsiStudioContext).GetMethod("PlotMessages", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(streamType);
                    var plotMessagesAction = new Action<IStreamTreeNode>(s => genericPlotMessages.Invoke(this, new object[] { s }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { "Visualize Messages", plotMessagesAction }) as TypeKeyedActionCommand);

                    var genericZoomToStreamExtents = typeof(PsiStudioContext).GetMethod("ZoomToStreamExtents", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(streamType);
                    var zoomToStreamExtentsAction = new Action<IStreamTreeNode>(s => genericZoomToStreamExtents.Invoke(this, new object[] { s }));
                    result.Add(Activator.CreateInstance(typeof(TypeKeyedActionCommand<,>).MakeGenericType(streamType, typeof(IStreamTreeNode)), new object[] { "Zoom to Stream Extents", zoomToStreamExtentsAction }) as TypeKeyedActionCommand);
                }
            }

            return result;
        }

        private void InitVisualizeStreamCommands()
        {
            KnownSerializers.Default.Register<MathNet.Numerics.LinearAlgebra.Storage.DenseColumnMajorMatrixStorage<double>>(null);

            this.AddVisualizeStreamCommand<AnnotatedEvent>("Visualize", (s) => this.ShowAnnotations(s, false));
            this.AddVisualizeStreamCommand<double>("Plot", (s) => this.PlotDouble(s, false));
            this.AddVisualizeStreamCommand<float>("Plot", (s) => this.PlotFloat(s, false));
            this.AddVisualizeStreamCommand<TimeSpan>("Plot (as ms)", (s) => this.PlotTimeSpan(s, false));
            this.AddVisualizeStreamCommand<int>("Plot", (s) => this.PlotInt(s, false));
            this.AddVisualizeStreamCommand<bool>("Plot", (s) => this.PlotBool(s, false));
            this.AddVisualizeStreamCommand<Shared<Image>>("Visualize", (s) => this.Show2D<ImageVisualizationObject, Shared<Image>, ImageVisualizationObjectBaseConfiguration>(s, true));
            this.AddVisualizeStreamCommand<Shared<EncodedImage>>("Visualize", (s) => this.Show2D<EncodedImageVisualizationObject, Shared<EncodedImage>, ImageVisualizationObjectBaseConfiguration>(s, true));
            this.AddVisualizeStreamCommand<IStreamingSpeechRecognitionResult>("Visualize", (s) => this.Show<SpeechRecognitionVisualizationObject, IStreamingSpeechRecognitionResult, SpeechRecognitionVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<KinectBody>>("Visualize", (s) => this.Show3D<KinectBodies3DVisualizationObject, List<KinectBody>, KinectBodies3DVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<CoordinateSystem>>("Visualize ", (s) => this.Show3D<ScatterCoordinateSystemsVisualizationObject, List<CoordinateSystem>, ScatterCoordinateSystemsVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<CoordinateSystem>>("Visualize as Planar Direction", (s) => this.Show3D<ScatterPlanarDirectionVisualizationObject, List<CoordinateSystem>, ScatterPlanarDirectionVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<CoordinateSystem>("Visualize", (s) => this.Show3D<ScatterCoordinateSystemsVisualizationObject, List<CoordinateSystem>, ScatterCoordinateSystemsVisualizationObjectConfiguration>(s, false, typeof(CoordinateSystemAdapter)));
            this.AddVisualizeStreamCommand<Point[]>("Visualize", (s) => this.Show2D<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>(s, false, typeof(PointArrayToScatterPlotAdapter)));
            this.AddVisualizeStreamCommand<List<Tuple<Point, string>>>("Visualize", (s) => this.Show2D<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<Point2D?>("Visualize", (s) => this.Show2D<ScatterPlotVisualizationObject, List<Tuple<Point, string>>, ScatterPlotVisualizationObjectConfiguration>(s, false, typeof(NullablePoint2DToScatterPlotAdapter)));
            this.AddVisualizeStreamCommand<Point3D?>("Visualize", (s) => this.Show3D<Points3DVisualizationObject, List<System.Windows.Media.Media3D.Point3D>, Points3DVisualizationObjectConfiguration>(s, false, typeof(NullablePoint3DAdapter)));
            this.AddVisualizeStreamCommand<List<Point3D>>("Visualize", (s) => this.Show3D<Points3DVisualizationObject, List<System.Windows.Media.Media3D.Point3D>, Points3DVisualizationObjectConfiguration>(s, false, typeof(ListPoint3DAdapter)));
            this.AddVisualizeStreamCommand<byte[]>("Visualize as 3D Depth", this.ShowDepth3D);
            this.AddVisualizeStreamCommand<byte[]>("Visualize as 2D Depth", this.ShowDepth2D);
            this.AddVisualizeStreamCommand<AudioBuffer>("Visualize", this.PlotAudio);
            this.AddVisualizeStreamCommand<List<Tuple<System.Drawing.Rectangle, string>>>("Visualize", (s) => this.Show2D<ScatterRectangleVisualizationObject, List<Tuple<System.Drawing.Rectangle, string>>, ScatterRectangleVisualizationObjectConfiguration>(s, false));
            this.AddVisualizeStreamCommand<List<System.Drawing.Rectangle>>("Visualize", (s) => this.Show2D<ScatterRectangleVisualizationObject, List<Tuple<System.Drawing.Rectangle, string>>, ScatterRectangleVisualizationObjectConfiguration>(s, false, typeof(ListRectangleAdapter)));
        }

        private void AddVisualizeStreamCommand<TKey>(string displayName, Action<IStreamTreeNode> action)
        {
            this.typeVisualizerActions.Add(new TypeKeyedActionCommand<TKey, IStreamTreeNode>(displayName, action));
        }

        private void EnsureCurrentPanel<T>(bool newPanel)
            where T : VisualizationPanel, new()
        {
            if (newPanel || this.VisualizationContainer.CurrentPanel == null || (this.VisualizationContainer.CurrentPanel as T) == null)
            {
                var panel = new T();
                this.VisualizationContainer.AddPanel(panel);
            }
        }

        private void Show<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<TimelineVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel);
        }

        private void Show<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel, Type streamAdapterType)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<TimelineVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel, streamAdapterType);
        }

        private TVisObj Show<TPanel, TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel, Type streamAdapterType = null, Type summarizerType = null, params object[] summarizerArgs)
            where TPanel : VisualizationPanel, new()
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            var partition = streamTreeNode.Partition;
            var visObj = new TVisObj();
            visObj.Configuration.Name = streamTreeNode.StreamName;

            this.EnsureCurrentPanel<TPanel>(newPanel);
            this.VisualizationContainer.CurrentPanel.AddVisualizationObject(visObj);

            var streamBinding = new StreamBinding(
                streamTreeNode.StreamName, partition.Name, partition.StoreName, partition.StorePath, typeof(SimpleReader), streamAdapterType, summarizerType, summarizerArgs);
            visObj.OpenStream(streamBinding);

            // Don't zoom to range in live mode
            if (this.VisualizationContainer.Navigator.NavigationMode != Visualization.Navigation.NavigationMode.Live)
            {
                this.VisualizationContainer.ZoomToRange(streamTreeNode.Partition.OriginatingTimeInterval);
            }

            return visObj;
        }

        private void Show2D<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<XYVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel);
        }

        private void Show2D<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel, Type streamAdapterType)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<XYVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel, streamAdapterType);
        }

        private void Show3D<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<XYZVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel);
        }

        private void Show3D<TVisObj, TData, TConfig>(IStreamTreeNode streamTreeNode, bool newPanel, Type streamAdapterType)
            where TVisObj : StreamVisualizationObject<TData, TConfig>, new()
            where TConfig : StreamVisualizationObjectConfiguration, new()
        {
            this.Show<XYZVisualizationPanel, TVisObj, TData, TConfig>(streamTreeNode, newPanel, streamAdapterType);
        }

        private AnnotatedEventVisualizationObject ShowAnnotations(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            var partition = streamTreeNode.Partition;
            var visObj = new AnnotatedEventVisualizationObject();
            visObj.Configuration.Name = streamTreeNode.StreamName;

            this.EnsureCurrentPanel<TimelineVisualizationPanel>(newPanel);
            this.VisualizationContainer.CurrentPanel.AddVisualizationObject(visObj);

            var streamBinding = new StreamBinding(streamTreeNode.StreamName, partition.Name, partition.StoreName, partition.StorePath, typeof(AnnotationSimpleReader));
            visObj.OpenStream(streamBinding);
            this.VisualizationContainer.ZoomToRange(streamTreeNode.Partition.OriginatingTimeInterval);

            return visObj;
        }

        private void ShowDepth2D(IStreamTreeNode streamTreeNode)
        {
            this.Show2D<ImageVisualizationObject, Shared<Image>, ImageVisualizationObjectBaseConfiguration>(streamTreeNode, false, typeof(CompressedImageAdapter));
        }

        private void ShowDepth3D(IStreamTreeNode streamTreeNode)
        {
            this.Show3D<KinectDepth3DVisualizationObject, Shared<Image>, KinectDepth3DVisualizationObjectConfiguration>(
                streamTreeNode, false, typeof(CompressedImageAdapter));
        }

        private void PlotBool(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, typeof(BoolAdapter), typeof(RangeSummarizer));
        }

        private void PlotAudio(IStreamTreeNode streamTreeNode)
        {
            var visObj = this.Show<TimelineVisualizationPanel, AudioVisualizationObject, double, AudioVisualizationObjectConfiguration>(
                streamTreeNode, false, null, typeof(AudioSummarizer), 0);
            visObj.Configuration.Name = streamTreeNode.StreamName;
            this.VisualizationContainer.ZoomToRange(streamTreeNode.Partition.OriginatingTimeInterval);
        }

        private void PlotDouble(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, null, typeof(RangeSummarizer));
        }

        private void PlotFloat(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, typeof(FloatAdapter), typeof(RangeSummarizer));
        }

        private void PlotInt(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(streamTreeNode, newPanel, typeof(IntAdapter), typeof(RangeSummarizer));
        }

        private void PlotLatency<TData>(IStreamTreeNode streamTreeNode, bool newPanel = false)
        {
            var visObj = this.Show<TimelineVisualizationPanel, TimeIntervalVisualizationObject, Tuple<DateTime, DateTime>, TimeIntervalVisualizationObjectConfiguration>(
                streamTreeNode, newPanel, typeof(LatencyAdapter<TData>), typeof(TimeIntervalSummarizer));
            visObj.Configuration.Color = System.Drawing.Color.Red;
            visObj.Configuration.Name = streamTreeNode.StreamName;
        }

        private void PlotMessages<TData>(IStreamTreeNode streamTreeNode)
        {
            var visObj = this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(streamTreeNode, false, typeof(MessageAdapter<TData>));
            visObj.Configuration.MarkerSize = 4;
            visObj.Configuration.MarkerStyle = Visualization.Common.MarkerStyle.Circle;
            visObj.Configuration.Name = streamTreeNode.StreamName;
        }

        private void PlotTimeSpan(IStreamTreeNode streamTreeNode, bool newPanel)
        {
            this.Show<TimelineVisualizationPanel, PlotVisualizationObject, double, PlotVisualizationObjectConfiguration>(streamTreeNode, newPanel, typeof(TimeSpanAdapter), typeof(RangeSummarizer));
        }

        private void ZoomToStreamExtents<TData>(IStreamTreeNode streamTreeNode)
        {
            if (streamTreeNode.FirstMessageOriginatingTime.HasValue && streamTreeNode.LastMessageOriginatingTime.HasValue)
            {
                this.VisualizationContainer.Navigator.Zoom(streamTreeNode.FirstMessageOriginatingTime.Value, streamTreeNode.LastMessageOriginatingTime.Value);
            }
            else
            {
                this.VisualizationContainer.Navigator.ZoomToDataRange();
            }
        }

        private Assembly AssemblyResolver(AssemblyName assemblyName)
        {
            // Attempt to match by full name first
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().FullName == assemblyName.FullName);
            if (assembly != null)
            {
                return assembly;
            }

            // Otherwise try to match by simple name without version, culture or key
            assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName));
            if (assembly != null)
            {
                return assembly;
            }

            return null;
        }
    }
}

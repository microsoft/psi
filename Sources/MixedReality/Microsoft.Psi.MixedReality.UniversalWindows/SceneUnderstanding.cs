// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using MathNet.Spatial.Euclidean;
    using Microsoft.MixedReality.SceneUnderstanding;
    using Microsoft.Psi.Components;
    using Microsoft.Psi.Spatial.Euclidean;
    using StereoKit;
    using Windows.Perception.Spatial.Preview;

    /// <summary>
    /// Component representing perceived scene understanding.
    /// </summary>
    public class SceneUnderstanding : Generator, IProducer<SceneObjectCollection>, IDisposable
    {
        private readonly Pipeline pipeline;
        private readonly SceneUnderstandingConfiguration configuration;

        private CoordinateSystem scenePoseInWorld;
        private Scene scene = null;
        private (double Width, double Height) placementRectangleSize = (0, 0);

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneUnderstanding"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="configuration">The configuration for the component.</param>
        /// <param name="name">An optional name for the component.</param>
        public SceneUnderstanding(Pipeline pipeline, SceneUnderstandingConfiguration configuration = null, string name = nameof(SceneUnderstanding))
            : base(pipeline, true, name)
        {
            // requires Spatial Perception capability
            if (!SceneObserver.IsSupported())
            {
                throw new Exception("SceneObserver is not supported.");
            }

            this.pipeline = pipeline;
            this.configuration = configuration ??= new ();
            this.placementRectangleSize = this.configuration.InitialPlacementRectangleSize;
            this.PlacementRectangleSizeInput = pipeline.CreateReceiver<(int Height, int Width)>(this, this.UpdatePlacementRectangleSize, nameof(this.PlacementRectangleSizeInput));
            this.Out = pipeline.CreateEmitter<SceneObjectCollection>(this, nameof(this.Out));
        }

        /// <summary>
        /// Gets stream of placement size.
        /// </summary>
        public Receiver<(int Width, int Height)> PlacementRectangleSizeInput { get; private set; }

        /// <summary>
        /// Gets the stream of scene understanding.
        /// </summary>
        public Emitter<SceneObjectCollection> Out { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.scene?.Dispose();
        }

        /// <inheritdoc />
        protected override DateTime GenerateNext(DateTime currentTime)
        {
            SceneObserverAccessStatus accessStatus = SceneObserver.RequestAccessAsync().GetAwaiter().GetResult();
            if (accessStatus != SceneObserverAccessStatus.Allowed)
            {
                throw new Exception($"SceneObserver access denied: {accessStatus}");
            }

            // Initialize a new Scene
            if (this.scene == null)
            {
                this.scene = SceneObserver.ComputeAsync(this.configuration.SceneQuerySettings, (float)this.configuration.QueryRadius).GetAwaiter().GetResult();
            }
            else
            {
                var lastScene = this.scene;
                this.scene = SceneObserver.ComputeAsync(this.configuration.SceneQuerySettings, (float)this.configuration.QueryRadius, this.scene).GetAwaiter().GetResult();
                lastScene.Dispose();
            }

            // Get the transform to convert from scene understanding coordinates to world coordinates
            var sceneSpatialCoordinateSystem = SpatialGraphInteropPreview.CreateCoordinateSystemForNode(this.scene.OriginSpatialGraphNodeId);
            this.scenePoseInWorld = sceneSpatialCoordinateSystem.TryConvertSpatialCoordinateSystemToPsiCoordinateSystem() ?? new CoordinateSystem();

            List<Mesh3D> GetMeshes(IEnumerable<SceneObject> sceneObjects, Func<SceneObject, IEnumerable<SceneMesh>> selector)
            {
                return sceneObjects.SelectMany(
                    obj => obj.Meshes.Select(
                        m => ToMesh3D(m, this.GetWorldPose(obj)))).ToList();
            }

            List<Rectangle3D> GetRectangles(IEnumerable<SceneObject> sceneObjects)
            {
                Rectangle3D QuadToRectangle(SceneQuad quad, CoordinateSystem rectanglePose)
                {
                    var w = quad.Extents.X;
                    var h = quad.Extents.Y;

                    return new Rectangle3D(
                        rectanglePose.Origin,
                        rectanglePose.YAxis.Negate().Normalize(),
                        rectanglePose.ZAxis.Normalize(),
                        -w / 2,
                        -h / 2,
                        w,
                        h);
                }

                return sceneObjects.SelectMany(
                    obj => obj.Quads.Select(
                        q => QuadToRectangle(q, this.GetWorldPose(obj)))).ToList();
            }

            List<Rectangle3D?> GetPlacementRectangles(IEnumerable<SceneObject> sceneObjects)
            {
                if (this.configuration.ComputePlacementRectangles && this.placementRectangleSize.Width > 0 && this.placementRectangleSize.Height > 0)
                {
                    Rectangle3D? QuadToPlacementRectangle(SceneQuad quad, CoordinateSystem rectanglePose)
                    {
                        var w = this.placementRectangleSize.Width;
                        var h = this.placementRectangleSize.Height;
                        if (!quad.FindCentermostPlacement(new Vector2((float)w, (float)h), out var placement))
                        {
                            return null; // no placement found
                        }

                        // origin is top-left of quad plane, so shift to be relative to the centroid (in 3D)
                        var placementFromCenter = new Vec3(placement.X - (quad.Extents.X / 2f), placement.Y - (quad.Extents.Y / 2f), 0);

                        return new Rectangle3D(
                            rectanglePose.Transform(placementFromCenter.ToPoint3D()),
                            rectanglePose.YAxis.Negate().Normalize(),
                            rectanglePose.ZAxis.Normalize(),
                            -w / 2,
                            -h / 2,
                            w,
                            h);
                    }

                    return sceneObjects.SelectMany(
                        obj => obj.Quads.Select(
                            q => QuadToPlacementRectangle(q, this.GetWorldPose(obj)))).ToList();
                }
                else
                {
                    return new List<Rectangle3D?>(0);
                }
            }

            var scene = new SceneObjectCollection();

            foreach (var group in this.scene.SceneObjects.GroupBy(o => o.Kind))
            {
                Action<SceneObjectCollection.SceneObject> setter = group.Key switch
                {
                    SceneObjectKind.Background => x => scene.Background = x,
                    SceneObjectKind.Ceiling => x => scene.Ceiling = x,
                    SceneObjectKind.CompletelyInferred => x => scene.Inferred = x,
                    SceneObjectKind.Floor => x => scene.Floor = x,
                    SceneObjectKind.Platform => x => scene.Platform = x,
                    SceneObjectKind.Unknown => x => scene.Unknown = x,
                    SceneObjectKind.Wall => x => scene.Wall = x,
                    SceneObjectKind.World => x => scene.World = x,
                    _ => throw new Exception($"Unexpected scene object kind: {group.Key}"),
                };

                setter(new SceneObjectCollection.SceneObject(
                    GetMeshes(group, x => x.Meshes),
                    GetMeshes(group, x => x.ColliderMeshes),
                    GetRectangles(group),
                    GetPlacementRectangles(group)));
            }

            this.Out.Post(scene, currentTime);

            // Since acquiring the scene understanding information may take a long time,
            // if the normal scheduled time is behind the pipeline time, use the
            // current pipeline time.
            var scheduledTime = currentTime + this.configuration.MinQueryInterval;
            var currentPipelineTime = this.pipeline.GetCurrentTime();
            return (currentPipelineTime > scheduledTime) ? currentPipelineTime : scheduledTime;
        }

        private static Mesh3D ToMesh3D(SceneMesh mesh, CoordinateSystem meshPose)
        {
            var vertices = new Vector3[mesh.VertexCount];
            var indices = new uint[mesh.TriangleIndexCount];

            mesh.GetVertexPositions(vertices);
            mesh.GetTriangleIndices(indices);

            return new Mesh3D(vertices.Select(v => meshPose.Transform(v.ToPoint3D())).ToArray(), indices);
        }

        private void UpdatePlacementRectangleSize((int Width, int Height) size)
        {
            this.placementRectangleSize = size;
        }

        private CoordinateSystem GetWorldPose(SceneObject sceneObject)
        {
            var posePsiBasis = sceneObject.GetLocationAsMatrix().RebaseToMathNetCoordinateSystem();
            return posePsiBasis.TransformBy(this.scenePoseInWorld);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Microsoft Research license.

namespace Microsoft.Psi.MixedReality.Applications
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::StereoKit;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.MixedReality.StereoKit;
    using Color = System.Drawing.Color;

    /// <summary>
    /// Implements a general renderer of various stereokit UI elements.
    /// </summary>
    public class Renderer
    {
        private readonly Dictionary<global::StereoKit.Color, Material> materials = new ();
        private readonly Dictionary<float, Mesh> sphereMeshes = new ();
        private readonly Dictionary<(Font Font, float CharacterHeightInMeters, global::StereoKit.Color Color), TextStyle> textStyles = new ();
        private readonly Material occlusionMaterial;

        private CoordinateSystem head = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Renderer"/> class.
        /// </summary>
        internal Renderer()
        {
            this.occlusionMaterial = Material.Default.Copy();
            this.occlusionMaterial[MatParamName.ColorTint] = new global::StereoKit.Color(0, 0, 0, 0); // transparent black
            this.WireframeMaterial = Material.Default.Copy();
            this.WireframeMaterial.Wireframe = true;
        }

        /// <summary>
        /// Gets the wireframe material.
        /// </summary>
        public Material WireframeMaterial { get; }

        /// <summary>
        /// Set the head pose for the renderer.
        /// </summary>
        /// <param name="head">The current head pose.</param>
        public virtual void SetHeadPose(CoordinateSystem head)
        {
            head?.DeepClone(ref this.head);
        }

        /// <summary>
        /// Gets a coordinate system at a specified location aligned with the horizontal plane but pointing to the head position.
        /// </summary>
        /// <param name="x">The X coordinate for the location.</param>
        /// <param name="y">The Y coordinate for the location.</param>
        /// <param name="z">The Z coordinate for the location.</param>
        /// <returns>A coordinate system at a specified location aligned with the horizontal plane but pointing to the head position.</returns>
        public CoordinateSystem GetHorizontalHeadOrientedCoordinateSystem(double x, double y, double z)
            => this.GetHorizontalHeadOrientedCoordinateSystem(new Point3D(x, y, z));

        /// <summary>
        /// Gets a coordinate system at a specified location aligned with the horizontal plane but pointing to the head position.
        /// </summary>
        /// <param name="position">The position for the coordinate system.</param>
        /// <returns>A coordinate system at a specified location aligned with the horizontal plane but pointing to the head position.</returns>
        public CoordinateSystem GetHorizontalHeadOrientedCoordinateSystem(Point3D position)
        {
            var xAxis = (new Point3D(this.head.Origin.X, this.head.Origin.Y, position.Z) - position).Normalize();
            var zAxis = UnitVector3D.ZAxis;
            var yAxis = zAxis.CrossProduct(xAxis);
            return new CoordinateSystem(position, xAxis, yAxis, zAxis);
        }

        /// <summary>
        /// Gets a coordinate system at a specified location oriented towards the head position.
        /// </summary>
        /// <param name="x">The X coordinate for the location.</param>
        /// <param name="y">The Y coordinate for the location.</param>
        /// <param name="z">The Z coordinate for the location.</param>
        /// <returns>A coordinate system at a specified location oriented towards the head position.</returns>
        public CoordinateSystem GetHeadOrientedCoordinateSystem(double x, double y, double z)
            => this.head != null ? this.GetTargetOrientedCoordinateSystem(new Point3D(x, y, z), this.head.Origin) : new CoordinateSystem();

        /// <summary>
        /// Gets a coordinate system at a specified location oriented towards the head position.
        /// </summary>
        /// <param name="position">The position for the coordinate system.</param>
        /// <returns>A coordinate system at a specified location oriented towards the head position.</returns>
        public CoordinateSystem GetHeadOrientedCoordinateSystem(Point3D position)
            => this.head != null ? this.GetTargetOrientedCoordinateSystem(position, this.head.Origin) : new CoordinateSystem();

        /// <summary>
        /// Gets a coordinate system at a specified location oriented towards a specified position.
        /// </summary>
        /// <param name="position">The location for the coordinate system.</param>
        /// <param name="target">The target position towards which the coordinate system is oriented.</param>
        /// <returns>A coordinate system at a specified location oriented towards a specified position.</returns>
        public CoordinateSystem GetTargetOrientedCoordinateSystem(Point3D position, Point3D target)
        {
            var xAxis = (new Point3D(target.X, target.Y, target.Z) - position).Normalize();
            var yAxis = UnitVector3D.ZAxis.CrossProduct(xAxis);
            var zAxis = xAxis.CrossProduct(yAxis);
            return new CoordinateSystem(position, xAxis, yAxis, zAxis);
        }

        /// <summary>
        /// Gets or creates a material with a specified color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>The material with the specified color.</returns>
        public Material GetOrCreateMaterial(global::StereoKit.Color color)
        {
            if (!this.materials.ContainsKey(color))
            {
                this.materials.Add(color, Material.Default.Copy());
                this.materials[color][MatParamName.ColorTint] = color;
                this.materials[color].Wireframe = false;
            }

            return this.materials[color];
        }

        /// <summary>
        /// Gets or creates a material with a specified color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>A material with the specified color.</returns>
        public Material GetOrCreateMaterial(Color color) => this.GetOrCreateMaterial(color.ToStereoKitColor());

        /// <summary>
        /// Gets or creates a sphere mesh of a specified radius.
        /// </summary>
        /// <param name="radius">The radius for the sphere.</param>
        /// <returns>A sphere mesh of the specified radius.</returns>
        public Mesh GetOrCreateSphereMesh(float radius)
        {
            if (!this.sphereMeshes.ContainsKey(radius))
            {
                this.sphereMeshes.Add(radius, Mesh.GenerateSphere(radius));
            }

            return this.sphereMeshes[radius];
        }

        /// <summary>
        /// Gets or creates a circle mesh of a specified radius.
        /// </summary>
        /// <param name="outerRadius">The outer radius for the circle.</param>
        /// <param name="innerRadius">The inner radius for the circle.</param>
        /// <returns>A a circle mesh of the specified radius.</returns>
        public Mesh GetOrCreateCircleMesh(float outerRadius, float innerRadius)
        {
            // Create the circle mesh
            var circleCorners = new List<Point3D>();
            var circleIndices = new List<uint>();
            var thetaDiv = 40U;

            for (uint i = 0; i < thetaDiv; i++)
            {
                var angle = i * 2 * Math.PI / thetaDiv;
                var sin = (float)Math.Sin(angle);
                var cos = (float)Math.Cos(angle);
                circleCorners.Add(new Point3D(0, innerRadius * sin, innerRadius * cos));
                circleCorners.Add(new Point3D(0, outerRadius * sin, outerRadius * cos));
                if (i > 0)
                {
                    circleIndices.Add((i * 2) - 1);
                    circleIndices.Add((i - 1) * 2);
                    circleIndices.Add(i * 2);

                    circleIndices.Add((i * 2) + 1);
                    circleIndices.Add((i * 2) - 1);
                    circleIndices.Add(i * 2);
                }
            }

            circleIndices.Add(((thetaDiv - 1) * 2) + 1);
            circleIndices.Add((thetaDiv - 1) * 2);
            circleIndices.Add(0);

            circleIndices.Add(1);
            circleIndices.Add(((thetaDiv - 1) * 2) + 1);
            circleIndices.Add(0);

            var circleMesh = new Mesh();
            circleMesh.SetVerts(circleCorners.Select(v => new Vertex(v.ToVec3(), Vec3.One)).ToArray());
            circleMesh.SetInds(circleIndices.ToArray());

            return circleMesh;
        }

        /// <summary>
        /// Creates a mesh from a set of vertices and indices.
        /// </summary>
        /// <param name="vertices">The set of mesh vertices.</param>
        /// <param name="indices">The list of mesh indices.</param>
        /// <returns>The created mesh.</returns>
        public Mesh CreateMesh(List<Point3D> vertices, List<uint> indices)
        {
            var mesh = new Mesh();
            mesh.SetVerts(vertices.Select(v => new Vertex(v.ToVec3(), Vec3.One)).ToArray());
            mesh.SetInds(indices.ToArray());
            return mesh;
        }

        /// <summary>
        /// Gets or creates a text style of a specified color.
        /// </summary>
        /// <param name="color">The text color.</param>
        /// <returns>A text style of the specified color.</returns>
        public TextStyle GetOrCreateTextStyle(global::StereoKit.Color color)
            => this.GetOrCreateTextStyle(Font.Default, 0.02f, color);

        /// <summary>
        /// Gets or creates a text style.
        /// </summary>
        /// <param name="font">The font for the text style.</param>
        /// <param name="characterHeightMeters">The character height (in meters).</param>
        /// <param name="color">The text color.</param>
        /// <returns>The created text style.</returns>
        public TextStyle GetOrCreateTextStyle(Font font, float characterHeightMeters, global::StereoKit.Color color)
        {
            if (!this.textStyles.ContainsKey((font, characterHeightMeters, color)))
            {
                var textStyle = Text.MakeStyle(font, characterHeightMeters, color);
                this.textStyles.Add((font, characterHeightMeters, color), textStyle);
            }

            return this.textStyles[(font, characterHeightMeters, color)];
        }

        /// <summary>
        /// Renders a text.
        /// </summary>
        /// <param name="pose">The pose at which to render the text.</param>
        /// <param name="text">The text to render.</param>
        /// <param name="textStyle">The text style.</param>
        /// <param name="textAlign">The text alignment.</param>
        public void RenderText(CoordinateSystem pose, string text, TextStyle textStyle, TextAlign textAlign = TextAlign.Center)
            => Text.Add(text, pose.ToStereoKitMatrix(), textStyle, textAlign);

        /// <summary>
        /// Renders a mesh.
        /// </summary>
        /// <param name="pose">The pose at which to render the mesh.</param>
        /// <param name="mesh">The mesh.</param>
        /// <param name="material">The material.</param>
        public void RenderMesh(CoordinateSystem pose, Mesh mesh, Material material)
            => mesh.Draw(material, pose.ToStereoKitMatrix());

        /// <summary>
        /// Renders a line.
        /// </summary>
        /// <param name="startPoint">The start point for the line.</param>
        /// <param name="endPoint">The end point for the line.</param>
        /// <param name="lineThickness">The line thickness.</param>
        /// <param name="color">The line color.</param>
        public void RenderLine(Point3D startPoint, Point3D endPoint, float lineThickness, Color color)
            => Lines.Add(startPoint.ToVec3(), endPoint.ToVec3(), color.ToStereoKitColor(), lineThickness);

        /// <summary>
        /// Renders a line.
        /// </summary>
        /// <param name="startPoint">The start point for the line.</param>
        /// <param name="endPoint">The end point for the line.</param>
        /// <param name="lineThickness">The line thickness.</param>
        /// <param name="color">The line color.</param>
        public void RenderLine(Point3D startPoint, Point3D endPoint, float lineThickness, global::StereoKit.Color color)
            => Lines.Add(startPoint.ToVec3(), endPoint.ToVec3(), color, lineThickness);

        /// <summary>
        /// Renders a line.
        /// </summary>
        /// <param name="pose">A pose relative to which the start and end points are defined.</param>
        /// <param name="startPoint">The start point for the line.</param>
        /// <param name="endPoint">The end point for the line.</param>
        /// <param name="lineThickness">The line thickness.</param>
        /// <param name="color">The line color.</param>
        public void RenderLine(CoordinateSystem pose, Point3D startPoint, Point3D endPoint, float lineThickness, Color color)
            => Lines.Add(startPoint.TransformBy(pose).ToVec3(), endPoint.TransformBy(pose).ToVec3(), color.ToStereoKitColor(), lineThickness);

        /// <summary>
        /// Renders a polygon.
        /// </summary>
        /// <param name="pose">A pose relative to which the points are defined.</param>
        /// <param name="points">The set of points for the polygon vertices.</param>
        /// <param name="lineThickness">The line thickness.</param>
        /// <param name="color">The line color.</param>
        public void RenderPolygon(CoordinateSystem pose, List<Point3D> points, float lineThickness, Color color)
            => this.RenderPolygon(pose, points, lineThickness, color.ToStereoKitColor());

        /// <summary>
        /// Renders a polygon.
        /// </summary>
        /// <param name="pose">A pose relative to which the points are defined.</param>
        /// <param name="points">The set of points for the polygon vertices.</param>
        /// <param name="lineThickness">The line thickness.</param>
        /// <param name="color">The line color.</param>
        public void RenderPolygon(CoordinateSystem pose, List<Point3D> points, float lineThickness, global::StereoKit.Color color)
        {
            // Draw the slate borders
            for (int i = 0; i < points.Count - 1; i++)
            {
                Lines.Add(
                    points[i].TransformBy(pose).ToVec3(),
                    points[i + 1].TransformBy(pose).ToVec3(),
                    color,
                    lineThickness);
            }

            Lines.Add(
                points[points.Count - 1].TransformBy(pose).ToVec3(),
                points[0].TransformBy(pose).ToVec3(),
                color,
                lineThickness);
        }

        /// <summary>
        /// Renders a sphere.
        /// </summary>
        /// <param name="center">The center of the sphere.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="color">The color.</param>
        public void RenderSphere(Point3D center, float radius, Color color)
        {
            var material = this.GetOrCreateMaterial(color);
            var sphereMesh = this.GetOrCreateSphereMesh(radius);
            sphereMesh.Draw(material, new CoordinateSystem(center, UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis).ToStereoKitMatrix());
        }

        /// <summary>
        /// Renders a sphere.
        /// </summary>
        /// <param name="center">The center of the sphere.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="color">The color.</param>
        public void RenderSphere(Point3D center, float radius, global::StereoKit.Color color)
        {
            var material = this.GetOrCreateMaterial(color);
            var sphereMesh = this.GetOrCreateSphereMesh(radius);
            sphereMesh.Draw(material, new CoordinateSystem(center, UnitVector3D.XAxis, UnitVector3D.YAxis, UnitVector3D.ZAxis).ToStereoKitMatrix());
        }

        /// <summary>
        /// Renders a model.
        /// </summary>
        /// <param name="model">The model to render.</param>
        /// <param name="pose">The pose relative to which to render the model.</param>
        /// <param name="movable">A value indicating whether the model is movable by the user.</param>
        /// <param name="scalable">A value indicating whether the model is scalable by the user.</param>
        /// <param name="wireframe">A value indicating whether the model should be rendered as wireframe.</param>
        /// <param name="name">An optional name for the model.</param>
        public void RenderModel(Model model, ref CoordinateSystem pose, bool movable = false, bool scalable = false, bool wireframe = false, string name = null)
        {
            // Get the scale of the model (uniform scaling assumed)
            var scale = (float)pose.XAxis.Length;

            // Compute the stereokit pose (which does not include the scale preserved above)
            var stereoKitPose = pose.ToStereoKitPose();
            if (movable)
            {
                // If movable, create a handle with the stereokit pose and scaled dimensions
                UI.EnableFarInteract = true;
                name ??= Guid.NewGuid().ToString();
                UI.Handle(name, ref stereoKitPose, model.Bounds.Scaled(scale), false);
            }

            if (scalable)
            {
                // Create a UI slider for scaling the model
                var sliderPose = this.GetHeadOrientedCoordinateSystem(pose.Origin)
                    .TransformBy(CoordinateSystem.Translation(new Vector3D(0, 0, -0.15)))
                    .ToStereoKitPose();
                UI.WindowBegin("Scale", ref sliderPose, new Vec2(25 * U.cm, 0), UIWin.Body, UIMove.None);
                UI.HSlider("Scale", ref scale, 0, 5, 0.05f, confirmMethod: UIConfirm.Pinch);
                UI.WindowEnd();
            }

            // Retrieve the \psi basis pose (in case the user has moved the handle and/or scaled it)
            var modifiedStereoKitPose = stereoKitPose.ToMatrix(scale);
            pose = modifiedStereoKitPose.ToCoordinateSystem();

            // Setup wireframe rendering if necessary
            if (wireframe)
            {
                foreach (var visual in model.Visuals)
                {
                    visual.Material.Wireframe = true;
                }
            }

            // Render
            model.Draw(modifiedStereoKitPose);

            if (movable)
            {
                UI.EnableFarInteract = false;
            }
        }

        /// <summary>
        /// Renders a button.
        /// </summary>
        /// <param name="pose">The pose relative to which to render the button.</param>
        /// <param name="text">The text of the button.</param>
        /// <param name="action">The action to perform when the button is pressed.</param>
        public void RenderButton(CoordinateSystem pose, string text, Action action)
        {
            var stereoKitPose = pose.ToStereoKitPose();
            UI.WindowBegin(string.Empty, ref stereoKitPose, UIWin.Empty);
            if (UI.Button(text))
            {
                action?.Invoke();
            }

            UI.WindowEnd();
        }
    }
}

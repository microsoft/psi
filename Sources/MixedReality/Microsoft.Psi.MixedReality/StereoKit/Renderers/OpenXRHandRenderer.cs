// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality.StereoKit
{
    using System;
    using global::StereoKit;

    /// <summary>
    /// Component that visually renders a <see cref="OpenXR.Hand"/>.
    /// </summary>
    /// <remarks>
    /// Many of the constants and computations below were adapted from StereoKit code, particularly
    /// https://github.com/StereoKit/StereoKit/blob/8865d3a21d2d59a3ef16295d9fbc48cff33a0639/StereoKitC/hands/input_hand.cpp.
    /// </remarks>
    public class OpenXRHandRenderer : MeshRenderer, IConsumer<OpenXR.Hand>
    {
        // Constants needed for calculating the hand mesh values
        private const int SliceCount = 6;
        private const int RingCount = 7;
        private const int NumFingers = 5;
        private const int JointsPerFinger = 5;
        private const double Deg2Rad = Math.PI * 2.0 / 360.0;
        private static readonly float Sqrt2 = (float)Math.Sqrt(2);
        private static readonly float[] TexCoords = new float[6] { 1, 1 - 0.44f, 1 - 0.69f, 1 - 0.85f, 1 - 0.96f, 1 - 0.99f };

        private static readonly Vec3[] SinCos = new Vec3[7]
        {
            new Vec3((float)Math.Cos(Deg2Rad * 162), (float)Math.Sin(Deg2Rad * 162), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 90 ), (float)Math.Sin(Deg2Rad * 90 ), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 18 ), (float)Math.Sin(Deg2Rad * 18 ), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 18 ), (float)Math.Sin(Deg2Rad * 18 ), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 306), (float)Math.Sin(Deg2Rad * 306), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 234), (float)Math.Sin(Deg2Rad * 234), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 162), (float)Math.Sin(Deg2Rad * 162), 0),
        };

        private static readonly Vec3[] SinCosNorm = new Vec3[7]
        {
            new Vec3((float)Math.Cos(Deg2Rad * 126), (float)Math.Sin(Deg2Rad * 126), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 90 ), (float)Math.Sin(Deg2Rad * 90 ), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 54 ), (float)Math.Sin(Deg2Rad * 54 ), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 18 ), (float)Math.Sin(Deg2Rad * 18 ), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 306), (float)Math.Sin(Deg2Rad * 306), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 234), (float)Math.Sin(Deg2Rad * 234), 0),
            new Vec3((float)Math.Cos(Deg2Rad * 162), (float)Math.Sin(Deg2Rad * 162), 0),
        };

        private static readonly float[][] JointRadius = new float[5][]
        {
            // These joint radius values are hard-coded from values observed by the live hand tracker.
            new float[5] { 0.022382032f, 0.022382032f, 0.0149935745f, 0.0108900220f, 0.0084764630f },
            new float[5] { 0.026314713f, 0.013893547f, 0.0097728750f, 0.0078005140f, 0.0062052673f },
            new float[5] { 0.025847950f, 0.013303086f, 0.0094775240f, 0.0073014176f, 0.0070318560f },
            new float[5] { 0.023970472f, 0.012584036f, 0.0087478840f, 0.0075323120f, 0.0060640685f },
            new float[5] { 0.022030410f, 0.010670155f, 0.0081925210f, 0.0070020200f, 0.0055612730f },
        };

        private readonly Vertex[] handMeshVertices = new Vertex[(RingCount * SliceCount + 1) * NumFingers];
        private bool handActive = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenXRHandRenderer"/> class.
        /// </summary>
        /// <param name="pipeline">The pipeline to add the component to.</param>
        /// <param name="visible">Visibility.</param>
        /// <param name="name">An optional name for the component.</param>
        public OpenXRHandRenderer(Pipeline pipeline, bool visible = true, string name = nameof(OpenXRHandRenderer))
            : base(pipeline, new Mesh(), System.Drawing.Color.White, visible: visible, name: name)
        {
            this.In = pipeline.CreateReceiver<OpenXR.Hand>(this, this.UpdateMesh, nameof(this.In));
            this.Material = Default.MaterialHand.Copy();
            this.InitializeMesh();
        }

        /// <summary>
        /// Gets the receiver for the hand to render.
        /// </summary>
        public Receiver<OpenXR.Hand> In { get; private set; }

        /// <inheritdoc/>
        protected override void Render()
        {
            if (this.handActive)
            {
                base.Render();
            }
        }

        private void InitializeMesh()
        {
            // Much of this code is adapted from StereoKit.

            // Create and populate the array of face indices for the mesh
            var indCount = (3 * 5 * 2 * (SliceCount - 1) + (8 * 3)) * NumFingers;
            var handMeshIndices = new uint[indCount];

            int ind = 0;
            for (uint f = 0; f < NumFingers; f++)
            {
                uint startVert = f * (RingCount * SliceCount + 1);
                uint endVert = (f + 1) * (RingCount * SliceCount + 1) - (RingCount + 1);

                // start cap
                handMeshIndices[ind++] = startVert + 2;
                handMeshIndices[ind++] = startVert + 1;
                handMeshIndices[ind++] = startVert + 0;

                handMeshIndices[ind++] = startVert + 4;
                handMeshIndices[ind++] = startVert + 3;
                handMeshIndices[ind++] = startVert + 6;

                handMeshIndices[ind++] = startVert + 5;
                handMeshIndices[ind++] = startVert + 4;
                handMeshIndices[ind++] = startVert + 6;

                // tube faces
                for (uint j = 0; j < SliceCount - 1; j++)
                {
                    for (uint c = 0; c < RingCount - 1; c++)
                    {
                        if (c == 2)
                        {
                            c++;
                        }

                        uint curr1 = startVert + j * RingCount + c;
                        uint next1 = startVert + (j + 1) * RingCount + c;
                        uint curr2 = startVert + j * RingCount + c + 1;
                        uint next2 = startVert + (j + 1) * RingCount + c + 1;

                        handMeshIndices[ind++] = next2;
                        handMeshIndices[ind++] = next1;
                        handMeshIndices[ind++] = curr1;

                        handMeshIndices[ind++] = curr2;
                        handMeshIndices[ind++] = next2;
                        handMeshIndices[ind++] = curr1;
                    }
                }

                // end cap
                handMeshIndices[ind++] = endVert + 0;
                handMeshIndices[ind++] = endVert + 1;
                handMeshIndices[ind++] = endVert + 7;

                handMeshIndices[ind++] = endVert + 1;
                handMeshIndices[ind++] = endVert + 2;
                handMeshIndices[ind++] = endVert + 7;

                handMeshIndices[ind++] = endVert + 3;
                handMeshIndices[ind++] = endVert + 4;
                handMeshIndices[ind++] = endVert + 7;

                handMeshIndices[ind++] = endVert + 4;
                handMeshIndices[ind++] = endVert + 5;
                handMeshIndices[ind++] = endVert + 7;

                handMeshIndices[ind++] = endVert + 5;
                handMeshIndices[ind++] = endVert + 6;
                handMeshIndices[ind++] = endVert + 7;
            }

            // Generate uvs and colors for the mesh vertices
            int v = 0;
            for (int f = 0; f < NumFingers; f++)
            {
                float x = ((float)f / NumFingers) + (0.5f / NumFingers);
                for (int j = 0; j < SliceCount; j++)
                {
                    float y = TexCoords[f == 0 ? Math.Max(0, j - 1) : j];
                    var uv = new Vec2(x, y);
                    var gray = new Color32(200, 200, 200, 255);

                    this.handMeshVertices[v].uv = uv;
                    this.handMeshVertices[v++].col = Color32.White;
                    this.handMeshVertices[v].uv = uv;
                    this.handMeshVertices[v++].col = Color32.White;
                    this.handMeshVertices[v].uv = uv;
                    this.handMeshVertices[v++].col = Color32.White;

                    this.handMeshVertices[v].uv = uv;
                    this.handMeshVertices[v++].col = gray;
                    this.handMeshVertices[v].uv = uv;
                    this.handMeshVertices[v++].col = gray;
                    this.handMeshVertices[v].uv = uv;
                    this.handMeshVertices[v++].col = gray;
                    this.handMeshVertices[v].uv = uv;
                    this.handMeshVertices[v++].col = gray;
                }

                this.handMeshVertices[v].uv = new Vec2(x, 0);
                this.handMeshVertices[v++].col = Color32.White;
            }

            this.Mesh.SetInds(handMeshIndices);
        }

        private void UpdateMesh(OpenXR.Hand hand)
        {
            this.handActive = hand is not null && hand.IsActive;

            if (this.handActive)
            {
                // Convert to StereoKit poses for each finger joint
                var handJointPoses = new Pose[5][]
                {
                new Pose[5]
                {
                    hand.Joints[(int)HandJointIndex.ThumbMetacarpal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.ThumbMetacarpal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.ThumbProximal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.ThumbDistal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.ThumbTip].ToStereoKitPose(),
                },
                new Pose[5]
                {
                    hand.Joints[(int)HandJointIndex.IndexMetacarpal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.IndexProximal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.IndexIntermediate].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.IndexDistal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.IndexTip].ToStereoKitPose(),
                },
                new Pose[5]
                {
                    hand.Joints[(int)HandJointIndex.MiddleMetacarpal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.MiddleProximal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.MiddleIntermediate].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.MiddleDistal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.MiddleTip].ToStereoKitPose(),
                },
                new Pose[5]
                {
                    hand.Joints[(int)HandJointIndex.RingMetacarpal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.RingProximal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.RingIntermediate].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.RingDistal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.RingTip].ToStereoKitPose(),
                },
                new Pose[5]
                {
                    hand.Joints[(int)HandJointIndex.PinkyMetacarpal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.PinkyProximal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.PinkyIntermediate].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.PinkyDistal].ToStereoKitPose(),
                    hand.Joints[(int)HandJointIndex.PinkyTip].ToStereoKitPose(),
                },
                };

                // Update the mesh vertices accordingly
                this.UpdateHandMeshVertices(handJointPoses);
                this.Mesh.SetVerts(this.handMeshVertices);
            }
        }

        private void UpdateHandMeshVertices(Pose[][] handJointPoses)
        {
            // Much of this code is adapted from StereoKit.
            int v = 0;
            for (int f = 0; f < NumFingers; f++)
            {
                var tipPose = handJointPoses[f][JointsPerFinger - 1];
                var tipRadius = JointRadius[f][JointsPerFinger - 1];
                var tipFwd = tipPose.orientation * Vec3.Forward;
                var tipUp = tipPose.orientation * Vec3.Up;

                for (int j = 0; j < JointsPerFinger; j++)
                {
                    var pose = handJointPoses[f][j];
                    var radius = JointRadius[f][j];
                    var posePrev = handJointPoses[f][Math.Max(0, j - 1)];
                    var orientation = Quat.Slerp(posePrev.orientation, pose.orientation, 0.5f);

                    // Make local right and up axis vectors
                    var right = orientation * Vec3.Right;
                    var up = orientation * Vec3.Up;

                    // Find the scale for this joint
                    float scale = radius;
                    if (f == 0 && j < 2)
                    {
                        // thumb is too fat at the bottom
                        scale *= 0.5f;
                    }

                    // Use the local axis to create a ring of verts
                    for (int i = 0; i < RingCount; i++)
                    {
                        this.handMeshVertices[v].norm = (up * SinCosNorm[i].y + right * SinCosNorm[i].x) * Sqrt2;
                        this.handMeshVertices[v].pos = pose.position + (up * SinCos[i].y + right * SinCos[i].x) * scale;
                        v++;
                    }

                    // Last ring to blunt the fingertip
                    if (j == JointsPerFinger - 1)
                    {
                        scale *= 0.75f;
                        for (int i = 0; i < RingCount; i++)
                        {
                            Vec3 at = pose.position + tipFwd * radius * 0.65f;
                            this.handMeshVertices[v].norm = (up * SinCosNorm[i].y + right * SinCosNorm[i].x) * Sqrt2;
                            this.handMeshVertices[v].pos = at + (up * SinCos[i].y + right * SinCos[i].x) * scale + tipUp * radius * 0.25f;
                            v++;
                        }
                    }
                }

                this.handMeshVertices[v].norm = tipFwd;
                this.handMeshVertices[v].pos = tipPose.position + this.handMeshVertices[v].norm * tipRadius + tipUp * tipRadius * 0.9f;
                v++;
            }
        }
    }
}
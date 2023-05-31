// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Kinect
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Psi;
    using Microsoft.Psi.Calibration;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;

    /// <summary>
    /// Create Mesh.
    /// </summary>
    public class Mesh
    {
        private HalfEdge[] edges;
        private Face[] faces;
        private Vertex[] vertices;

        /// <summary>
        /// Gets number of mesh vertices.
        /// </summary>
        public int NumberVertices => this.Vertices.Length;

        /// <summary>
        /// Gets number of mesh faces.
        /// </summary>
        public int NumberFaces => this.Faces.Length;

        /// <summary>
        /// Gets or sets mesh vertices.
        /// </summary>
        public Vertex[] Vertices { get => this.vertices; set => this.vertices = value; }

        /// <summary>
        /// Gets or sets mesh faces.
        /// </summary>
        public Face[] Faces { get => this.faces; set => this.faces = value; }

        /// <summary>
        /// Gets or sets mesh edges.
        /// </summary>
        public HalfEdge[] Edges { get => this.edges; set => this.edges = value; }

        /// <summary>
        /// Create mesh from depth map.
        /// </summary>
        /// <param name="depthImage">Depth map image.</param>
        /// <param name="colorData">Color data image.</param>
        /// <param name="calib">Kinect calibration.</param>
        /// <returns>Mesh.</returns>
        public static Mesh MeshFromDepthMap(Shared<DepthImage> depthImage, Shared<Image> colorData, IDepthDeviceCalibrationInfo calib)
        {
            Mesh mesh = new Mesh();
            int width = depthImage.Resource.Width;
            int height = depthImage.Resource.Height;
            mesh.Vertices = new Vertex[width * height];
            bool[] vertexValid = new bool[width * height];
            mesh.Faces = new Face[2 * (width - 1) * (height - 1)];
            byte[] depthData = depthImage.Resource.ReadBytes(depthImage.Resource.Size);
            byte[] pixelData = colorData.Resource.ReadBytes(colorData.Resource.Size);
            int count = 0;
            unsafe
            {
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        ushort* src = (ushort*)((byte*)depthImage.Resource.ImageData.ToPointer() + (i * depthImage.Resource.Stride)) + j;
                        ushort depth = *src;
                        Point2D pt = new Point2D(j, i);
                        vertexValid[count] = (depth == 0) ? false : true;
                        mesh.Vertices[count].Pos = new Point3D(0.0, 0.0, 0.0);
                        mesh.Vertices[count].Color = new Point3D(0.0, 0.0, 0.0);
                        if (depth != 0)
                        {
                            // Determine vertex position+color via new calibration
                            Point2D newpt = new Point2D(pt.X, calib.DepthIntrinsics.ImageHeight - pt.Y);
                            Point3D p = calib.DepthIntrinsics.GetCameraSpacePosition(newpt, depth, depthImage.Resource.DepthValueSemantics, true);
                            mesh.Vertices[count].Pos = new Point3D(p.X / 1000.0, p.Y / 1000.0, p.Z / 1000.0);

                            Vector<double> pos = Vector<double>.Build.Dense(4);
                            pos[0] = mesh.Vertices[count].Pos.X;
                            pos[1] = mesh.Vertices[count].Pos.Y;
                            pos[2] = mesh.Vertices[count].Pos.Z;
                            pos[3] = 1.0;

                            pos = calib.ColorExtrinsics * pos;
                            Point3D clrPt = new Point3D(pos[0], pos[1], pos[2]);
                            if (calib.ColorIntrinsics.TryGetPixelPosition(clrPt, true, out var pixelCoord))
                            {
                                byte* pixel = ((byte*)colorData.Resource.ImageData.ToPointer() + ((int)pixelCoord.Y * colorData.Resource.Stride)) + (4 * (int)pixelCoord.X);
                                mesh.Vertices[count].Color = new Point3D((double)(int)*(pixel + 2), (double)(int)*(pixel + 1), (double)(int)*pixel);
                            }
                        }

                        count++;
                    }
                }
            }

            count = 0;

            // Create our edge list
            //
            // There are 6 edges per quad along with the edges
            // around the outside of the entire image (2*(width+height-2) edges)
            //     <------   <------
            //   X ------> X ------> X
            //  |^   0   //||   6   //||
            //  ||      // ||      // ||
            //  ||2   1//  ||8   7//  ||
            //  ||    //   ||    //   ||
            //  ||   //3  4||   //9 10||
            //  ||  //     ||  //     ||
            //  v| //  5   v| //  11  v|
            //   X <------ X <------ X
            //     ------>   ------>
            //  |^   12  //||   18  //||
            //  ||      // ||      // ||
            //  ||14 13//  ||20 19//  ||
            //  ||    // 16||    // 22||
            //  ||   //15  ||   //21  ||
            //  ||  //     ||  //     ||
            //  v| //  17  v| //  23  v|
            //   X <------ X <------ X
            //     ------>   ------>
            int edgeOffset = (width - 1) * (height - 1) * 6;
            int numEdges = edgeOffset + (2 * (width - 1)) + (2 * (height - 1));
            mesh.Edges = new HalfEdge[numEdges];
            for (int i = 0; i < numEdges; i++)
            {
                mesh.Edges[i] = default(HalfEdge);
            }

            int faceIndex = 0;
            int edgeIndex = 0;

            // Create our edge list
            for (int j = 0; j < height - 1; j++)
            {
                for (int i = 0; i < width - 1; i++)
                {
                    mesh.Faces[faceIndex] = default(Face);

                    mesh.Faces[faceIndex].Valid =
                        vertexValid[(j * width) + i + 1] &&
                        vertexValid[((j + 1) * width) + i] &&
                        vertexValid[(j * width) + i];

                    mesh.Edges[edgeIndex].Ccw = edgeIndex + 2;
                    mesh.Edges[edgeIndex].Cw = edgeIndex + 1;
                    mesh.Edges[edgeIndex].Face = faceIndex;
                    mesh.Edges[edgeIndex].Head = (j * width) + i + 1;
                    if (j == 0)
                    {
                        mesh.Edges[edgeIndex].Opp = edgeOffset + i;
                    }
                    else
                    {
                        mesh.Edges[edgeIndex].Opp = edgeIndex - (width * 6) + 5;
                    }

                    mesh.Edges[edgeIndex + 1].Ccw = edgeIndex;
                    mesh.Edges[edgeIndex + 1].Cw = edgeIndex + 2;
                    mesh.Edges[edgeIndex + 1].Face = faceIndex;
                    mesh.Edges[edgeIndex + 1].Head = ((j + 1) * width) + i;
                    mesh.Edges[edgeIndex + 1].Opp = edgeIndex + 3;

                    mesh.Edges[edgeIndex + 2].Ccw = edgeIndex + 1;
                    mesh.Edges[edgeIndex + 2].Cw = edgeIndex;
                    mesh.Edges[edgeIndex + 2].Face = faceIndex;
                    mesh.Edges[edgeIndex + 2].Head = (j * width) + i;
                    if (i == 0)
                    {
                        mesh.Edges[edgeIndex].Opp = edgeOffset + (width - 1) + j;
                    }
                    else
                    {
                        mesh.Edges[edgeIndex].Opp = edgeIndex - 4;
                    }

                    mesh.Faces[faceIndex].Edge = edgeIndex;
                    edgeIndex += 3;
                    faceIndex++;

                    mesh.Faces[faceIndex] = default(Face);

                    mesh.Faces[faceIndex].Valid =
                        vertexValid[(j * width) + i + 1] &&
                        vertexValid[((j + 1) * width) + i + 1] &&
                        vertexValid[((j + 1) * width) + i];

                    mesh.Edges[edgeIndex].Ccw = edgeIndex + 2;
                    mesh.Edges[edgeIndex].Cw = edgeIndex + 1;
                    mesh.Edges[edgeIndex].Face = faceIndex;
                    mesh.Edges[edgeIndex].Head = (j * width) + i + 1;
                    mesh.Edges[edgeIndex].Opp = edgeIndex - 2;

                    mesh.Edges[edgeIndex + 1].Ccw = edgeIndex;
                    mesh.Edges[edgeIndex + 1].Cw = edgeIndex + 2;
                    mesh.Edges[edgeIndex + 1].Face = faceIndex;
                    mesh.Edges[edgeIndex + 1].Head = ((j + 1) * width) + i + 1;
                    if (i == width - 1)
                    {
                        mesh.Edges[edgeIndex].Opp = edgeOffset + (width - 1) + (height - 1) + j;
                    }
                    else
                    {
                        mesh.Edges[edgeIndex].Opp = edgeIndex + 4;
                    }

                    mesh.Edges[edgeIndex + 2].Ccw = edgeIndex + 1;
                    mesh.Edges[edgeIndex + 2].Cw = edgeIndex;
                    mesh.Edges[edgeIndex + 2].Face = faceIndex;
                    mesh.Edges[edgeIndex + 2].Head = ((j + 1) * width) + i;
                    if (j == height - 1)
                    {
                        mesh.Edges[edgeIndex].Opp = edgeOffset + (width - 1) + (2 * (height - 1)) + i;
                    }
                    else
                    {
                        mesh.Edges[edgeIndex].Opp = edgeIndex + ((width - 1) * 6) - 5;
                    }

                    mesh.Faces[faceIndex].Edge = edgeIndex;
                    edgeIndex += 3;
                    faceIndex++;
                }
            }

            // Link up outer edges... first top edges
            int prevEdge = edgeOffset + width;
            int edge = edgeOffset;
            for (int i = 0; i < width - 1; i++)
            {
                mesh.Edges[edge].Cw = prevEdge;
                mesh.Edges[edge].Ccw = edge + 1;
                mesh.Edges[edge].Opp = i * 6;
                mesh.Edges[edge].Face = -1;
                mesh.Edges[edge].Head = i;
                prevEdge = edge;
                edge++;
            }

            // next the left edges
            prevEdge = edgeOffset;
            for (int i = 0; i < height - 1; i++)
            {
                mesh.Edges[edge].Cw = edge + 1;
                mesh.Edges[edge].Ccw = prevEdge;
                mesh.Edges[edge].Opp = (i * (width - 1) * 6) + 2;
                mesh.Edges[edge].Face = -1;
                mesh.Edges[edge].Head = width * (i + 1);
                prevEdge = edge;
            }

            // next the right edges
            prevEdge = edgeOffset + (width - 1);
            for (int i = 0; i < height - 1; i++)
            {
                mesh.Edges[edge].Ccw = edge + 1;
                mesh.Edges[edge].Cw = prevEdge;
                mesh.Edges[edge].Opp = (i * (width - 1) * 6) - 2;
                mesh.Edges[edge].Face = -1;
                mesh.Edges[edge].Head = (i * width) - 1;
                prevEdge = edge;
            }

            // finally the bottom edges
            prevEdge = edgeOffset + (width - 1) + (height - 1);
            for (int i = 0; i < width - 1; i++)
            {
                mesh.Edges[edge].Cw = edge + 1;
                mesh.Edges[edge].Ccw = prevEdge;
                mesh.Edges[edge].Opp = ((height - 2) * (width - 1) * 6) + (i * 6) + 5;
                mesh.Edges[edge].Face = -1;
                mesh.Edges[edge].Head = ((height - 1) * width) + i;
                prevEdge = edge;
            }

            return mesh;
        }

        /// <summary>
        /// Mesh face.
        /// </summary>
        public struct Face
        {
            private int edge; // index of one edge on the face
            private bool valid;

            /// <summary>
            /// Gets or sets face edge.
            /// </summary>
            public int Edge { get => this.edge; set => this.edge = value; }

            /// <summary>
            /// Gets or sets a value indicating whether face is valid.
            /// </summary>
            public bool Valid { get => this.valid; set => this.valid = value; }
        }

        /// <summary>
        /// Mesh vertex.
        /// </summary>
        public struct Vertex
        {
            private Point3D pos;
            private Point3D color;

            /// <summary>
            /// Gets or sets vertex position.
            /// </summary>
            public Point3D Pos { get => this.pos; set => this.pos = value; }

            /// <summary>
            /// Gets or sets vertex color.
            /// </summary>
            public Point3D Color { get => this.color; set => this.color = value; }
        }

        /// <summary>
        /// Mesh edge.
        /// </summary>
        public struct HalfEdge
        {
            private int ccw; // index of edge moving counter-clockwise
            private int cw; // index of edge moving clockwise
            private int opp; // index of opposite edge
            private int face; // index of face
            private int head; // index of head vertex

            /// <summary>
            /// Gets or sets counter-clockwise.
            /// </summary>
            public int Ccw { get => this.ccw; set => this.ccw = value; }

            /// <summary>
            /// Gets or sets clockwise.
            /// </summary>
            public int Cw { get => this.cw; set => this.cw = value; }

            /// <summary>
            /// Gets or sets opp.
            /// </summary>
            public int Opp { get => this.opp; set => this.opp = value; }

            /// <summary>
            /// Gets or sets face.
            /// </summary>
            public int Face { get => this.face; set => this.face = value; }

            /// <summary>
            /// Gets or sets head.
            /// </summary>
            public int Head { get => this.head; set => this.head = value; }
        }
    }
}

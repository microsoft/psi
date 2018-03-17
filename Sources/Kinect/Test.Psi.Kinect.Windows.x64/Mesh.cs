// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Kinect
{
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Kinect;
    using Microsoft.Psi;
    using Microsoft.Psi.Imaging;
    using Microsoft.Psi.Kinect;

    public class Mesh
    {
        public HalfEdge[] Edges;
        public Face[] Faces;
        public Vertex[] Vertices;

        public int NumberVertices => this.Vertices.Length;

        public int NumberFaces => this.Faces.Length;

        public static Mesh MeshFromDepthMap(Shared<Image> depthMap, Shared<Image> colorData, IKinectCalibration calib)
        {
            Mesh mesh = new Mesh();
            int width = depthMap.Resource.Width;
            int height = depthMap.Resource.Height;
            mesh.Vertices = new Vertex[width * height];
            bool[] vertexValid = new bool[width * height];
            mesh.Faces = new Face[2 * (width - 1) * (height - 1)];
            byte[] depthData = depthMap.Resource.ReadBytes(depthMap.Resource.Size);
            byte[] pixelData = colorData.Resource.ReadBytes(colorData.Resource.Size);
            int count = 0;
            unsafe
            {
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        ushort* src = (ushort*)((byte*)depthMap.Resource.ImageData.ToPointer() + (i * depthMap.Resource.Stride)) + j;
                        ushort depth = *src;
                        Point2D pt = new Point2D(j, i);
                        vertexValid[count] = (depth == 0) ? false : true;
                        mesh.Vertices[count].Pos = new Point3D(0.0, 0.0, 0.0);
                        mesh.Vertices[count].Color = new Point3D(0.0, 0.0, 0.0);
                        if (depth != 0)
                        {
                            DepthSpacePoint depthPt;
                            depthPt.X = j;
                            depthPt.Y = i;
                            Point2D pixelCoord;

                            // Determine vertex position+color via new calibration
                            Point2D newpt = new Point2D(pt.X, calib.DepthIntrinsics.ImageHeight - pt.Y);
                            Point3D p = calib.DepthIntrinsics.ToCameraSpace(newpt, depth, true);
                            mesh.Vertices[count].Pos = new Point3D(p.X / 1000.0, p.Y / 1000.0, p.Z / 1000.0);

                            Vector<double> pos = Vector<double>.Build.Dense(4);
                            pos[0] = mesh.Vertices[count].Pos.X;
                            pos[1] = mesh.Vertices[count].Pos.Y;
                            pos[2] = mesh.Vertices[count].Pos.Z;
                            pos[3] = 1.0;

                            pos = calib.ColorExtrinsics * pos;
                            Point3D clrPt = new Point3D(pos[0], pos[1], pos[2]);
                            pixelCoord = calib.ColorIntrinsics.ToPixelSpace(clrPt, true);
                            if (pixelCoord.X >= 0 && pixelCoord.X < colorData.Resource.Width &&
                                pixelCoord.Y >= 0 && pixelCoord.Y < colorData.Resource.Height)
                            {
                                byte* pixel = ((byte*)colorData.Resource.ImageData.ToPointer() + ((int)pixelCoord.Y * colorData.Resource.Stride)) + 4 * (int)pixelCoord.X;
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
            int numEdges = edgeOffset + 2 * (width - 1) + 2 * (height - 1);
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
                        vertexValid[j * width + i + 1] &&
                        vertexValid[(j + 1) * width + i] &&
                        vertexValid[j * width + i];

                    mesh.Edges[edgeIndex].Ccw = edgeIndex + 2;
                    mesh.Edges[edgeIndex].Cw = edgeIndex + 1;
                    mesh.Edges[edgeIndex].Face = faceIndex;
                    mesh.Edges[edgeIndex].Head = j * width + i + 1;
                    if (j == 0)
                    {
                        mesh.Edges[edgeIndex].Opp = edgeOffset + i;
                    }
                    else
                    {
                        mesh.Edges[edgeIndex].Opp = edgeIndex - width * 6 + 5;
                    }

                    mesh.Edges[edgeIndex + 1].Ccw = edgeIndex;
                    mesh.Edges[edgeIndex + 1].Cw = edgeIndex + 2;
                    mesh.Edges[edgeIndex + 1].Face = faceIndex;
                    mesh.Edges[edgeIndex + 1].Head = (j + 1) * width + i;
                    mesh.Edges[edgeIndex + 1].Opp = edgeIndex + 3;

                    mesh.Edges[edgeIndex + 2].Ccw = edgeIndex + 1;
                    mesh.Edges[edgeIndex + 2].Cw = edgeIndex;
                    mesh.Edges[edgeIndex + 2].Face = faceIndex;
                    mesh.Edges[edgeIndex + 2].Head = j * width + i;
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
                        vertexValid[j * width + i + 1] &&
                        vertexValid[(j + 1) * width + i + 1] &&
                        vertexValid[(j + 1) * width + i];

                    mesh.Edges[edgeIndex].Ccw = edgeIndex + 2;
                    mesh.Edges[edgeIndex].Cw = edgeIndex + 1;
                    mesh.Edges[edgeIndex].Face = faceIndex;
                    mesh.Edges[edgeIndex].Head = j * width + i + 1;
                    mesh.Edges[edgeIndex].Opp = edgeIndex - 2;

                    mesh.Edges[edgeIndex + 1].Ccw = edgeIndex;
                    mesh.Edges[edgeIndex + 1].Cw = edgeIndex + 2;
                    mesh.Edges[edgeIndex + 1].Face = faceIndex;
                    mesh.Edges[edgeIndex + 1].Head = (j + 1) * width + i + 1;
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
                    mesh.Edges[edgeIndex + 2].Head = (j + 1) * width + i;
                    if (j == height - 1)
                    {
                        mesh.Edges[edgeIndex].Opp = edgeOffset + (width - 1) + 2 * (height - 1) + i;
                    }
                    else
                    {
                        mesh.Edges[edgeIndex].Opp = edgeIndex + (width - 1) * 6 - 5;
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
                mesh.Edges[edge].Opp = i * (width - 1) * 6 + 2;
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
                mesh.Edges[edge].Opp = i * (width - 1) * 6 - 2;
                mesh.Edges[edge].Face = -1;
                mesh.Edges[edge].Head = i * width - 1;
                prevEdge = edge;
            }

            // finally the bottom edges
            prevEdge = edgeOffset + (width - 1) + (height - 1);
            for (int i = 0; i < width - 1; i++)
            {
                mesh.Edges[edge].Cw = edge + 1;
                mesh.Edges[edge].Ccw = prevEdge;
                mesh.Edges[edge].Opp = (height - 2) * (width - 1) * 6 + i * 6 + 5;
                mesh.Edges[edge].Face = -1;
                mesh.Edges[edge].Head = (height - 1) * width + i;
                prevEdge = edge;
            }

            return mesh;
        }

        public struct Face
        {
            public int Edge; // index of one edge on the face
            public bool Valid;
        }

        public struct Vertex
        {
            public Point3D Pos;
            public Point3D Color;
        }

        public struct HalfEdge
        {
            public int Ccw; // index of edge moving counter-clockwise
            public int Cw; // index of edge moving clockwise
            public int Opp; // index of opposite edge
            public int Face; // index of face
            public int Head; // index of head vertex
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using MathNet.Numerics.LinearAlgebra;

#pragma warning disable SA1600

    /// <summary>
    /// Defines a Matrix class
    /// </summary>
    internal class Matrix
    {
        private static double z0;
        private static double z1;
        private static bool generate = false;
        private static Random random = new Random();

        private int m;
        private int n;
        private int mn;
        private double[] data;

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> class.
        /// </summary>
        public Matrix()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> class.
        /// </summary>
        /// <param name="m">Number of rows in matrix</param>
        /// <param name="n">Number of columns in matrix</param>
        public Matrix(int m, int n)
        {
            this.m = m;
            this.n = n;
            this.mn = this.m * this.n;
            this.data = new double[this.mn];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> class.
        /// </summary>
        /// <param name="mat">Matrix to copy from</param>
        public Matrix(Matrix mat)
        {
            this.m = mat.m;
            this.n = mat.n;
            this.mn = this.m * this.n;
            this.data = new double[this.mn];
            this.Copy(mat);
        }

        /// <summary>
        /// Gets or sets a column of values in the matrix
        /// </summary>
        public double[][] ValuesByColumn
        {
            get
            {
                double[][] mat = new double[this.n][];

                for (int j = 0; j < this.n; j++)
                {
                    mat[j] = new double[this.m];
                }

                for (int i = 0; i < this.m; i++)
                {
                    for (int j = 0; j < this.n; j++)
                    {
                        mat[j][i] = this[i, j];
                    }
                }

                return mat;
            }

            set
            {
                double[][] mat = value;
                this.n = mat.Length;
                this.m = mat[0].Length;
                this.mn = this.m * this.n;
                this.data = new double[this.mn];
                for (int i = 0; i < this.m; i++)
                {
                    for (int j = 0; j < this.n; j++)
                    {
                        this[i, j] = mat[j][i];
                    }
                }
            }
        }

        /// <summary>
        /// Gets number of rows in the matrix
        /// </summary>
        public int Rows
        {
            get { return this.m; }
        }

        /// <summary>
        /// Gets number of columns in the matrix
        /// </summary>
        public int Cols
        {
            get { return this.n; }
        }

        /// <summary>
        /// Gets the total number of elements in the matrix
        /// </summary>
        public int Size
        {
            get { return this.mn; }
        }

        /// <summary>
        /// Indexer into the matrix
        /// </summary>
        /// <param name="i">Row to access</param>
        /// <param name="j">Column to access</param>
        /// <returns>The entry at specified row/column</returns>
        public double this[int i, int j]
        {
            get { return this.data[(i * this.n) + j]; }
            set { this.data[(i * this.n) + j] = value; }
        }

        /// <summary>
        /// Indexer that treats the matrix as a flat array
        /// </summary>
        /// <param name="i">Index to access</param>
        /// <returns>Value at Ith element in the matrix</returns>
        public double this[int i]
        {
            get { return this.data[i]; }
            set { this.data[i] = value; }
        }

        /// <summary>
        /// Returns an identity matrix
        /// </summary>
        /// <param name="m">Number of rows in matrix</param>
        /// <param name="n">Number of columns in matrix</param>
        /// <returns>New identity matrix of size MxN</returns>
        public static Matrix Identity(int m, int n)
        {
            var mat = new Matrix(m, n);
            mat.Identity();
            return mat;
        }

        /// <summary>
        /// Returns an zero matrix
        /// </summary>
        /// <param name="m">Number of rows in matrix</param>
        /// <param name="n">Number of columns in matrix</param>
        /// <returns>New zero matrix of size MxN</returns>
        public static Matrix Zero(int m, int n)
        {
            var mat = new Matrix(m, n);
            mat.Zero();
            return mat;
        }

        /// <summary>
        /// Copies a submatrix from matA into matB
        /// </summary>
        /// <param name="matA">Matrix to copy from</param>
        /// <param name="ai">Row offset to start copying from</param>
        /// <param name="aj">Column offset to start copying from</param>
        /// <param name="m">Number of rows to copy</param>
        /// <param name="n">Number of columns to copy</param>
        /// <param name="matB">Matrix to copy to</param>
        /// <param name="bi">Row offset to copy to</param>
        /// <param name="bj">Column offset to copy to</param>
        public static void CopyRange(Matrix matA, int ai, int aj, int m, int n, Matrix matB, int bi, int bj)
        {
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matB[bi + i, bj + j] = matA[ai + i, aj + j];
                }
            }
        }

        /// <summary>
        /// Copies a single row from matA to matB
        /// </summary>
        /// <param name="matA">Matrix to copy from</param>
        /// <param name="row">Row to copy</param>
        /// <param name="matB">Matrix to copy to</param>
        public static void CopyRow(Matrix matA, int row, Matrix matB)
        {
            for (int j = 0; j < matA.n; j++)
            {
                matB.data[j] = matA[row, j];
            }
        }

        /// <summary>
        /// Copies a single column from matA to matB
        /// </summary>
        /// <param name="matA">Matrix to copy from</param>
        /// <param name="col">Column to copy</param>
        /// <param name="matB">Matrix to copy to</param>
        public static void CopyCol(Matrix matA, int col, Matrix matB)
        {
            for (int i = 0; i < matA.m; i++)
            {
                matB.data[i] = matA[i, col];
            }
        }

        /// <summary>
        /// Copies the diagonal from matA to matB
        /// </summary>
        /// <param name="matA">Matrix to copy from</param>
        /// <param name="matB">Matrix to copy to</param>
        public static void CopyDiag(Matrix matA, Matrix matB)
        {
            int maxd = (matA.m > matA.n) ? matA.m : matA.n;
            for (int i = 0; i < maxd; i++)
            {
                matB.data[i] = matA[i, i];
            }
        }

        // equals
        public static bool Equals(Matrix matA, Matrix matB)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                if (matA.data[i] != matB.data[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static void Reshape(Matrix matA, Matrix matB)
        {
            int k = 0;
            for (int i = 0; i < matA.m; i++)
            {
                for (int j = 0; j < matA.n; j++)
                {
                    matB.data[k++] = matA[i, j];
                }
            }
        }

        // change shape
        public static void Transpose(Matrix matA, Matrix matB)
        {
            if (matA != matB)
            {
                for (int i = 0; i < matA.m; i++)
                {
                    for (int j = 0; j < matA.n; j++)
                    {
                        matB[j, i] = matA[i, j];
                    }
                }
            }
            else
            { // must be square
                double s;
                for (int i = 0; i < matA.m; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        s = matA[i, j];
                        matA[i, j] = matA[j, i];
                        matA[j, i] = s;
                    }
                }
            }
        }

        // matrix-scalar ops
        public static void Identity(Matrix matA)
        {
            for (int i = 0; i < matA.m; i++)
            {
                for (int j = 0; j < matA.n; j++)
                {
                    if (i == j)
                    {
                        matA[i, j] = 1.0;
                    }
                    else
                    {
                        matA[i, j] = 0.0;
                    }
                }
            }
        }

        public static void Set(Matrix matA, double c)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matA.data[i] = c;
            }
        }

        public static void Pow(Matrix matA, double c, Matrix matB)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matB.data[i] = Math.Pow(matA.data[i], c);
            }
        }

        public static void Exp(Matrix matA, Matrix matB)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matB.data[i] = Math.Exp(matA.data[i]);
            }
        }

        public static void Log(Matrix matA, Matrix matB)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matB.data[i] = Math.Log(matA.data[i]);
            }
        }

        public static void Abs(Matrix matA, Matrix matB)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matB.data[i] = Math.Abs(matA.data[i]);
            }
        }

        public static void Add(Matrix matA, double c, Matrix matB)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matB.data[i] = c + matA.data[i];
            }
        }

        public static void Scale(Matrix matA, double c, Matrix matB)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matB.data[i] = c * matA.data[i];
            }
        }

        public static void ScaleAdd(Matrix matA, double c, Matrix matB)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matB.data[i] += c * matA.data[i];
            }
        }

        public static void Reciprocal(Matrix matA, Matrix matB)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matB.data[i] = 1.0 / matA.data[i];
            }
        }

        public static void Bound(Matrix matA, Matrix matB, Matrix matC)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                if (matC.data[i] < matA.data[i])
                {
                    matC.data[i] = matA.data[i];
                }

                if (matC.data[i] > matB.data[i])
                {
                    matC.data[i] = matB.data[i];
                }
            }
        }

        public static void Add(Matrix matA, Matrix matB, Matrix matC)
        {
            for (int i = 0; i < matA.Size; i++)
            {
                matC.data[i] = matA.data[i] + matB.data[i];
            }
        }

        public static void Sub(Matrix matA, Matrix matB, Matrix matC)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matC.data[i] = matA.data[i] - matB.data[i];
            }
        }

        public static void ElemMult(Matrix matA, Matrix matB, Matrix matC)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matC.data[i] = matA.data[i] * matB.data[i];
            }
        }

        public static void Divide(Matrix matA, Matrix matB, Matrix matC)
        {
            for (int i = 0; i < matA.mn; i++)
            {
                matC.data[i] = matA.data[i] / matB.data[i];
            }
        }

        public static double Dot(Matrix matA, Matrix matB)
        {
            double sum = 0.0;
            for (int i = 0; i < matA.mn; i++)
            {
                sum += matA.data[i] * matB.data[i];
            }

            return sum;
        }

        public static void Outer(Matrix matA, Matrix matB, Matrix matC)
        {
            for (int i = 0; i < matC.m; i++)
            {
                for (int j = 0; j < matC.n; j++)
                {
                    matC[i, j] = matA.data[i] * matB.data[j];
                }
            }
        }

        public static void Cross(Matrix matA, Matrix matB, Matrix matC)
        {
            matC.data[0] = (matA.data[1] * matB.data[2]) - (matA.data[2] * matB.data[1]);
            matC.data[1] = (matA.data[2] * matB.data[0]) - (matA.data[0] * matB.data[2]);
            matC.data[2] = (matA.data[0] * matB.data[1]) - (matA.data[1] * matB.data[0]);
        }

        public static void Mult(Matrix matA, Matrix matB, Matrix matC)
        {
            for (int i = 0; i < matA.m; i++)
            {
                for (int j = 0; j < matB.n; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < matA.n; k++)
                    {
                        sum += matA[i, k] * matB[k, j];
                    }

                    matC[i, j] = sum;
                }
            }
        }

        public static void MultATA(Matrix matA, Matrix matB, Matrix matC)
        {
            for (int i = 0; i < matA.n; i++)
            { // matA.m
                for (int j = 0; j < matB.n; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < matA.m; k++)
                    { // matA.n
                        sum += matA[k, i] * matB[k, j];
                    }

                    matC[i, j] = sum;
                }
            }
        }

        public static void MultATAParallel(Matrix matA, Matrix matB, Matrix matC)
        {
            Parallel.For(0, matA.n, i =>
            {
                for (int j = 0; j < matB.n; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < matA.m; k++)
                    { // matA.n
                        sum += matA[k, i] * matB[k, j];
                    }
                    matC[i, j] = sum;
                }
            });
        }

        public static Matrix<double> ToMathNet(Matrix matA)
        {
            var matB = Matrix<double>.Build.Dense(matA.Rows, matA.Cols);
            for (int i = 0; i < matA.Rows; i++)
            {
                for (int j = 0; j < matA.Cols; j++)
                {
                    matB[i, j] = matA[i, j];
                }
            }

            return matB;
        }

        public static void FromMathNet(Matrix<double> matA, Matrix matB)
        {
            for (int i = 0; i < matA.RowCount; i++)
            {
                for (int j = 0; j < matA.ColumnCount; j++)
                {
                    matB[i, j] = matA[i, j];
                }
            }
        }

        public static void FromMathNet(Vector<double> matA, Matrix matB)
        {
            for (int i = 0; i < matA.Count; i++)
            {
                matB[i] = matA[i];
            }
        }

        public static double Det3x3(Matrix matA)
        {
            double a = matA[0, 0];
            double b = matA[0, 1];
            double c = matA[0, 2];
            double d = matA[1, 0];
            double e = matA[1, 1];
            double f = matA[1, 2];
            double g = matA[2, 0];
            double h = matA[2, 1];
            double i = matA[2, 2];

            return ((a * e * i) + (b * f * g) + (c * d * h)) - ((c * e * g) + (b * d * i) + (a * f * h));
        }

        public static void LeastSquares(Matrix x, Matrix matA, Matrix matB)
        {
            // use svd
            // for overdetermined systems A*x = b
            // x = V * diag(1/wj) * U T * b
            // NRC p. 66
            int m = matA.m;
            int n = matA.n;

            Matrix matU = new Matrix(m, n);
            Matrix matV = new Matrix(n, n);
            Matrix w = new Matrix(n, 1);
            Matrix matW = new Matrix(n, n);
            matA.SVD(matU, w, matV);
            w.Reciprocal();
            matW.Diag(w);

            Matrix matM = new Matrix(n, n);
            matM.Mult(matV, matW);

            Matrix matN = new Matrix(n, m);
            matN.MultAAT(matM, matU);

            x.Mult(matN, matB);
        }

        public static void Rot2D(Matrix matA, double theta)
        {
            // clockwise rotation
            double s = Math.Sin(theta);
            double c = Math.Cos(theta);
            matA[0, 0] = c;
            matA[1, 0] = s;
            matA[0, 1] = -s;
            matA[1, 1] = c;
        }

        public static void RotEuler2Matrix(double x, double y, double z, Matrix matA)
        {
            double s1 = Math.Sin(x);
            double s2 = Math.Sin(y);
            double s3 = Math.Sin(z);
            double c1 = Math.Cos(x);
            double c2 = Math.Cos(y);
            double c3 = Math.Cos(z);

            matA[0, 0] = c3 * c2;
            matA[0, 1] = (-s3 * c1) + (c3 * s2 * s1);
            matA[0, 2] = (s3 * s1) + (c3 * s2 * c1);
            matA[1, 0] = s3 * c2;
            matA[1, 1] = (c3 * c1) + (s3 * s2 * s1);
            matA[1, 2] = (-c3 * s1) + (s3 * s2 * c1);
            matA[2, 0] = -s2;
            matA[2, 1] = c2 * s1;
            matA[2, 2] = c2 * c1;
        }

        public static void RotFromTo2Quat(Matrix x, Matrix y, Matrix q)
        {
            Matrix axis = new Matrix(3, 1);
            axis.Cross(y, x);
            axis.Normalize();

            double angle = Math.Acos(x.Dot(y));
            double s = Math.Sin(angle / 2.0);

            q[0] = axis[0] * s;
            q[1] = axis[1] * s;
            q[2] = axis[2] * s;
            q[3] = Math.Cos(angle / 2.0);
        }

        public static void RotQuat2Matrix(Matrix q, Matrix matA)
        {
            double x = q[0];
            double y = q[1];
            double z = q[2];
            double w = q[3];

            // Watt and Watt p. 363
            double s = 2.0 / Math.Sqrt((x * x) + (y * y) + (z * z) + (w * w));

            double xs = x * s;
            double ys = y * s;
            double zs = z * s;
            double wx = w * xs;
            double wy = w * ys;
            double wz = w * zs;
            double xx = x * xs;
            double xy = x * ys;
            double xz = x * zs;
            double yy = y * ys;
            double yz = y * zs;
            double zz = z * zs;

            matA[0, 0] = 1 - (yy + zz);
            matA[0, 1] = xy + wz;
            matA[0, 2] = xz - wy;

            matA[1, 0] = xy - wz;
            matA[1, 1] = 1 - (xx + zz);
            matA[1, 2] = yz + wx;

            matA[2, 0] = xz + wy;
            matA[2, 1] = yz - wx;
            matA[2, 2] = 1 - (xx + yy);
        }

        public static void RotAxisAngle2Quat(Matrix axis, double angle, Matrix q)
        {
            q[0] = axis[0] * Math.Sin(angle / 2.0);
            q[1] = axis[1] * Math.Sin(angle / 2.0);
            q[2] = axis[2] * Math.Sin(angle / 2.0);
            q[3] = Math.Cos(angle / 2.0);
        }

        public static void RotMatrix2Quat(Matrix matA, Matrix q)
        {
            // Watt and Watt p. 362
            double trace = matA[0, 0] + matA[1, 1] + matA[2, 2] + 1.0;
            q[3] = Math.Sqrt(trace);

            q[0] = (matA[2, 1] - matA[1, 2]) / (4 * q[3]);
            q[1] = (matA[0, 2] - matA[2, 0]) / (4 * q[3]);
            q[2] = (matA[1, 0] - matA[0, 1]) / (4 * q[3]);

            // not tested
        }

        public static void RotMatrix2Euler(Matrix matA, ref double x, ref double y, ref double z)
        {
            y = -Math.Asin(matA[2, 0]);
            double c = Math.Cos(y);

            double cost3 = matA[0, 0] / c;
            double sint3 = matA[1, 0] / c;
            z = Math.Atan2(sint3, cost3);

            double sint1 = matA[2, 1] / c;
            double cost1 = matA[2, 2] / c;
            x = Math.Atan2(sint1, cost1);
        }

        public static void QuatMult(Matrix matA, Matrix matB, Matrix matC)
        {
            Matrix v1 = new Matrix(3, 1);
            Matrix v2 = new Matrix(3, 1);
            Matrix v3 = new Matrix(3, 1);

            v1[0] = matA[0];
            v1[1] = matA[1];
            v1[2] = matA[2];
            double s1 = matA[3];

            v2[0] = matB[0];
            v2[1] = matB[1];
            v2[2] = matB[2];
            double s2 = matB[3];

            v3.Cross(v1, v2);

            matC[0] = (s1 * v2[0]) + (s2 * v1[0]) + v3[0];
            matC[1] = (s1 * v2[1]) + (s2 * v1[1]) + v3[1];
            matC[2] = (s1 * v2[2]) + (s2 * v1[2]) + v3[2];
            matC[3] = (s1 * s2) - v1.Dot(v2);
        }

        public static void QuatInvert(Matrix matA, Matrix matB)
        {
            matB[0] = -matA[0];
            matB[1] = -matA[1];
            matB[2] = -matA[2];
            matB[3] = matA[3]; // w
        }

        public static void QuatRot(Matrix q, Matrix x, Matrix y)
        {
            // p. 361 Watt and Watt
            Matrix p = new Matrix(4, 1);
            p[0] = x[0];
            p[1] = x[1];
            p[2] = x[2];
            p[3] = 0.0;

            Matrix q1 = new Matrix(4, 1);
            Matrix q2 = new Matrix(4, 1);
            Matrix qi = new Matrix(4, 1);

            qi.QuatInvert(q);

            q1.QuatMult(q, p);
            q2.QuatMult(q1, qi);

            y[0] = q2[0];
            y[1] = q2[1];
            y[2] = q2[2];
        }

        public static double L1distance(Matrix matA, Matrix matB)
        {
            double s = 0.0;
            double d;
            for (int i = 0; i < matA.mn; i++)
            {
                d = matA.data[i] - matB.data[i];
                s += Math.Abs(d);
            }

            return s;
        }

        public static double L2distance(Matrix matA, Matrix matB)
        {
            double s = 0.0;
            double d;
            for (int i = 0; i < matA.mn; i++)
            {
                d = matA.data[i] - matB.data[i];
                s += d * d;
            }

            return Math.Sqrt(s);
        }

        public static void Normalize(Matrix matA, Matrix matB)
        {
            matB.Scale(matA, 1.0 / matA.Norm());
        }

        public static Matrix GaussianSample(int m, int n)
        {
            var matA = new Matrix(m, n);
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matA[i, j] = NextGaussianSample(0, 1);
                }
            }

            return matA;
        }

        public static Matrix GaussianSample(Matrix mu, double sigma)
        {
            int m = mu.Rows;
            int n = mu.Cols;

            var matA = new Matrix(m, n);
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    matA[i, j] = NextGaussianSample(mu[i, j], sigma);
                }
            }

            return matA;
        }

        public static double NextGaussianSample(double mu, double sigma)
        {
            // Box-Muller transform
            const double epsilon = double.MinValue;
            const double tau = 2.0 * Math.PI;

            generate = !generate;
            if (!generate)
            {
                return (z1 * sigma) + mu;
            }

            double u1, u2;
            do
            {
                u1 = random.NextDouble();
                u2 = random.NextDouble();
            }
            while (u1 <= epsilon);

            z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(tau * u2);
            z1 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(tau * u2);
            return (z0 * sigma) + mu;
        }

        public static void Test()
        {
            Matrix matA = new Matrix(2, 2);
            matA[0, 0] = 1.0;
            matA[0, 1] = 2.0;
            matA[1, 0] = 3.0;
            matA[1, 1] = 4.0;

            Console.WriteLine(matA.ToString());

            // test serialization
            XmlSerializer serializer = new XmlSerializer(typeof(Matrix));
            TextWriter writer = new StreamWriter("test.xml");

            serializer.Serialize(writer, matA);
            writer.Close();

            XmlSerializer deserializer = new XmlSerializer(typeof(Matrix));
            TextReader reader = new StreamReader("test.xml");
            Matrix matA2 = (Matrix)deserializer.Deserialize(reader);

            Console.WriteLine(matA2);
        }

        public static void MultAAT(Matrix matA, Matrix matB, Matrix matC)
        {
            for (int i = 0; i < matA.m; i++)
            {
                for (int j = 0; j < matB.m; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < matA.n; k++)
                    {
                        sum += matA[i, k] * matB[j, k];
                    }

                    matC[i, j] = sum;
                }
            }
        }

        public float[] AsFloatArray()
        {
            float[] array = new float[this.mn];
            for (int i = 0; i < this.mn; i++)
            {
                array[i] = (float)this.data[i];
            }

            return array;
        }

        // copy
        public void Copy(Matrix mat)
        {
            for (int i = 0; i < this.m; i++)
            {
                for (int j = 0; j < this.n; j++)
                {
                    this[i, j] = mat[i, j];
                }
            }
        }

        public void Copy(int bi, int bj, Matrix mat)
        {
            CopyRange(mat, 0, 0, mat.Rows, mat.Cols, this, bi, bj);
        }

        public void Copy(int bi, int bj, Matrix mat, int ai, int aj, int rows, int cols)
        {
            CopyRange(mat, ai, aj, rows, cols, this, bi, bj);
        }

        public void CopyRow(Matrix matA, int row)
        {
            CopyRow(matA, row, this);
        }

        public void CopyCol(Matrix matA, int col)
        {
            CopyCol(matA, col, this);
        }

        public void CopyDiag(Matrix matA)
        {
            CopyDiag(matA, this);
        }

        public void Diag(Matrix matA, Matrix d)
        {
            matA.Zero();
            for (int i = 0; i < matA.m; i++)
            {
                matA[i, i] = d[i];
            }
        }

        public void Diag(Matrix d)
        {
            this.Diag(this, d);
        }

        public bool Equals(Matrix matA)
        {
            return Equals(matA, this);
        }

        public void Transpose(Matrix matA)
        {
            Transpose(matA, this);
        }

        public void Transpose()
        {
            Transpose(this, this);
        }

        public void Reshape(Matrix matA)
        {
            Reshape(matA, this);
        }

        public void Identity()
        {
            Identity(this);
        }

        public void Set(double c)
        {
            Set(this, c);
        }

        public void Zero()
        {
            this.Set(0.0);
        }

        public void Randomize()
        {
            System.Random rnd = new System.Random();
            for (int i = 0; i < this.mn; i++)
            {
                this.data[i] = rnd.NextDouble();
            }
        }

        public void Linspace(double x0, double x1)
        {
            double dx = (x1 - x0) / (double)(this.mn - 1);
            for (int i = 0; i < this.mn; i++)
            {
                this.data[i] = x0 + (dx * i);
            }
        }

        public void Pow(Matrix matA, double c)
        {
            Pow(matA, c, this);
        }

        public void Pow(double c)
        {
            Pow(this, c, this);
        }

        public void Exp(Matrix matA)
        {
            Exp(matA, this);
        }

        public void Exp()
        {
            Exp(this, this);
        }

        public void Log(Matrix matA)
        {
            Log(matA, this);
        }

        public void Log()
        {
            Log(this, this);
        }

        public void Abs(Matrix matA)
        {
            Abs(matA, this);
        }

        public void Abs()
        {
            Abs(this, this);
        }

        public void Add(Matrix matA, double c)
        {
            Add(matA, c, this);
        }

        public void Add(double c)
        {
            Add(this, c, this);
        }

        public void Scale(Matrix matA, double c)
        {
            Scale(matA, c, this);
        }

        public void Scale(double c)
        {
            Scale(this, c, this);
        }

        public void ScaleAdd(Matrix matA, double c)
        {
            ScaleAdd(matA, c, this);
        }

        public void ScaleAdd(double c)
        {
            ScaleAdd(this, c, this);
        }

        public void Reciprocal(Matrix matA)
        {
            Reciprocal(matA, this);
        }

        public void Reciprocal()
        {
            Reciprocal(this, this);
        }

        // limits data between elements of A and B
        public void Bound(Matrix matA, Matrix matB)
        {
            Bound(matA, matB, this);
        }

        // matrix-matrix elementwise ops
        public void Add(Matrix matA, Matrix matB)
        {
            Add(matA, matB, this);
        }

        public void Add(Matrix matB)
        {
            Add(this, matB, this);
        }

        public void Sub(Matrix matA, Matrix matB)
        {
            Sub(matA, matB, this);
        }

        public void Sub(Matrix matB)
        {
            Sub(this, matB, this);
        }

        public void ElemMult(Matrix matA, Matrix matB)
        {
            ElemMult(matA, matB, this);
        }

        public void ElemMult(Matrix matB)
        {
            ElemMult(this, matB, this);
        }

        public void Divide(Matrix matA, Matrix matB)
        {
            Divide(matA, matB, this);
        }

        public void Divide(Matrix matB)
        {
            Divide(this, matB, this);
        }

        // vector ops
        public double Dot(Matrix matB)
        {
            return Dot(this, matB);
        }

        public void Outer(Matrix matA, Matrix matB)
        {
            Outer(matA, matB, this);
        }

        public void Cross(Matrix matA, Matrix matB)
        {
            Cross(matA, matB, this);
        }

        // matrix-matrix ops
        public void Mult(Matrix matA, Matrix matB)
        {
            Mult(matA, matB, this);
        }

        public void MultAAT(Matrix matA, Matrix matB)
        {
            MultAAT(matA, matB, this);
        }

        public void MultATA(Matrix matA, Matrix matB)
        {
            MultATA(matA, matB, this);
        }

        public void MultATAParallel(Matrix matA, Matrix matB)
        {
            MultATAParallel(matA, matB, this);
        }

        public void Inverse(Matrix matA)
        {
            // invert A and store in this
            var anet = ToMathNet(matA);
            var inverse = anet.Inverse();
            FromMathNet(inverse, this);
        }

        public double Det3x3()
        {
            return Det3x3(this);
        }

        public void Eig(Matrix v, Matrix d)
        {
            var evd = ToMathNet(this).Evd();
            FromMathNet(evd.EigenVectors, v);
            for (int i = 0; i < this.Rows; i++)
            {
                d[i] = evd.D[i, i];
            }
        }

        public void Eig2x2(Matrix matA, Matrix v, Matrix matD)
        {
            double a = matA[0, 0];
            double b = matA[0, 1];
            double c = matA[1, 0];
            double d = matA[1, 1];

            // solve det(A - l*I) = 0 for eigenvalues l
            double s = Math.Sqrt(((a + d) * (a + d)) + (4 * ((b * c) - (a * d))));
            matD[0] = (a + d + s) / 2;
            matD[1] = (a + d - s) / 2;

            // solve for eigenvectors v in (A - l*I)*v = 0 for each eigenvalue
            // set v1 = 1.0
            double v0, n;

            // first eigenvector
            v0 = (matD[0] - d) / c;
            n = Math.Sqrt((v0 * v0) + 1);

            v[0, 0] = v0 / n;
            v[1, 0] = 1.0 / n;

            // second eigenvector
            v0 = (matD[1] - d) / c;
            n = Math.Sqrt((v0 * v0) + 1);

            v[0, 1] = v0 / n;
            v[1, 1] = 1.0 / n;
        }

        public void Eig2x2(Matrix v, Matrix d)
        {
            this.Eig2x2(this, v, d);
        }

        public void SVD(Matrix matU, Matrix w, Matrix matV)
        {
            var svd = ToMathNet(this).Svd();
            FromMathNet(svd.U, matU);
            FromMathNet(svd.S, w);
            FromMathNet(svd.VT.Transpose(), matV);
        }

        public void LeastSquares(Matrix matA, Matrix matB)
        {
            LeastSquares(this, matA, matB);
        }

        // rotation conversions
        public void Rot2D(double theta)
        {
            Rot2D(this, theta);
        }

        public void RotEuler2Matrix(double x, double y, double z)
        {
            RotEuler2Matrix(x, y, z, this);
        }

        public void RotFromTo2Quat(Matrix x, Matrix y)
        {
            RotFromTo2Quat(x, y, this);
        }

        public void RotQuat2Matrix(Matrix q)
        {
            RotQuat2Matrix(q, this);
        }

        public void RotAxisAngle2Quat(Matrix axis, double angle)
        {
            RotAxisAngle2Quat(axis, angle, this);
        }

        public void RotMatrix2Quat(Matrix matA)
        {
            RotMatrix2Quat(matA, this);
        }

        public void RotMatrix2Euler(ref double x, ref double y, ref double z)
        {
            RotMatrix2Euler(this, ref x, ref y, ref z);
        }

        // quaternion ops; quat is ((X, Y, Z), W)
        public void QuatMult(Matrix matA, Matrix matB)
        {
            QuatMult(matA, matB, this);
        }

        public void QuatInvert(Matrix matA)
        {
            QuatInvert(matA, this);
        }

        public void QuatInvert()
        {
            QuatInvert(this, this);
        }

        public void QuatRot(Matrix q, Matrix x)
        {
            QuatRot(q, x, this);
        }

        // norms
        public double Minimum(ref int argmin)
        {
            double min = this.data[0];
            int mini = 0;
            for (int i = 1; i < this.mn; i++)
            {
                if (this.data[i] < min)
                {
                    min = this.data[i];
                    mini = i;
                }
            }

            argmin = mini;
            return min;
        }

        public double Maximum(ref int argmax)
        {
            double max = this.data[0];
            int maxi = 0;
            for (int i = 1; i < this.mn; i++)
            {
                if (this.data[i] > max)
                {
                    max = this.data[i];
                    maxi = i;
                }
            }

            argmax = maxi;
            return max;
        }

        public double Norm()
        {
            double sum = 0;
            for (int i = 0; i < this.mn; i++)
            {
                sum += this.data[i] * this.data[i];
            }

            return Math.Sqrt(sum);
        }

        public double Sum()
        {
            double sum = 0;
            for (int i = 0; i < this.mn; i++)
            {
                sum += this.data[i];
            }

            return sum;
        }

        public double SumSquares()
        {
            double sum = 0;
            for (int i = 0; i < this.mn; i++)
            {
                sum += this.data[i] * this.data[i];
            }

            return sum;
        }

        public double Product()
        {
            double product = 1.0;
            for (int i = 0; i < this.mn; i++)
            {
                product *= this.data[i];
            }

            return product;
        }

        public double L1distance(Matrix matA)
        {
            return L1distance(matA, this);
        }

        public double L2distance(Matrix matA)
        {
            return L2distance(matA, this);
        }

        public void Normalize(Matrix matA)
        {
            Normalize(matA, this);
        }

        public void Normalize()
        {
            Normalize(this, this);
        }

        public double Magnitude()
        {
            double sum = 0;
            for (int i = 0; i < this.mn; i++)
            {
                sum += this.data[i] * this.data[i];
            }

            return sum;
        }

        public void NormalizeRows()
        {
            double sum;
            for (int i = 0; i < this.m; i++)
            {
                sum = 0;
                for (int j = 0; j < this.n; j++)
                {
                    sum += this[i, j];
                }

                for (int j = 0; j < this.n; j++)
                {
                    this[i, j] = this[i, j] / sum;
                }
            }
        }

        public override string ToString()
        {
            string s = string.Empty;

            for (int i = 0; i < this.m; i++)
            {
                for (int j = 0; j < this.n; j++)
                {
                    s += this[i, j].ToString();
                    if (j < this.n - 1)
                    {
                        s += ", \t";
                    }
                }

                s += " \r\n";
            }

            return s;
        }
    }
#pragma warning restore SA1600
}

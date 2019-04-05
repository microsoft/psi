// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Kinect.Windows
{
    using System;
    using MathNet.Spatial.Euclidean;
    using MathNet.Spatial.Units;
    using Microsoft.Kinect;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [Ignore]
    public class QuaternionTest : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Very simple test to make sure that our quaternion math is correct.
        // This rotates 10 degrees around the Y axis via a quaternion and then
        // rotates the entire result by 90 degrees. After this the resulting quaternion
        // should be rotated 100 degrees around Y axis.
        [TestMethod]
        [Timeout(60000)]
        [Ignore]
        public void TestQuaternion()
        {
            CoordinateSystem ncs = new CoordinateSystem();
            Vector3D v = new Vector3D(0.0, 1.0, 0.0);
            ncs = ncs.RotateCoordSysAroundVector(v.Normalize(), Angle.FromDegrees(90.0 /* in LHS */));
            Vector4 q;
            double axisX = 0.0;
            double axisY = 1.0;
            double axisZ = 0.0;
            double sa = Math.Sin(-5.0 /*half angle in RHS*/ * 3.1415926 / 180.0);
            double ca = Math.Cos(-5.0 /*half angle in RHS*/ * 3.1415926 / 180.0);
            q.X = (float)(axisX * sa);
            q.Y = (float)(axisY * sa);
            q.Z = (float)(axisZ * sa);
            q.W = (float)ca;
            var rot = ncs.GetRotationSubMatrix();
            var qrot = Microsoft.Psi.Kinect.KinectExtensions.QuaternionToMatrix(q);
            var qv = Microsoft.Psi.Kinect.KinectExtensions.MatrixToQuaternion(rot * qrot);
            Vector4 axisAngle = Microsoft.Psi.Kinect.KinectExtensions.QuaternionAsAxisAngle(qv);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}

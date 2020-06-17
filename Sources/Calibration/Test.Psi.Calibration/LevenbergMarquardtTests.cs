// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Test.Psi.Calibration
{
    using System;
    using MathNet.Numerics.LinearAlgebra;
    using Microsoft.Psi.Calibration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Distortion tests.
    /// </summary>
    [TestClass]
    public class LevenbergMarquardtTests
    {
        [TestMethod]
        [Timeout(60000)]
        public void TestOptimization()
        {
            // generate x_i, y_i observations on test function
            var random = new Random();

            int n = 200;

            var matX = Vector<double>.Build.Dense(n);
            var matY = Vector<double>.Build.Dense(n);

            double a = 100;
            double b = 102;
            for (int i = 0; i < n; i++)
            {
                double x = (random.NextDouble() / (Math.PI / 4.0)) - (Math.PI / 8.0);
                double y = (a * Math.Cos(b * x)) + (b * Math.Sin(a * x)) + (random.NextDouble() * 0.1);
                matX[i] = x;
                matY[i] = y;
            }

            LevenbergMarquardt.Function f = (Vector<double> parameters) =>
            {
                // return y_i - f(x_i, parameters) as column vector
                var error = Vector<double>.Build.Dense(n);

                double a2 = parameters[0];
                double b2 = parameters[1];

                for (int i = 0; i < n; i++)
                {
                    double y = (a2 * Math.Cos(b2 * matX[i])) + (b2 * Math.Sin(a2 * matX[i]));
                    error[i] = matY[i] - y;
                }

                return error;
            };

            var levenbergMarquardt = new LevenbergMarquardt(f);

            var parameters0 = Vector<double>.Build.Dense(2);
            parameters0[0] = 90;
            parameters0[1] = 96;

            var rmsError = levenbergMarquardt.Minimize(parameters0);
        }
    }
}

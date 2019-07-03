// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Kinect
{
    using System;
    using System.IO;

    /// <summary>
    /// Defines a class for performing Levenberg-Marquardt optimization.
    /// </summary>
    internal class LevenbergMarquardt
    {
        private int maximumIterations = 100;
        private double minimumReduction = 1.0e-5;
        private double maximumLambda = 1.0e7;
        private double lambdaIncrement = 10.0;
        private double initialLambda = 1.0e-3;
        private Function function;
        private Jacobian jacobianFunction;
        private States state = States.Running;
        private double rmsError;
        private System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

        /// <summary>
        /// Initializes a new instance of the <see cref="LevenbergMarquardt"/> class.
        /// </summary>
        /// <param name="function">Cost function.</param>
        public LevenbergMarquardt(Function function)
            : this(function, new NumericalDifferentiation(function).Jacobian)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LevenbergMarquardt"/> class.
        /// </summary>
        /// <param name="function">Cost function.</param>
        /// <param name="jacobianFunction">Jacobian.</param>
        public LevenbergMarquardt(Function function, Jacobian jacobianFunction)
        {
            this.function = function;
            this.jacobianFunction = jacobianFunction;
        }

        /// <summary>
        /// y_i - f(x_i, parameters) as column vector.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Matrix.</returns>
        public delegate Matrix Function(Matrix parameters);

        /// <summary>
        /// J_ij, ith error from function, jth parameter.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Matrix.</returns>
        public delegate Matrix Jacobian(Matrix parameters);

        /// <summary>
        /// States for optimization.
        /// </summary>
        public enum States
        {
#pragma warning disable SA1602 // Enumeration items must be documented
            Running,
            MaximumIterations,
            LambdaTooLarge,
            ReductionStepTooSmall,
#pragma warning restore SA1602 // Enumeration items must be documented
        }

        /// <summary>
        /// Gets the RMS error.
        /// </summary>
        public double RMSError
        {
            get { return this.rmsError; }
        }

        /// <summary>
        /// Gets the optimization state.
        /// </summary>
        public States State
        {
            get { return this.state; }
        }

        /// <summary>
        /// Performs unit test.
        /// </summary>
        public static void Test()
        {
            // generate x_i, y_i observations on test function
            var random = new Random();

            int n = 200;

            var matX = new Matrix(n, 1);
            var matY = new Matrix(n, 1);

            double a = 100;
            double b = 102;
            for (int i = 0; i < n; i++)
            {
                double x = (random.NextDouble() / (Math.PI / 4.0)) - (Math.PI / 8.0);
                double y = (a * Math.Cos(b * x)) + (b * Math.Sin(a * x)) + (random.NextDouble() * 0.1);
                matX[i] = x;
                matY[i] = y;
            }

            Function f = (Matrix parameters) =>
            {
                // return y_i - f(x_i, parameters) as column vector
                var error = new Matrix(n, 1);

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

            var parameters0 = new Matrix(2, 1);
            parameters0[0] = 90;
            parameters0[1] = 96;

            var rmsError = levenbergMarquardt.Minimize(parameters0);
        }

        /// <summary>
        /// Minimizes function.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Returns the RMS.</returns>
        public double Minimize(Matrix parameters)
        {
            this.state = States.Running;
            for (int iteration = 0; iteration < this.maximumIterations; iteration++)
            {
                this.MinimizeOneStep(parameters);
                if (this.state != States.Running)
                {
                    return this.RMSError;
                }
            }

            this.state = States.MaximumIterations;
            return this.RMSError;
        }

        /// <summary>
        /// Writes the specified matrix to the specified file.
        /// </summary>
        /// <param name="matA">Matrix to write.</param>
        /// <param name="filename">Name of output file.</param>
        public void WriteMatrixToFile(Matrix matA, string filename)
        {
            var file = new StreamWriter(filename);
            for (int i = 0; i < matA.Rows; i++)
            {
                for (int j = 0; j < matA.Cols; j++)
                {
                    file.Write(matA[i, j] + "\t");
                }

                file.WriteLine();
            }

            file.Close();
        }

        /// <summary>
        /// Single step of the optimization.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Returns the error.</returns>
        public double MinimizeOneStep(Matrix parameters)
        {
            // initial value of the function; callee knows the size of the returned vector
            var errorVector = this.function(parameters);
            var error = errorVector.Dot(errorVector);

            // Jacobian; callee knows the size of the returned matrix
            var matJ = this.jacobianFunction(parameters);

            // J'*J
            var matJtJ = new Matrix(parameters.Size, parameters.Size);

            // stopWatch.Restart();
            // JtJ.MultATA(J, J); // this is the big calculation that could be parallelized
            matJtJ.MultATAParallel(matJ, matJ);

            // Console.WriteLine("JtJ: J size {0}x{1} {2}ms", J.Rows, J.Cols, stopWatch.ElapsedMilliseconds);

            // J'*error
            var matJtError = new Matrix(parameters.Size, 1);

            // stopWatch.Restart();
            matJtError.MultATA(matJ, errorVector); // error vector must be a column vector

            // Console.WriteLine("JtError: errorVector size {0}x{1} {2}ms", errorVector.Rows, errorVector.Cols, stopWatch.ElapsedMilliseconds);

            // allocate some space
            var matJtJaugmented = new Matrix(parameters.Size, parameters.Size);
            var matJtJinv = new Matrix(parameters.Size, parameters.Size);
            var matDelta = new Matrix(parameters.Size, 1);
            var matNewParameters = new Matrix(parameters.Size, 1);

            // find a value of lambda that reduces error
            double lambda = this.initialLambda;
            while (true)
            {
                // augment J'*J: J'*J += lambda*(diag(J))
                matJtJaugmented.Copy(matJtJ);
                for (int i = 0; i < parameters.Size; i++)
                {
                    matJtJaugmented[i, i] = (1.0 + lambda) * matJtJ[i, i];
                }

                // WriteMatrixToFile(errorVector, "errorVector");
                // WriteMatrixToFile(J, "J");
                // WriteMatrixToFile(JtJaugmented, "JtJaugmented");
                // WriteMatrixToFile(JtError, "JtError");

                // solve for delta: (J'*J + lambda*(diag(J)))*delta = J'*error
                matJtJinv.Inverse(matJtJaugmented);
                matDelta.Mult(matJtJinv, matJtError);

                // new parameters = parameters - delta [why not add?]
                matNewParameters.Sub(parameters, matDelta);

                // evaluate function, compute error
                var newErrorVector = this.function(matNewParameters);
                double newError = newErrorVector.Dot(newErrorVector);

                // if error is reduced, divide lambda by 10
                bool improvement;
                if (newError < error)
                {
                    lambda /= this.lambdaIncrement;
                    improvement = true;
                }
                else
                { // if not, multiply lambda by 10
                    lambda *= this.lambdaIncrement;
                    improvement = false;
                }

                // termination criteria:
                // reduction in error is too small
                var diff = new Matrix(errorVector.Size, 1);
                diff.Sub(errorVector, newErrorVector);
                double diffSq = diff.Dot(diff);
                double errorDelta = Math.Sqrt(diffSq / error);

                if (errorDelta < this.minimumReduction)
                {
                    this.state = States.ReductionStepTooSmall;
                }

                // lambda is too big
                if (lambda > this.maximumLambda)
                {
                    this.state = States.LambdaTooLarge;
                }

                // change in parameters is too small [not implemented]

                // if we made an improvement, accept the new parameters
                if (improvement)
                {
                    parameters.Copy(matNewParameters);
                    error = newError;
                    break;
                }

                // if we meet termination criteria, break
                if (this.state != States.Running)
                {
                    break;
                }
            }

            this.rmsError = Math.Sqrt(error / errorVector.Size);
            return this.rmsError;
        }

        /// <summary>
        /// Class for doing numerical differentiation.
        /// </summary>
        public class NumericalDifferentiation
        {
            private Function function;

            /// <summary>
            /// Initializes a new instance of the <see cref="NumericalDifferentiation"/> class.
            /// </summary>
            /// <param name="function">Cost function.</param>
            public NumericalDifferentiation(Function function)
            {
                this.function = function;
            }

            /// <summary>
            /// Returns the Jacobian
            /// J_ij, ith error from function, jth parameter.
            /// </summary>
            /// <param name="parameters">Parameters.</param>
            /// <returns>Returns Jacobian.</returns>
            public Matrix Jacobian(Matrix parameters)
            {
                const double deltaFactor = 1.0e-6;
                const double minDelta = 1.0e-6;

                // evaluate the function at the current solution
                var errorVector0 = this.function(parameters);
                var matJ = new Matrix(errorVector0.Size, parameters.Size);

                // vary each paremeter
                for (int j = 0; j < parameters.Size; j++)
                {
                    double parameterValue = parameters[j]; // save the original value

                    double delta = parameterValue * deltaFactor;
                    if (Math.Abs(delta) < minDelta)
                    {
                        delta = minDelta;
                    }

                    parameters[j] = parameters[j] + delta;

                    // we only get error from function, but error(p + d) - error(p) = f(p + d) - f(p)
                    var errorVector = this.function(parameters);
                    errorVector.Sub(errorVector0);

                    for (int i = 0; i < errorVector0.Rows; i++)
                    {
                        matJ[i, j] = errorVector[i] / delta;
                    }

                    parameters[j] = parameterValue; // restore original value
                }

                return matJ;
            }
        }
    }
}

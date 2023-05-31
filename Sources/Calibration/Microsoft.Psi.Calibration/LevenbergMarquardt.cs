// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Calibration
{
    using System;
    using MathNet.Numerics.LinearAlgebra;

    /// <summary>
    /// Defines a class for performing Levenberg-Marquardt optimization.
    /// </summary>
    public class LevenbergMarquardt
    {
        private int maximumIterations = 100;
        private double minimumReduction = 1.0e-5;
        private double maximumLambda = 1.0e7;
        private double lambdaIncrement = 10.0;
        private double initialLambda = 1.0e-3;
        private Function function;
        private Jacobian jacobianFunction;

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
        /// <param name="jacobianFunction">Jacobian function.</param>
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
        public delegate Vector<double> Function(Vector<double> parameters);

        /// <summary>
        /// J_ij, ith error from function, jth parameter.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Matrix.</returns>
        public delegate Matrix<double> Jacobian(Vector<double> parameters);

        /// <summary>
        /// States for optimization.
        /// </summary>
        public enum States
        {
            /// <summary>
            /// Running.
            /// </summary>
            Running,

            /// <summary>
            /// Maximum iterations.
            /// </summary>
            MaximumIterations,

            /// <summary>
            /// Lambda too large.
            /// </summary>
            LambdaTooLarge,

            /// <summary>
            /// Reduction step too small.
            /// </summary>
            ReductionStepTooSmall,
        }

        /// <summary>
        /// Gets the RMS error.
        /// </summary>
        public double RMSError { get; private set; }

        /// <summary>
        /// Gets the optimization state.
        /// </summary>
        public States State { get; private set; } = States.Running;

        /// <summary>
        /// Minimizes function.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Returns the RMS.</returns>
        public double Minimize(Vector<double> parameters)
        {
            this.State = States.Running;
            for (int iteration = 0; iteration < this.maximumIterations; iteration++)
            {
                this.MinimizeOneStep(parameters);
                if (this.State != States.Running)
                {
                    return this.RMSError;
                }
            }

            this.State = States.MaximumIterations;
            return this.RMSError;
        }

        /// <summary>
        /// Single step of the optimization.
        /// </summary>
        /// <param name="parameters">Parameters.</param>
        /// <returns>Returns the error.</returns>
        public double MinimizeOneStep(Vector<double> parameters)
        {
            // initial value of the function; callee knows the size of the returned vector
            var errorVector = this.function(parameters);
            var error = errorVector.DotProduct(errorVector);

            // Jacobian; callee knows the size of the returned matrix
            var matJ = this.jacobianFunction(parameters);

            // J'*J
            var matJtJ = matJ.TransposeThisAndMultiply(matJ);

            // J'*error
            var matJtError = matJ.TransposeThisAndMultiply(errorVector);

            // allocate some space
            var matJtJaugmented = Matrix<double>.Build.Dense(parameters.Count, parameters.Count);

            // find a value of lambda that reduces error
            double lambda = this.initialLambda;
            while (true)
            {
                // augment J'*J: J'*J += lambda*(diag(J))
                matJtJ.CopyTo(matJtJaugmented);
                for (int i = 0; i < parameters.Count; i++)
                {
                    matJtJaugmented[i, i] = (1.0 + lambda) * matJtJ[i, i];
                }

                // solve for delta: (J'*J + lambda*(diag(J)))*delta = J'*error
                var matJtJinv = matJtJaugmented.Inverse();
                var matDelta = matJtJinv * matJtError;

                // new parameters = parameters - delta [why not add?]
                var matNewParameters = parameters - matDelta;

                // evaluate function, compute error
                var newErrorVector = this.function(matNewParameters);
                double newError = newErrorVector.DotProduct(newErrorVector);

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
                var diff = errorVector - newErrorVector;
                double diffSq = diff.DotProduct(diff);
                double errorDelta = Math.Sqrt(diffSq / error);

                if (errorDelta < this.minimumReduction)
                {
                    this.State = States.ReductionStepTooSmall;
                }

                // lambda is too big
                if (lambda > this.maximumLambda)
                {
                    this.State = States.LambdaTooLarge;
                }

                // change in parameters is too small [not implemented]

                // if we made an improvement, accept the new parameters
                if (improvement)
                {
                    matNewParameters.CopyTo(parameters);
                    error = newError;
                    break;
                }

                // if we meet termination criteria, break
                if (this.State != States.Running)
                {
                    break;
                }
            }

            this.RMSError = Math.Sqrt(error / errorVector.Count);
            return this.RMSError;
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
            public Matrix<double> Jacobian(Vector<double> parameters)
            {
                const double deltaFactor = 1.0e-6;
                const double minDelta = 1.0e-6;

                // evaluate the function at the current solution
                var errorVector0 = this.function(parameters);
                var matJ = Matrix<double>.Build.Dense(errorVector0.Count, parameters.Count);

                // vary each paremeter
                for (int j = 0; j < parameters.Count; j++)
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
                    errorVector -= errorVector0;

                    for (int i = 0; i < errorVector0.Count; i++)
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

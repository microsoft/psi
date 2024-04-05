// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Onnx
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ML;
    using Microsoft.ML.Data;
    using Microsoft.ML.Transforms.Onnx;

    /// <summary>
    /// Implements a helper class for running ONNX models.
    /// </summary>
    /// <typeparam name="TModelIn">The input data type of the model.</typeparam>
    /// <typeparam name="TModelOut">The output data type of the model.</typeparam>
    /// <remarks>This class implements the ability to run a specified ONNX model.
    /// It does so by leveraging the ML.NET framework. The
    /// <see cref="OnnxModelConfiguration"/> object specified at construction
    /// time provides information about where to load the network from, etc.</remarks>
    public class OnnxModel<TModelIn, TModelOut> : IDisposable
    {
        private static readonly Dictionary<Type, PrimitiveDataViewType> DataViewTypeMap = new ()
        {
            { typeof(int), NumberDataViewType.Int32 },
            { typeof(float), NumberDataViewType.Single },
            { typeof(byte), NumberDataViewType.Byte },
            { typeof(double), NumberDataViewType.Double },
            { typeof(short), NumberDataViewType.Int16 },
            { typeof(long), NumberDataViewType.Int64 },
            { typeof(sbyte), NumberDataViewType.SByte },
            { typeof(ushort), NumberDataViewType.UInt16 },
            { typeof(uint), NumberDataViewType.UInt32 },
            { typeof(ulong), NumberDataViewType.UInt64 },
        };

        private readonly OnnxModelConfiguration configuration;
        private readonly MLContext context = new ();
        private readonly SchemaDefinition schemaDefinition;
        private OnnxTransformer onnxTransformer;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnnxModel{TIn, TOut}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration for the onnx model runner.</param>
        public OnnxModel(OnnxModelConfiguration configuration)
        {
            this.configuration = configuration;

            // The schemaDefinition is a ML.NET construct that allows us to specify the form
            // of the inputs. In this case we construct a schema definition programmatically
            // to reflect that the input is a vector of floats, of the sizes specified in the
            // configuration
            this.schemaDefinition = SchemaDefinition.Create(typeof(OnnxInputVector<TModelIn>));
            this.schemaDefinition[nameof(OnnxInputVector<TModelIn>.Vector)].ColumnType = new VectorDataViewType(DataViewTypeMap[typeof(TModelIn)], this.configuration.InputVectorSize);
            this.schemaDefinition[nameof(OnnxInputVector<TModelIn>.Vector)].ColumnName = this.configuration.InputVectorName;

            // We create the onnxTransformer which will be used to score inputs
            var onnxEmptyInputDataView = this.context.Data.LoadFromEnumerable(new List<OnnxInputVector<TModelIn>>(), this.schemaDefinition);
            var scoringEstimator =
                this.context.Transforms.ApplyOnnxModel(
                    modelFile: configuration.ModelFileName,
                    outputColumnNames: new[] { configuration.OutputVectorName },
                    inputColumnNames: new[] { configuration.InputVectorName },
                    shapeDictionary: configuration.ShapeDictionary,
                    gpuDeviceId: configuration.GpuDeviceId,
                    fallbackToCpu: false);
            this.onnxTransformer = scoringEstimator.Fit(onnxEmptyInputDataView);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.onnxTransformer?.Dispose();
            this.onnxTransformer = null;
        }

        /// <summary>
        /// Runs the ONNX model on an input vector.
        /// </summary>
        /// <param name="input">The input vector.</param>
        /// <returns>The output vector produced by the ONNX model.</returns>
        public TModelOut[] GetPrediction(TModelIn[] input)
        {
            // construct the input
            var onnxInput = new List<OnnxInputVector<TModelIn>> { new OnnxInputVector<TModelIn> { Vector = input } };

            // construct a data view over the input
            var onnxInputDataView = this.context.Data.LoadFromEnumerable(onnxInput, this.schemaDefinition);

            // apply the onnxTransformer and extract the results
            return this.onnxTransformer.Transform(onnxInputDataView).GetColumn<TModelOut[]>(this.configuration.OutputVectorName).ToArray()[0];
        }
    }
}

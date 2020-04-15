// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Adapters
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Microsoft.Psi.PsiStudio;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents information about a stream adapter.
    /// </summary>
    public class StreamAdapterMetadata
    {
        private StreamAdapterMetadata(Type inputType, Type outputType, Type adapterType)
        {
            this.InputType = inputType;
            this.OutputType = outputType;
            this.AdapterType = adapterType;
        }

        /// <summary>
        /// Gets the input type.
        /// </summary>
        public Type InputType { get; private set; }

        /// <summary>
        /// Gets the output type.
        /// </summary>
        public Type OutputType { get; private set; }

        /// <summary>
        /// Gets the adapter type.
        /// </summary>
        public Type AdapterType { get; private set; }

        /// <summary>
        /// Creates a new stream adapter metadata.
        /// </summary>
        /// <param name="streamAdapterType">The type of the stream adapter.</param>
        /// <param name="logWriter">The log writer where errors should be written to.</param>
        /// <returns>A stream adapter metadata.</returns>
        public static StreamAdapterMetadata Create(Type streamAdapterType, VisualizationLogWriter logWriter)
        {
            if (streamAdapterType == null)
            {
                throw new NullReferenceException(nameof(streamAdapterType));
            }

            // Stream adapter must not be a generic type
            if (streamAdapterType.IsGenericType)
            {
                logWriter.WriteError("StreamAdapter {0} could not be loaded because it is a generic type", streamAdapterType.Name);
                return null;
            }

            // Find the stream adapter base type
            Type baseStreamAdapterType = streamAdapterType;
            while ((baseStreamAdapterType != null) && (baseStreamAdapterType.Name != typeof(StreamAdapter<,>).Name))
            {
                baseStreamAdapterType = baseStreamAdapterType.BaseType;
            }

            // Make sure ouor type really derives from the base stream adapter type.
            if (baseStreamAdapterType == null)
            {
                logWriter.WriteError("StreamAdapter {0} could not be loaded because it is not derived from StreamAdapter<TSrc, TDest>", streamAdapterType.Name);
                return null;
            }

            // Create the stream adapter metadata
            return new StreamAdapterMetadata(baseStreamAdapterType.GenericTypeArguments[0], baseStreamAdapterType.GenericTypeArguments[1], streamAdapterType);
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using Microsoft.Psi.Visualization.Data;

    /// <summary>
    /// Represents information about a summarizer.
    /// </summary>
    public class SummarizerMetadata
    {
        private SummarizerMetadata(Type inputType, Type outputType, Type summarizerType)
        {
            this.InputType = inputType;
            this.OutputType = outputType;
            this.SummarizerType = summarizerType;
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
        /// Gets the summarizer type.
        /// </summary>
        public Type SummarizerType { get; private set; }

        /// <summary>
        /// Creates a new summarizer metadata.
        /// </summary>
        /// <param name="summarizerType">The type of the summarizer.</param>
        /// <param name="logWriter">The log writer where errors should be written to.</param>
        /// <returns>A summarizer metadata.</returns>
        public static SummarizerMetadata Create(Type summarizerType, VisualizationLogWriter logWriter)
        {
            if (summarizerType == null)
            {
                throw new NullReferenceException(nameof(summarizerType));
            }

            // Summarizer must not be a generic type
            if (summarizerType.IsGenericType)
            {
                logWriter.WriteError("Summarizer {0} could not be loaded because it is a generic type", summarizerType.Name);
                return null;
            }

            // Summarizers must be directly derived from Summarizer<TSrc, TDest>
            Type baseType = summarizerType.BaseType;
            if (baseType.Name != typeof(Summarizer<,>).Name || baseType.Module.Name != typeof(Summarizer<,>).Module.Name)
            {
                logWriter.WriteError("Summarizer {0} could not be loaded because it is not directly derived from Summarizer<TSrc, TDest>", summarizerType.Name);
                return null;
            }

            // Create the summarizer metadata
            return new SummarizerMetadata(summarizerType.BaseType.GenericTypeArguments[0], summarizerType.BaseType.GenericTypeArguments[1], summarizerType);
        }
    }
}

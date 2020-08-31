// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    /// <summary>
    /// Represents an annotation schema definition.
    /// </summary>
    public class AnnotationSchemaDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSchemaDefinition"/> class.
        /// </summary>
        /// <param name="name">The name of the annotation schema definition.</param>
        /// <param name="schema">The schema of the annotation schema definition.</param>
        public AnnotationSchemaDefinition(string name, IAnnotationSchema schema)
        {
            this.Name = name;
            this.Schema = schema;
        }

        /// <summary>
        /// Gets or sets the name of the schema defintion.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the annotation schema.
        /// </summary>
        public IAnnotationSchema Schema { get; set; }
    }
}
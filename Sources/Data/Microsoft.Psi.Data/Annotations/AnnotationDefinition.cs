// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Psi.Data.Converters;
    using Microsoft.Psi.Data.Helpers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Represents a definition of an enumeration.
    /// </summary>
    public class AnnotationDefinition
    {
        private static JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Converters = { new StringEnumConverter() },
            SerializationBinder = new SafeSerializationBinder(),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationDefinition"/> class.
        /// </summary>
        /// <param name="name">The name of the annotation definition.</param>
        public AnnotationDefinition(string name)
        {
            this.Name = name;
            this.SchemaDefinitions = new List<AnnotationSchemaDefinition>();
        }

        /// <summary>
        /// Gets the collection of schema definitions in the annotation definition.
        /// </summary>
        public List<AnnotationSchemaDefinition> SchemaDefinitions { get; private set; }

        /// <summary>
        /// Gets the name of the annotation definition.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Loads an annotation definition from disk.
        /// </summary>
        /// <param name="fileName">The full path and filename of the annotation definition to load.</param>
        /// <returns>The requested annotation definition if it exists, otherwise null.</returns>
        public static AnnotationDefinition Load(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            try
            {
                using (StreamReader streamReader = new StreamReader(fileName))
                {
                    JsonReader reader = new JsonTextReader(streamReader);
                    JsonSerializer serializer = JsonSerializer.Create(jsonSerializerSettings);
                    return serializer.Deserialize<AnnotationDefinition>(reader);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this annotation definition contains an annotation schema definition with a specified name.
        /// </summary>
        /// <param name="schemaDefinitionName">The name of the schem a definition to search for.</param>
        /// <returns>True if the annotation difinition contains an annotation schema definition with the specified name, otherwise returns false.</returns>
        public bool ContainsSchemaDefinition(string schemaDefinitionName)
        {
            return this.SchemaDefinitions.Exists(s => s.Name == schemaDefinitionName);
        }

        /// <summary>
        /// Gets a specified annotation schema definition.
        /// </summary>
        /// <param name="schemaDefinitionName">The name of the schem a definition to search for.</param>
        /// <returns>The requested annotation schema definition if it exists in this annotation definition, otherwise null.</returns>
        public AnnotationSchemaDefinition GetSchemaDefinition(string schemaDefinitionName)
        {
            return this.SchemaDefinitions.FirstOrDefault(s => s.Name == schemaDefinitionName);
        }

        /// <summary>
        /// Adds a new annotation schema definition to this annotation definition.
        /// </summary>
        /// <param name="schemaDefinition">The annotation schema definition to add to the collection.</param>
        public void AddSchemaDefinition(AnnotationSchemaDefinition schemaDefinition)
        {
            if (this.ContainsSchemaDefinition(schemaDefinition.Name))
            {
                throw new ApplicationException(string.Format("AnnotationDefinition {0} already contains a schema definition named {1}.", this.Name, schemaDefinition.Name));
            }

            this.SchemaDefinitions.Add(schemaDefinition);
        }

        /// <summary>
        /// Creates a new time interval annotation instance based on this annotation definition.
        /// </summary>
        /// <param name="timeInterval">The annotation's interval.</param>
        /// <returns>A new time interval annotation.</returns>
        public TimeIntervalAnnotation CreateTimeIntervalAnnotation(TimeInterval timeInterval)
        {
            // Create the collection of initial values for the annotation
            // based on the default values of each schema in the definition.
            Dictionary<string, object> values = new Dictionary<string, object>();
            foreach (AnnotationSchemaDefinition schemaDefinition in this.SchemaDefinitions)
            {
                MethodInfo defaultValueProperty = schemaDefinition.Schema.GetType().GetProperty("DefaultValue").GetGetMethod(false);
                values[schemaDefinition.Name] = defaultValueProperty.Invoke(schemaDefinition.Schema, new object[] { });
            }

            return new TimeIntervalAnnotation(timeInterval, values);
        }

        /// <summary>
        /// Saves this annotation definition to disk.
        /// </summary>
        /// <param name="fileName">The full path and filename of the location to save this annotation definition.</param>
        public void Save(string fileName)
        {
            StreamWriter jsonFile = null;
            try
            {
                jsonFile = File.CreateText(fileName);
                using (var jsonWriter = new JsonTextWriter(jsonFile))
                {
                    JsonSerializer serializer = JsonSerializer.Create(jsonSerializerSettings);
                    serializer.Serialize(jsonWriter, this);
                }
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }
    }
}
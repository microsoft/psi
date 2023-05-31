// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Psi.Data.Helpers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Represents an annotation schema.
    /// </summary>
    public class AnnotationSchema
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new ()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Converters = { new StringEnumConverter() },
            SerializationBinder = new SafeSerializationBinder(),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSchema"/> class.
        /// </summary>
        /// <param name="name">The name of the annotation schema.</param>
        public AnnotationSchema(string name)
        {
            this.Name = name;
            this.AttributeSchemas = new List<AnnotationAttributeSchema>();
        }

        /// <summary>
        /// Gets the name of the annotation schema.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the collection of attribute schemas.
        /// </summary>
        public List<AnnotationAttributeSchema> AttributeSchemas { get; private set; }

        /// <summary>
        /// Loads an annotation schema from disk.
        /// </summary>
        /// <param name="fileName">The full path and filename of the annotation schema to load.</param>
        /// <returns>The requested annotation schema.</returns>
        public static AnnotationSchema LoadFrom(string fileName)
        {
            using var streamReader = new StreamReader(fileName);
            return LoadFrom(streamReader);
        }

        /// <summary>
        /// Tries to load an annotation schema from disk.
        /// </summary>
        /// <param name="fileName">The full path and filename of the annotation schema to load.</param>
        /// <param name="annotationSchema">The loaded annotation schema if successful.</param>
        /// <returns>True if the annotation schema is loaded successfully, otherwise null.</returns>
        public static bool TryLoadFrom(string fileName, out AnnotationSchema annotationSchema)
        {
            annotationSchema = null;
            if (!File.Exists(fileName))
            {
                return false;
            }

            try
            {
                annotationSchema = LoadFrom(fileName);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Loads an annotation schema from disk.
        /// </summary>
        /// <param name="fileName">The full path and filename of the annotation schema to load.</param>
        /// <returns>The requested annotation schema if it exists, otherwise null.</returns>
        public static AnnotationSchema LoadOrDefault(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            try
            {
                using var streamReader = new StreamReader(fileName);
                JsonReader reader = new JsonTextReader(streamReader);
                JsonSerializer serializer = JsonSerializer.Create(JsonSerializerSettings);
                var annotationSchema = serializer.Deserialize<AnnotationSchema>(reader);

                // Perform simple deserialization checks
                if (string.IsNullOrEmpty(annotationSchema.Name))
                {
                    throw new Exception("Deserialized annotation schema has empty name.");
                }
                else if (annotationSchema.AttributeSchemas.Count == 0)
                {
                    throw new Exception("Deserialized annotation schema has no attributes.");
                }
                else if (annotationSchema.AttributeSchemas.Any(s => string.IsNullOrEmpty(s.Name)))
                {
                    throw new Exception("Deserialized annotation schema which contains attributes with no names specified.");
                }

                return annotationSchema;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this annotation schema contains a specified attribute.
        /// </summary>
        /// <param name="attributeName">The name of the attribute.</param>
        /// <returns>True if the annotation schema contains the specified attribute, otherwise false.</returns>
        public bool ContainsAttribute(string attributeName)
            => this.AttributeSchemas.Any(ad => ad.Name == attributeName);

        /// <summary>
        /// Gets the schema for a specified attribute.
        /// </summary>
        /// <param name="attributeName">The name of the attribute.</param>
        /// <returns>The schema for a specified attribute if the attribute exists, otherwise null.</returns>
        public AnnotationAttributeSchema GetAttributeSchema(string attributeName)
            => this.AttributeSchemas.FirstOrDefault(ad => ad.Name == attributeName);

        /// <summary>
        /// Adds a new attribute to this annotation schema.
        /// </summary>
        /// <param name="attributeSchema">The attribute schema to add.</param>
        public void AddAttributeSchema(AnnotationAttributeSchema attributeSchema)
        {
            if (this.ContainsAttribute(attributeSchema.Name))
            {
                throw new ApplicationException(string.Format("AnnotationSchema {0} already contains an attribute named {1}.", this.Name, attributeSchema.Name));
            }

            this.AttributeSchemas.Add(attributeSchema);
        }

        /// <summary>
        /// Creates a new time interval annotation instance on a specified track, based on this annotation schema.
        /// </summary>
        /// <param name="timeInterval">The time interval.</param>
        /// <param name="track">The track.</param>
        /// <returns>A new time interval annotation.</returns>
        public TimeIntervalAnnotation CreateDefaultTimeIntervalAnnotation(TimeInterval timeInterval, string track)
        {
            // Create the collection of initial values for the annotation based on the default values
            var values = new Dictionary<string, IAnnotationValue>();
            foreach (var attributeSchema in this.AttributeSchemas)
            {
                values[attributeSchema.Name] = attributeSchema.ValueSchema.GetDefaultAnnotationValue();
            }

            return new TimeIntervalAnnotation(timeInterval, track, values);
        }

        /// <summary>
        /// Saves this annotation schema to a specified file.
        /// </summary>
        /// <param name="fileName">The full path and filename to save this annotation schema to.</param>
        public void Save(string fileName)
        {
            StreamWriter jsonFile = null;
            try
            {
                jsonFile = File.CreateText(fileName);
                using var jsonWriter = new JsonTextWriter(jsonFile);
                JsonSerializer.Create(JsonSerializerSettings).Serialize(jsonWriter, this);
            }
            finally
            {
                jsonFile?.Dispose();
            }
        }

        private static AnnotationSchema LoadFrom(StreamReader streamReader)
        {
            var reader = new JsonTextReader(streamReader);
            var serializer = JsonSerializer.Create(JsonSerializerSettings);
            var annotationSchema = serializer.Deserialize<AnnotationSchema>(reader);

            // Perform simple deserialization checks
            if (string.IsNullOrEmpty(annotationSchema.Name))
            {
                throw new Exception("Deserialized annotation schema has empty name.");
            }
            else if (annotationSchema.AttributeSchemas.Count == 0)
            {
                throw new Exception("Deserialized annotation schema has no attributes.");
            }

            return annotationSchema;
        }
    }
}
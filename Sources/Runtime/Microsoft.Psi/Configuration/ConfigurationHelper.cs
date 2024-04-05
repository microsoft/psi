// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// A component helper for managing component configuration.
    /// </summary>
    public static class ConfigurationHelper
    {
        /// <summary>
        /// Loads or creates a new instance of the configuration class.
        /// </summary>
        /// <typeparam name="T">The type of the configuration class.</typeparam>
        /// <param name="configurationFilename">The name of the configuration file to load.</param>
        /// <param name="defaultConfiguration">The default configuration to use if no configuration was loaded.</param>
        /// <param name="createFileIfNotExist">Whether the configuration file should be created (using the default configuration) if one does not already exist.</param>
        /// <param name="extraTypes">Extra types that are required for serialization.</param>
        /// <returns>The loaded configuration if it exists, or the default configuration.</returns>
        public static T ReadFromFileOrDefault<T>(string configurationFilename, T defaultConfiguration = default, bool createFileIfNotExist = false, Type[] extraTypes = null)
        {
            T configuration;
            if (!string.IsNullOrEmpty(configurationFilename))
            {
                // if file exists, try to load
                if (File.Exists(configurationFilename))
                {
                    configuration = ReadFromXml<T>(configurationFilename, extraTypes);
                }
                else
                {
                    // otherwise use the default configuration
                    configuration = defaultConfiguration;
                    if (createFileIfNotExist && configuration is not null)
                    {
                        // save the configuration to the file if requested and it is not null
                        WriteToXml(configuration, configurationFilename, indent: true, extraTypes);
                    }
                }
            }
            else
            {
                // default configuration
                configuration = defaultConfiguration;
            }

            return configuration;
        }

        /// <summary>
        /// Write the instance of the configuration class to a specified file.
        /// </summary>
        /// <typeparam name="T">The type of the configuration class.</typeparam>
        /// <param name="configurationFilename">The name of the file to write to.</param>
        /// <param name="configuration">The configuration to write to the file.</param>
        /// <param name="extraTypes">Extra types that are required for serialization.</param>
        public static void WriteToFile<T>(string configurationFilename, T configuration = default, Type[] extraTypes = null)
            => WriteToXml(configuration, configurationFilename, indent: true, extraTypes);

        /// <summary>
        /// Reads a new instance of the configuration class from a <see cref="Stream"/>.
        /// </summary>
        /// <typeparam name="T">The type of the configuration class.</typeparam>
        /// <param name="stream">The stream from which to read the configuration.</param>
        /// <param name="extraTypes">Extra types that are required for serialization.</param>
        /// <returns>The configuration object.</returns>
        public static T ReadFromStream<T>(Stream stream, Type[] extraTypes = null) => ReadFromXml<T>(stream, extraTypes);

        /// <summary>
        /// Writes the configuration to a stream.
        /// </summary>
        /// <typeparam name="T">The type of the configuration class.</typeparam>
        /// <param name="configuration">The configuration to write.</param>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="extraTypes">Extra types that are required for serialization.</param>
        public static void WriteToStream<T>(T configuration, Stream stream, Type[] extraTypes = null) => WriteToXml<T>(configuration, stream, true, extraTypes);

        /// <summary>
        /// Reads an object of type T from an XML file.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="filename">The name of the file to read from.</param>
        /// <param name="extraTypes">Extra types that are required for serialization.</param>
        private static T ReadFromXml<T>(string filename, Type[] extraTypes)
        {
            using var fileStream = File.OpenRead(filename);
            return ReadFromXml<T>(fileStream, extraTypes);
        }

        /// <summary>
        /// Reads an object of type T from an XML stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to read.</typeparam>
        /// <param name="stream">The stream from which to read the object.</param>
        /// <param name="extraTypes">Extra types that are required for serialization.</param>
        private static T ReadFromXml<T>(Stream stream, Type[] extraTypes)
        {
            var xmlSerializer = new XmlSerializer(typeof(T), extraTypes);
            return (T)xmlSerializer.Deserialize(stream);
        }

        /// <summary>
        /// Saves the configuration to an XML file.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="object">The object to write.</param>
        /// <param name="filename">The name of the file to write to.</param>
        /// <param name="indent">Whether to indent the XML.</param>
        /// <param name="extraTypes">Extra types that are required for serialization.</param>
        private static void WriteToXml<T>(T @object, string filename, bool indent, Type[] extraTypes)
        {
            using var fileStream = File.Create(filename);
            WriteToXml<T>(@object, fileStream, indent, extraTypes);
        }

        /// <summary>
        /// Saves the configuration to an XML stream.
        /// </summary>
        /// <typeparam name="T">The type of the object to write.</typeparam>
        /// <param name="object">The object to write.</param>
        /// <param name="stream">The stream to which to write the object.</param>
        /// <param name="indent">Whether to indent the XML.</param>
        /// <param name="extraTypes">Extra types that are required for serialization.</param>
        private static void WriteToXml<T>(T @object, Stream stream, bool indent, Type[] extraTypes)
        {
            using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = indent });
            var xmlSerializer = new XmlSerializer(typeof(T), extraTypes);
            xmlSerializer.Serialize(writer, @object);
        }
    }
}
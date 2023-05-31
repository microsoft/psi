// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System.IO;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// A component helper for managing component configuration.
    /// </summary>
    /// <typeparam name="T">The type of the configuration class.</typeparam>
    public class ConfigurationHelper<T>
        where T : class, new()
    {
        /// <summary>
        /// The configuration class.
        /// </summary>
        private T configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationHelper{T}"/> class.
        /// </summary>
        /// <param name="configurationFilename">The name of the configuration file to load.</param>
        public ConfigurationHelper(string configurationFilename)
        {
            if (!string.IsNullOrEmpty(configurationFilename))
            {
                // if file exists, try to load
                if (File.Exists(configurationFilename))
                {
                    this.LoadFromXml(configurationFilename);
                }
                else
                {
                    // otherwise create and save
                    this.configuration = new T();
                    this.SaveToXml(configurationFilename);
                }
            }
            else
            {
                // default configuration
                this.configuration = new T();
            }
        }

        /// <summary>
        /// Gets the configuration object.
        /// </summary>
        public T Configuration
        {
            get { return this.configuration; }
        }

        /// <summary>
        /// Loads an object of type T from XML.
        /// </summary>
        /// <param name="filename">The name of the file to load from.</param>
        private void LoadFromXml(string filename)
        {
            this.configuration = default(T);
            if (File.Exists(filename))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                using (var textReader = new StreamReader(filename))
                {
                    this.configuration = (T)xmlSerializer.Deserialize(textReader);
                }
            }
        }

        /// <summary>
        /// Saves the configuration to XML.
        /// </summary>
        /// <param name="filename">The name of the file to save to.</param>
        private void SaveToXml(string filename)
        {
            using (var writer = XmlWriter.Create(filename, new XmlWriterSettings() { Indent = true }))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                xmlSerializer.Serialize(writer, this.configuration);
            }
        }
    }
}

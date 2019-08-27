// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Serialization;

    /// <summary>
    /// Persisted application settings.
    /// </summary>
    public class PsiStudioSettings
    {
        private readonly string settingsFilename;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStudioSettings"/> class.
        /// </summary>
        public PsiStudioSettings()
        {
            // Set the path to the Settings file
            this.settingsFilename = Path.Combine(PsiStudioContext.PsiStudioDocumentsPath, "PsiStudioSettings.xml");

            // Set defaults for all settings
            this.WindowPositionLeft = "100";
            this.WindowPositionTop = "100";
            this.WindowWidth = "1024";
            this.WindowHeight = "768";
            this.WindowState = "Normal";
            this.TreeViewPanelWidth = "300";
            this.PropertiesPanelWidth = "300";
            this.DatasetsTabHeight = "400";
            this.CurrentLayoutName = null;

            // Update the settings with those from the file on disk
            this.LoadFromFile();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PsiStudioSettings"/> class.
        /// </summary>
        ~PsiStudioSettings()
        {
            using (var writer = XmlWriter.Create(this.settingsFilename, new XmlWriterSettings() { Indent = true }))
            {
                var xmlSerializer = new XmlSerializer(typeof(PsiStudioSettings));
                xmlSerializer.Serialize(writer, this);
            }
        }

        /// <summary>
        /// Gets or sets the main window left position.
        /// </summary>
        public string WindowPositionLeft { get; set; }

        /// <summary>
        /// Gets or sets the main window left position.
        /// </summary>
        public string WindowPositionTop { get; set; }

        /// <summary>
        /// Gets or sets the main window left position.
        /// </summary>
        public string WindowWidth { get; set; }

        /// <summary>
        /// Gets or sets the main window left position.
        /// </summary>
        public string WindowHeight { get; set; }

        /// <summary>
        /// Gets or sets the window state (normal, minimized, maximized).
        /// </summary>
        public string WindowState { get; set; }

        /// <summary>
        /// Gets or sets the width of the tree views panel.
        /// </summary>
        public string TreeViewPanelWidth { get; set; }

        /// <summary>
        /// Gets or sets the width of the properties panel.
        /// </summary>
        public string PropertiesPanelWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the datasets tab.
        /// </summary>
        public string DatasetsTabHeight { get; set; }

        /// <summary>
        /// Gets or sets the current layout.
        /// </summary>
        public string CurrentLayoutName { get; set; }

        /// <summary>
        /// Loads settings from the xml settings file.
        /// </summary>
        private void LoadFromFile()
        {
            Type thisType = this.GetType();

            // Load the settings XML file if it exists
            if (File.Exists(this.settingsFilename))
            {
                try
                {
                    XmlDocument settingsDocument = new XmlDocument();
                    settingsDocument.Load(this.settingsFilename);

                    // Get the list of PsiSettings
                    PropertyInfo[] properties = thisType.GetProperties();
                    foreach (PropertyInfo propertyInfo in properties)
                    {
                        // Check if this setting has a value in the settings file
                        XmlNode node = settingsDocument.DocumentElement.SelectSingleNode(string.Format("/{0}/{1}", thisType.Name, propertyInfo.Name));
                        if (node != null)
                        {
                            // Update the setting with the value from the settings file
                            propertyInfo.SetValue(this, node.InnerText);
                        }
                    }
                }
                catch (XmlException)
                {
                }
            }
        }
    }
}

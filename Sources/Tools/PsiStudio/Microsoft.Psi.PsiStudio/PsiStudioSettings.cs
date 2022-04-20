// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Windows;
    using System.Xml;
    using System.Xml.Serialization;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Persisted application settings.
    /// </summary>
    public class PsiStudioSettings
    {
        private string settingsFilename;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStudioSettings"/> class.
        /// </summary>
        public PsiStudioSettings()
        {
            // Set defaults for all settings
            this.WindowPositionLeft = "100";
            this.WindowPositionTop = "100";
            this.WindowWidth = "1024";
            this.WindowHeight = "768";
            this.WindowState = "Normal";
            this.TreeViewPanelWidth = "300";
            this.PropertiesPanelWidth = "300";
            this.DatasetsTabHeight = "400";
            this.ShowAbsoluteTiming = false;
            this.ShowTimingRelativeToSessionStart = false;
            this.ShowTimingRelativeToSelectionStart = false;
            this.CurrentLayoutName = null;
            this.AutoSaveDatasets = false;
            this.AutoLoadMRUDatasetOnStartUp = false;
            this.MRUDatasetFilename = null;
            this.AdditionalAssemblies = null;
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
        /// Gets or sets a value indicating whether the show absolute timing button should be pressed.
        /// </summary>
        public bool ShowAbsoluteTiming { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the show timing relative to session start button should be pressed.
        /// </summary>
        public bool ShowTimingRelativeToSessionStart { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the show timing relative to selection start button should be pressed.
        /// </summary>
        public bool ShowTimingRelativeToSelectionStart { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to set any open dataset object into autosave mode.
        /// </summary>
        public bool AutoSaveDatasets { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically load the most recently used dataset upon startup.
        /// </summary>
        public bool AutoLoadMRUDatasetOnStartUp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the most recently used dataset filename.
        /// </summary>
        public string MRUDatasetFilename { get; set; }

        /// <summary>
        /// Gets or sets the list of add-in assemblies.
        /// </summary>
        public string AdditionalAssemblies { get; set; }

        /// <summary>
        /// Gets the list of add-in components.
        /// </summary>
        [XmlIgnore]
        public List<string> AdditionalAssembliesList { get; private set; }

        /// <summary>
        /// Loads the settings from file.
        /// </summary>
        /// <param name="settingsFilename">The full name and path of the settings file.</param>
        /// <returns>The psi studio settings object that was loaded.</returns>
        public static PsiStudioSettings Load(string settingsFilename)
        {
            // Create the settings instance
            var settings = new PsiStudioSettings();

            // Update the settings with those from the file on disk
            settings.LoadFromFile(settingsFilename);

            // Generate the additional assemblies list
            settings.AdditionalAssembliesList = new List<string>();
            if (!string.IsNullOrWhiteSpace(settings.AdditionalAssemblies))
            {
                foreach (string additionalAssembly in settings.AdditionalAssemblies.Split(';'))
                {
                    settings.AdditionalAssembliesList.Add(additionalAssembly.Trim());
                }
            }

            return settings;
        }

        /// <summary>
        /// Saves the settings to the settings file.
        /// </summary>
        public void Save()
        {
            if (string.IsNullOrWhiteSpace(this.settingsFilename))
            {
                throw new InvalidOperationException("Could not save the settings to file because PsiStudioSettings.Load() was not previously called to set the filepath.");
            }

            // To avoid obliterating the existing settings file if something goes wrong, write
            // the settings to a temporary file and then copy the file over the existing settings
            // file once we know we were successful.
            string tempFilename = this.CreateTempFilename();

            try
            {
                using (var writer = XmlWriter.Create(tempFilename, new XmlWriterSettings() { Indent = true }))
                {
                    var xmlSerializer = new XmlSerializer(typeof(PsiStudioSettings));
                    xmlSerializer.Serialize(writer, this);
                }

                // Delete the existing settings file
                File.Delete(this.settingsFilename);

                // Move the temp file to be the new settings file
                File.Move(tempFilename, this.settingsFilename);
            }
            catch (Exception ex)
            {
                new MessageBoxWindow(
                    Application.Current.MainWindow,
                    "Save Settings Error",
                    $"An error occurred while attempting to save the application settings{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "Close",
                    null).ShowDialog();
            }
        }

        private string CreateTempFilename()
        {
            return this.settingsFilename.Substring(0, this.settingsFilename.IndexOf('.')) + ".tmp";
        }

        /// <summary>
        /// Loads settings from the xml settings file.
        /// </summary>
        private void LoadFromFile(string settingsFilename)
        {
            this.settingsFilename = settingsFilename;

            // Load the settings XML file if it exists
            if (File.Exists(this.settingsFilename))
            {
                try
                {
                    var settingsDocument = new XmlDocument() { XmlResolver = null };
                    var textReader = new System.IO.StreamReader(this.settingsFilename);
                    var reader = XmlReader.Create(textReader, new XmlReaderSettings() { XmlResolver = null });
                    settingsDocument.Load(reader);

                    // Get the list of settings
                    foreach (var propertyInfo in typeof(PsiStudioSettings).GetProperties())
                    {
                        // Check if this setting has a value in the settings file
                        var node = settingsDocument.DocumentElement.SelectSingleNode(string.Format("/{0}/{1}", typeof(PsiStudioSettings).Name, propertyInfo.Name));
                        if (node != null)
                        {
                            // Update the setting with the value from the settings file
                            propertyInfo.SetValue(this, Convert.ChangeType(node.InnerText, propertyInfo.PropertyType));
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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Xml;
    using System.Xml.Serialization;
    using Microsoft.Psi.Visualization.Windows;

    /// <summary>
    /// Persisted application settings.
    /// </summary>
    public class PsiStudioSettings
    {
        /// <summary>
        /// Current version of serialized settings file.
        /// </summary>
        /// <remarks>
        /// It is not necessary to bump the version when adding or removing properties unless the serialized
        /// schema for an existing property has changed in a way which necessitates special handling.
        /// </remarks>
        public const int CurrentVersion = 2;

        private string settingsFilename;

        /// <summary>
        /// Gets or sets the version of the settings object for serialization.
        /// </summary>
        /// <remarks>
        /// This defaults to 0 unless explicitly set in order to distinguish between older versions
        /// of the settings file (which did not have the Version attribute), and newer versions which
        /// which will contain the Version attribute set to CurrentVersion. Note that it is not necessary
        /// to bump CurrentVersion every time changes are made to PsiStudioSettings unless the serialized
        /// schema of an existing property has changed between versions and special handling is required
        /// for it. Simply adding or removing properties should not require a version change provided
        /// default values are provided for any added properties.
        /// </remarks>
        [XmlAttribute(AttributeName = "Version")]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets the main window left position.
        /// </summary>
        public string WindowPositionLeft { get; set; } = "100";

        /// <summary>
        /// Gets or sets the main window left position.
        /// </summary>
        public string WindowPositionTop { get; set; } = "100";

        /// <summary>
        /// Gets or sets the main window left position.
        /// </summary>
        public string WindowWidth { get; set; } = "1024";

        /// <summary>
        /// Gets or sets the main window left position.
        /// </summary>
        public string WindowHeight { get; set; } = "768";

        /// <summary>
        /// Gets or sets the window state (normal, minimized, maximized).
        /// </summary>
        public string WindowState { get; set; } = "Normal";

        /// <summary>
        /// Gets or sets the width of the tree views panel.
        /// </summary>
        public string TreeViewPanelWidth { get; set; } = "300";

        /// <summary>
        /// Gets or sets the width of the properties panel.
        /// </summary>
        public string PropertiesPanelWidth { get; set; } = "300";

        /// <summary>
        /// Gets or sets the height of the datasets tab.
        /// </summary>
        public string DatasetsTabHeight { get; set; } = "400";

        /// <summary>
        /// Gets or sets the name of the most recently used layout.
        /// </summary>
        public string MostRecentlyUsedLayoutName { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the show absolute timing button should be pressed.
        /// </summary>
        public bool ShowAbsoluteTiming { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the show timing relative to session start button should be pressed.
        /// </summary>
        public bool ShowTimingRelativeToSessionStart { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the show timing relative to selection start button should be pressed.
        /// </summary>
        public bool ShowTimingRelativeToSelectionStart { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to set any open dataset object into autosave mode.
        /// </summary>
        public bool AutoSaveDatasets { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically load the most recently used dataset upon startup.
        /// </summary>
        public bool AutoLoadMostRecentlyUsedDatasetOnStartUp { get; set; } = false;

        /// <summary>
        /// Gets or sets the list of most recently used dataset filenames.
        /// </summary>
        [XmlArray("MostRecentlyUsedDatasetFilenames")]
        [XmlArrayItem("Filename")]
        public List<string> MostRecentlyUsedDatasetFilenames { get; set; } = new ();

        /// <summary>
        /// Gets or sets the list of add-in assemblies.
        /// </summary>
        [XmlArray("AdditionalAssemblies")]
        [XmlArrayItem("Assembly")]
        public List<string> AdditionalAssemblies { get; set; } = new ();

        /// <summary>
        /// Gets or sets the set of type mappings.
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string> TypeMappings
        {
            get
            {
                var typeMappings = new Dictionary<string, string>();
                foreach (string typeMappingString in this.TypeMappingsAsStringList)
                {
                    var typeMappingKeyValue = typeMappingString.Split(':');

                    // Skip incorrect mapping strings which are not in the form "OldType:NewType"
                    if (typeMappingKeyValue.Length == 2)
                    {
                        typeMappings[typeMappingKeyValue[0].Trim()] = typeMappingKeyValue[1].Trim();
                    }
                }

                return typeMappings;
            }

            set
            {
                this.TypeMappingsAsStringList = value.Select(t => t.Key + ':' + t.Value).ToList();
            }
        }

        /// <summary>
        /// Gets or sets the string representation of the type mappings.
        /// </summary>
        [XmlArray("TypeMappings")]
        [XmlArrayItem("TypeMapping")]
        public List<string> TypeMappingsAsStringList { get; set; } = new ();

        /// <summary>
        /// Gets or sets a value indicating whether to show the security warning when loading
        /// third party assemblies or code.
        /// </summary>
        public bool ShowSecurityWarningOnLoadingThirdPartyCode { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the error log when loading
        /// third party assemblies.
        /// </summary>
        public bool ShowErrorLogOnLoadingAdditionalAssemblies { get; set; } = true;

        /// <summary>
        /// Loads the settings from file.
        /// </summary>
        /// <param name="psiStudioSettingsFilename">The full name and path of the settings file.</param>
        /// <returns>The psi studio settings object that was loaded.</returns>
        public static PsiStudioSettings Load(string psiStudioSettingsFilename)
        {
            // Create the settings instance
            var settings = new PsiStudioSettings();

            // Update the settings with those from the file on disk
            if (File.Exists(psiStudioSettingsFilename))
            {
                try
                {
                    using (var reader = XmlReader.Create(psiStudioSettingsFilename, new XmlReaderSettings() { XmlResolver = null }))
                    {
                        var xmlSerializer = new XmlSerializer(typeof(PsiStudioSettings));
                        settings = (PsiStudioSettings)xmlSerializer.Deserialize(reader);
                    }

                    // Attempt to read from an older version of the settings file
                    if (settings.Version < CurrentVersion)
                    {
                        settings.LoadFromPreviousVersionSettingsFile(psiStudioSettingsFilename);
                    }
                }
                catch (XmlException)
                {
                }
            }

            // Set the file name and current version
            settings.settingsFilename = psiStudioSettingsFilename;
            settings.Version = CurrentVersion;
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

        /// <summary>
        /// Adds the most recently used dataset filename to the most recently used list.
        /// Will promote it to the top if already present in the list.
        /// </summary>
        /// <param name="filename">The dataset filename to add to the most recently used list.</param>
        public void AddRecentlyUsedDatasetFilename(string filename)
        {
            int index = this.MostRecentlyUsedDatasetFilenames.IndexOf(filename);
            if (index == -1)
            {
                if (this.MostRecentlyUsedDatasetFilenames.Count() == 10)
                {
                    this.MostRecentlyUsedDatasetFilenames.RemoveAt(9);
                }

                this.MostRecentlyUsedDatasetFilenames.Insert(0, filename);
            }
            else if (index > 0)
            {
                // Move filename to the top of MRU list
                this.MostRecentlyUsedDatasetFilenames.RemoveAt(index);
                this.MostRecentlyUsedDatasetFilenames.Insert(0, filename);
            }
        }

        private string CreateTempFilename()
        {
            return this.settingsFilename.Substring(0, this.settingsFilename.IndexOf('.')) + ".tmp";
        }

        /// <summary>
        /// Loads settings from older version of the xml settings file.
        /// </summary>
        private void LoadFromPreviousVersionSettingsFile(string settingsFilename)
        {
            // Load the settings XML file if it exists
            if (File.Exists(settingsFilename))
            {
                try
                {
                    var settingsDocument = new XmlDocument() { XmlResolver = null };
                    var textReader = new StreamReader(settingsFilename);
                    var reader = XmlReader.Create(textReader, new XmlReaderSettings() { XmlResolver = null });
                    settingsDocument.Load(reader);

                    if (this.Version < 2)
                    {
                        var node = settingsDocument.DocumentElement.SelectSingleNode(string.Format("/{0}/{1}", typeof(PsiStudioSettings).Name, "MostRecentlyUsedDatasetFilename"));

                        // Settings files prior to version 2 only saved a single MostRecentlyUsedDatasetFilename rather than a list of most recently used datasets
                        if (node?.FirstChild?.NodeType == XmlNodeType.Text)
                        {
                            this.AddRecentlyUsedDatasetFilename(node.InnerText);
                        }
                    }

                    if (this.Version == 0)
                    {
                        var node = settingsDocument.DocumentElement.SelectSingleNode(string.Format("/{0}/{1}", typeof(PsiStudioSettings).Name, nameof(this.AdditionalAssemblies)));

                        // Version 0 saved AdditionalAssemblies as a single text element instead of an array
                        if (node?.FirstChild?.NodeType == XmlNodeType.Text)
                        {
                            // AdditionalAssemblies was previously serialized as a semicolon-separated string
                            this.AdditionalAssemblies.Clear();
                            this.AdditionalAssemblies.AddRange(node.InnerText.Split(';'));
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

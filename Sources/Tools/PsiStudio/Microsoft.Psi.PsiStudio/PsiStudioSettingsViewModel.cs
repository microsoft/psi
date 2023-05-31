// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using Microsoft.Psi.Data;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

    /// <summary>
    /// Implements a view model for Psi Studio settings.
    /// </summary>
    public class PsiStudioSettingsViewModel : ObservableObject, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> validationErrors = new ();
        private List<string> typeMappingsAsStringList;

        /// <summary>
        /// Initializes a new instance of the <see cref="PsiStudioSettingsViewModel"/> class.
        /// </summary>
        /// <param name="psiStudioSettings">The current psi studio settings.</param>
        public PsiStudioSettingsViewModel(PsiStudioSettings psiStudioSettings)
        {
            // Set defaults for all settings
            this.WindowPositionLeft = psiStudioSettings.WindowPositionLeft;
            this.WindowPositionTop = psiStudioSettings.WindowPositionTop;
            this.WindowWidth = psiStudioSettings.WindowWidth;
            this.WindowHeight = psiStudioSettings.WindowHeight;
            this.WindowState = psiStudioSettings.WindowState;
            this.TreeViewPanelWidth = psiStudioSettings.TreeViewPanelWidth;
            this.PropertiesPanelWidth = psiStudioSettings.PropertiesPanelWidth;
            this.DatasetsTabHeight = psiStudioSettings.DatasetsTabHeight;
            this.ShowAbsoluteTiming = psiStudioSettings.ShowAbsoluteTiming;
            this.ShowTimingRelativeToSessionStart = psiStudioSettings.ShowTimingRelativeToSessionStart;
            this.ShowTimingRelativeToSelectionStart = psiStudioSettings.ShowTimingRelativeToSelectionStart;
            this.MostRecentlyUsedLayoutName = psiStudioSettings.MostRecentlyUsedLayoutName;
            this.AutoSaveDatasets = psiStudioSettings.AutoSaveDatasets;
            this.AutoLoadMostRecentlyUsedDatasetOnStartUp = psiStudioSettings.AutoLoadMostRecentlyUsedDatasetOnStartUp;
            this.MostRecentlyUsedDatasetFilenames = psiStudioSettings.MostRecentlyUsedDatasetFilenames;

            // Generate a copy of the additional assemblies list
            this.AdditionalAssembliesAsStringList = psiStudioSettings.AdditionalAssemblies.ToList();

            // Generate a copy of the type mappings
            this.TypeMappingsAsStringList = psiStudioSettings.TypeMappingsAsStringList.ToList();

            this.ShowSecurityWarningOnLoadingThirdPartyCode = psiStudioSettings.ShowSecurityWarningOnLoadingThirdPartyCode;
            this.ShowErrorLogOnLoadingAdditionalAssemblies = psiStudioSettings.ShowErrorLogOnLoadingAdditionalAssemblies;
        }

        /// <inheritdoc/>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Gets or sets the main window left position.
        /// </summary>
        [Browsable(false)]
        public string WindowPositionLeft { get; set; }

        /// <summary>
        /// Gets or sets the main window top position.
        /// </summary>
        [Browsable(false)]
        public string WindowPositionTop { get; set; }

        /// <summary>
        /// Gets or sets the main window width.
        /// </summary>
        [Browsable(false)]
        public string WindowWidth { get; set; }

        /// <summary>
        /// Gets or sets the main window height.
        /// </summary>
        [Browsable(false)]
        public string WindowHeight { get; set; }

        /// <summary>
        /// Gets or sets the window state (normal, minimized, maximized).
        /// </summary>
        [Browsable(false)]
        public string WindowState { get; set; }

        /// <summary>
        /// Gets or sets the width of the tree views panel.
        /// </summary>
        [Browsable(false)]
        public string TreeViewPanelWidth { get; set; }

        /// <summary>
        /// Gets or sets the width of the properties panel.
        /// </summary>
        [Browsable(false)]
        public string PropertiesPanelWidth { get; set; }

        /// <summary>
        /// Gets or sets the height of the datasets tab.
        /// </summary>
        [Browsable(false)]
        public string DatasetsTabHeight { get; set; }

        /// <summary>
        /// Gets or sets the list of additional assemblies to load.
        /// </summary>
        [PropertyOrder(0)]
        [DisplayName("Additional Assemblies")]
        [Description("The list of additional third party assemblies to load at start-up.")]
        public List<string> AdditionalAssembliesAsStringList { get; set; }

        /// <summary>
        /// Gets or sets the string representation of the type mappings.
        /// </summary>
        [PropertyOrder(1)]
        [DisplayName("Type Mappings")]
        [Description("The list of type mappings to use when deserializing streams, specified as a collection of OldType:NewType.")]
        public List<string> TypeMappingsAsStringList
        {
            get => this.typeMappingsAsStringList;
            set
            {
                this.typeMappingsAsStringList = value;
                this.ValidateTypeMappingsAsStringList();
            }
        }

        /// <summary>
        /// Gets or sets the name of the most recently used layout.
        /// </summary>
        [PropertyOrder(2)]
        [DisplayName("Most Recently Used Layout")]
        [Description("The name of the most recently used layout.")]
        public string MostRecentlyUsedLayoutName { get; set; }

        /// <summary>
        /// Gets or sets the list of most recently used dataset filenames.
        /// </summary>
        [PropertyOrder(3)]
        [DisplayName("Most Recently Used Dataset Filenames")]
        [Description("The list of most recently used dataset filenames.")]
        public List<string> MostRecentlyUsedDatasetFilenames { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to set any open dataset object into autosave mode.
        /// </summary>
        [PropertyOrder(4)]
        [DisplayName("Auto Save Datasets")]
        [Description("Indicates whether datasets should be automatically saved when partitions/sessions are added or removed.")]
        public bool AutoSaveDatasets { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to automatically load the most recently used dataset upon startup.
        /// </summary>
        [PropertyOrder(5)]
        [DisplayName("Auto Load Most Recently Used Dataset")]
        [Description("Indicates whether the most recently used dataset should be automatically loaded at start-up.")]
        public bool AutoLoadMostRecentlyUsedDatasetOnStartUp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the show absolute timing button should be pressed.
        /// </summary>
        [PropertyOrder(6)]
        [DisplayName("Show Absolute Time")]
        [Description("Indicates whether the navigator should automatically show the Absolute Time clock.")]
        public bool ShowAbsoluteTiming { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the show timing relative to session start button should be pressed.
        /// </summary>
        [PropertyOrder(7)]
        [DisplayName("Show Time Relative to Session Start")]
        [Description("Indicates whether the navigator should automatically show the Time Relative to Session Start clock.")]
        public bool ShowTimingRelativeToSessionStart { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the show timing relative to selection start button should be pressed.
        /// </summary>
        [PropertyOrder(8)]
        [DisplayName("Show Time Relative to Selection Start")]
        [Description("Indicates whether the navigator should automatically show the Time Relative to Selection Start clock.")]
        public bool ShowTimingRelativeToSelectionStart { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to skip the security warning when loading
        /// third party assemblies or code.
        /// </summary>
        [PropertyOrder(9)]
        [DisplayName("Show Security Warning for Third Party Code")]
        [Description("Indicates whether to show the security warning when loading additional third party assemblies.")]
        public bool ShowSecurityWarningOnLoadingThirdPartyCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the error log when errors occur
        /// while loading third party assemblies or code.
        /// </summary>
        [PropertyOrder(10)]
        [DisplayName("Show Error Log when Loading Additional Assemblies")]
        [Description("Indicates whether to show the error log when errors occur while loading third party assemblies or code.")]
        public bool ShowErrorLogOnLoadingAdditionalAssemblies { get; set; }

        /// <summary>
        /// Gets the validation errors for this object.
        /// </summary>
        [Browsable(false)]
        public string Error
        {
            get
            {
                var errorText = new StringBuilder();
                if (this.validationErrors.Count > 0)
                {
                    errorText.AppendLine("There were errors with some of the settings values. Please correct them before saving.");
                    errorText.AppendLine();

                    // Concatenate the validation errors for each property
                    foreach (var errors in this.validationErrors)
                    {
                        errorText.AppendLine($"{errors.Key}:");
                        foreach (var error in errors.Value)
                        {
                            errorText.AppendLine(error);
                        }
                    }
                }

                return errorText.ToString();
            }
        }

        /// <inheritdoc/>
        [Browsable(false)]
        public bool HasErrors => this.validationErrors.Count > 0;

        /// <inheritdoc/>
        public IEnumerable GetErrors(string propertyName)
        {
            return this.validationErrors.TryGetValue(propertyName, out var errors) ? errors : null;
        }

        /// <summary>
        /// Updates the supplied PsiStudio settings object.
        /// </summary>
        /// <param name="settings">The PsiStudio settings object to update.</param>
        /// <returns>True if the configuration update requires a PsiStudio restart.</returns>
        public bool UpdateSettings(PsiStudioSettings settings)
        {
            var requiresRestart = false;
            var existingAssemblies = settings.AdditionalAssemblies.ToList();
            if (this.AdditionalAssembliesAsStringList.Any(s => !existingAssemblies.Contains(s)))
            {
                requiresRestart = true;
            }

            var existingTypeMappings = settings.TypeMappingsAsStringList;
            if (this.TypeMappingsAsStringList.Any(s => !existingTypeMappings.Contains(s)))
            {
                requiresRestart = true;
            }

            settings.WindowPositionLeft = this.WindowPositionLeft;
            settings.WindowPositionTop = this.WindowPositionTop;
            settings.WindowWidth = this.WindowWidth;
            settings.WindowHeight = this.WindowHeight;
            settings.WindowState = this.WindowState;
            settings.TreeViewPanelWidth = this.TreeViewPanelWidth;
            settings.PropertiesPanelWidth = this.PropertiesPanelWidth;
            settings.DatasetsTabHeight = this.DatasetsTabHeight;
            settings.ShowAbsoluteTiming = this.ShowAbsoluteTiming;
            settings.ShowTimingRelativeToSessionStart = this.ShowTimingRelativeToSessionStart;
            settings.ShowTimingRelativeToSelectionStart = this.ShowTimingRelativeToSelectionStart;
            settings.MostRecentlyUsedLayoutName = this.MostRecentlyUsedLayoutName;
            settings.AutoSaveDatasets = this.AutoSaveDatasets;
            settings.AutoLoadMostRecentlyUsedDatasetOnStartUp = this.AutoLoadMostRecentlyUsedDatasetOnStartUp;
            settings.MostRecentlyUsedDatasetFilenames = this.MostRecentlyUsedDatasetFilenames;
            settings.AdditionalAssemblies = this.AdditionalAssembliesAsStringList;
            settings.TypeMappingsAsStringList = this.TypeMappingsAsStringList;
            settings.ShowSecurityWarningOnLoadingThirdPartyCode = this.ShowSecurityWarningOnLoadingThirdPartyCode;
            settings.ShowErrorLogOnLoadingAdditionalAssemblies = this.ShowErrorLogOnLoadingAdditionalAssemblies;

            return requiresRestart;
        }

        /// <summary>
        /// Validator for the TypeMappingsAsStringList property.
        /// </summary>
        private void ValidateTypeMappingsAsStringList()
        {
            var typeMappings = new Dictionary<string, string>();
            var syntaxErrors = new List<string>();
            var duplicates = new List<string>();

            foreach (string mapping in this.TypeMappingsAsStringList)
            {
                // Ignore whitespace entries
                if (!string.IsNullOrWhiteSpace(mapping))
                {
                    // Ensure that mapping has the correct syntax of OldType:NewType
                    var types = mapping.Split(':');
                    if (types.Length != 2)
                    {
                        syntaxErrors.Add(mapping);
                    }
                    else
                    {
                        // Ensure there are no duplicate mappings for OldType
                        string fromType = types[0].Trim();
                        string toType = types[1].Trim();
                        if (typeMappings.ContainsKey(fromType))
                        {
                            duplicates.Add(fromType);
                        }
                        else
                        {
                            typeMappings.Add(fromType, toType);
                        }
                    }
                }
            }

            // Construct the validation error messages for this property
            var errors = new List<string>();

            if (syntaxErrors.Count > 0)
            {
                var errorText = new StringBuilder();
                errorText.AppendLine(
                    "There was an error with some of the type mapping(s). Each mapping should be on " +
                    "a separate line and be a pair of fully-qualified type names (e.g. OldType:NewType) " +
                    "separated by a colon. Please correct the following type mapping entries:");

                syntaxErrors.ForEach(mapping => errorText.AppendLine($"- {mapping}"));

                errors.Add(errorText.ToString());
            }

            if (duplicates.Count > 0)
            {
                var errorText = new StringBuilder();
                errorText.AppendLine("Duplicate mappings were found for the following types. Please enter only one mapping per type.");
                duplicates.ForEach(type => errorText.AppendLine($"- {type}"));

                errors.Add(errorText.ToString());
            }

            // Set the error text if there were validation errors
            if (errors.Count > 0)
            {
                this.validationErrors[nameof(this.TypeMappingsAsStringList)] = errors;
            }
            else
            {
                this.validationErrors.Remove(nameof(this.TypeMappingsAsStringList));
            }

            this.RaiseErrorsChanged(nameof(this.TypeMappingsAsStringList));
        }

        /// <summary>
        /// Raises the <see cref="ErrorsChanged"/> event the specified property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        private void RaiseErrorsChanged(string propertyName)
        {
            this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }
}

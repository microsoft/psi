// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Windows
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.Psi.PsiStudio.TypeSpec;
    using Microsoft.Psi.Visualization.DataTypes;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.ViewModels;

    /// <summary>
    /// Interaction logic for ScriptWindow.xaml.
    /// </summary>
    public partial class ScriptWindow : Window, INotifyPropertyChanged
    {
        private readonly StreamTreeNode streamTreeNode = null;
        private string returnTypeName = string.Empty;
        private Type returnType = null;
        private string scriptText = string.Empty;
        private string scriptDerivedStreamName = string.Empty;
        private bool isValidating = false;
        private string errorMessage = string.Empty;
        private int selectedUsingIndex = -1;
        private Assembly[] loadedAssemblies = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptWindow"/> class.
        /// </summary>
        /// <param name="owner">The window owner.</param>
        /// <param name="streamTreeNode">The stream tree node for the the stream for which to create an initial script (if any).</param>
        /// <param name="isNewScript">Indicates whether we are creating a new script or editing an existing one (name and return type cannot be changed for existing scripts).</param>
        public ScriptWindow(Window owner, StreamTreeNode streamTreeNode, bool isNewScript = true)
        {
            // Add some initial usings
            var initialUsings = new HashSet<string>
            {
                "System",
                streamTreeNode.DataType.Namespace,
            };

            // Add generic type params of the stream type as well
            if (streamTreeNode.DataType.IsGenericType)
            {
                foreach (var typeArg in streamTreeNode.DataType.GenericTypeArguments)
                {
                    initialUsings.Add(typeArg.Namespace);
                }
            }

            this.Usings = new ObservableCollection<string>(initialUsings);

            this.Owner = owner;
            this.streamTreeNode = streamTreeNode;
            this.IsNewScript = isNewScript;
            this.Title = isNewScript ? "Create New Script For Derived Stream" : "Edit Script";

            this.InitializeComponent();
            this.DataContext = this;

            int i = 0;
            this.scriptDerivedStreamName = "DerivedStream";
            while (this.streamTreeNode.Children.Any(node => node.Name == $"{this.scriptDerivedStreamName}"))
            {
                this.scriptDerivedStreamName = $"DerivedStream_{++i}";
            }

            // Create some initial script text for the user.
            this.ScriptText = "m";
            this.ReturnTypeName = "object";
        }

        /// <summary>
        /// The event fired when a bound property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the collection of usings/imports for the script engine.
        /// </summary>
        public ObservableCollection<string> Usings { get; set; }

        /// <summary>
        /// Gets the window title.
        /// </summary>
        public string WindowTitle => this.IsNewScript ? "Create Script Derived Stream" : "Modify Script Derived Stream";

        /// <summary>
        /// Gets the content for the OK button.
        /// </summary>
        public string OKButtonContent => this.IsNewScript ? "Create" : "Modify";

        /// <summary>
        /// Gets a value indicating whether there is an error message.
        /// </summary>
        public bool HasErrorMessage => !string.IsNullOrEmpty(this.ErrorMessage);

        /// <summary>
        /// Gets or sets the index of the currently selected using.
        /// </summary>
        public int SelectedUsingIndex
        {
            get { return this.selectedUsingIndex; }

            set
            {
                this.selectedUsingIndex = value;
                this.OnPropertyChanged(nameof(this.SelectedUsingIndex));
            }
        }

        /// <summary>
        /// Gets or sets the text of the user script.
        /// </summary>
        public string ScriptText
        {
            get { return this.scriptText; }

            set
            {
                this.scriptText = value;
                this.OnPropertyChanged(nameof(this.ScriptText));
            }
        }

        /// <summary>
        /// Gets or sets the name of the script derived stream.
        /// </summary>
        public string ScriptDerivedStreamName
        {
            get { return this.scriptDerivedStreamName; }

            set
            {
                this.scriptDerivedStreamName = value;
                this.OnPropertyChanged(nameof(this.ScriptDerivedStreamName));
            }
        }

        /// <summary>
        /// Gets or sets the return type name.
        /// </summary>
        public string ReturnTypeName
        {
            get { return this.returnTypeName; }

            set
            {
                this.returnTypeName = value;
                this.OnPropertyChanged(nameof(this.ReturnTypeName));
            }
        }

        /// <summary>
        /// Gets or sets the return type of the script.
        /// </summary>
        public Type ReturnType
        {
            get { return this.returnType; }

            set
            {
                this.returnType = value;
                this.ReturnTypeName = TypeSpec.GetCodeFriendlyName(this.returnType);
            }
        }

        /// <summary>
        /// Gets a value indicating whether we are currently validating the script.
        /// </summary>
        public bool IsValidating
        {
            get { return this.isValidating; }

            private set
            {
                this.isValidating = value;
                this.OnPropertyChanged(nameof(this.IsValidating));
                this.OnPropertyChanged(nameof(this.IsNotValidating));
            }
        }

        /// <summary>
        /// Gets a value indicating whether we are currently not validating the script.
        /// </summary>
        public bool IsNotValidating => !this.IsValidating;

        /// <summary>
        /// Gets a value indicating whether we are creating a new script.
        /// </summary>
        public bool IsNewScript { get; }

        /// <summary>
        /// Gets the error text generated during the last executio of the script.
        /// </summary>
        public string ErrorMessage
        {
            get { return this.errorMessage; }

            private set
            {
                this.errorMessage = value;
                this.OnPropertyChanged(nameof(this.ErrorMessage));
                this.OnPropertyChanged(nameof(this.HasErrorMessage));
            }
        }

        /// <summary>
        /// Gets a list of loaded assemblies.
        /// </summary>
        private Assembly[] LoadedAssemblies => this.loadedAssemblies ??= AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)).ToArray();

        /// <summary>
        /// Gets the script options.
        /// </summary>
        private ScriptOptions ScriptOptions => ScriptOptions.Default.WithReferences(this.LoadedAssemblies).WithImports(this.Usings);

        /// <summary>
        /// Called when a bound property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task<bool> Validate()
        {
            return this.ValidateScriptName() &&
                await this.ValidateReturnType() &&
                await this.ValidateScript();
        }

        private bool ValidateScriptName()
        {
            // Only validate script name when creating a new script
            if (this.IsNewScript)
            {
                if (string.IsNullOrWhiteSpace(this.ScriptDerivedStreamName))
                {
                    this.ErrorMessage = "Please specify a script name.";
                    return false;
                }
                else if (this.streamTreeNode.Children.Any(node => node.Name == $"{this.ScriptDerivedStreamName}"))
                {
                    this.ErrorMessage = $"There is already a derived stream named {this.ScriptDerivedStreamName}. Please specify a unique name for the script.";
                    return false;
                }
                else if (this.ScriptDerivedStreamName.Contains('.'))
                {
                    this.ErrorMessage = $"The script derived stream name cannot contain the '.' character.";
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> ValidateReturnType()
        {
            if (string.IsNullOrWhiteSpace(this.ReturnTypeName))
            {
                this.ErrorMessage = "Please enter a return type for the script.";
                return false;
            }

            try
            {
                return await Task.Run(async () =>
                {
                    // Compute the actual script return Type from the user-specified type name
                    var result = await CSharpScript.RunAsync<Type>($"typeof({this.returnTypeName})", this.ScriptOptions);
                    this.ReturnType = result.ReturnValue;
                    return true;
                });
            }
            catch (CompilationErrorException cee)
            {
                this.ErrorMessage = $"Error evaluating script return type {this.ReturnTypeName}." +
                    Environment.NewLine + cee.Diagnostics.EnumerableToString(Environment.NewLine);

                return false;
            }
        }

        private async Task<bool> ValidateScript()
        {
            try
            {
                // Create and compile the script to validate it
                return await Task.Run(() =>
                {
                    // Create the generic method CSharpScript.Create<T>(string, ...)
                    var createScriptMethod = typeof(CSharpScript).GetMethods()
                        .Where(m => m.Name == nameof(CSharpScript.Create))
                        .FirstOrDefault(m => m.IsGenericMethod && m.GetParameters()[0].ParameterType == typeof(string))
                        .MakeGenericMethod(this.ReturnType);

                    var globalsType = typeof(ScriptGlobals<>).MakeGenericType(this.streamTreeNode.DataType);
                    dynamic script = createScriptMethod.Invoke(null, new object[] { this.ScriptText, this.ScriptOptions, globalsType, null });

                    ImmutableArray<Diagnostic> diagnostics = script.Compile();

                    this.ErrorMessage = diagnostics.EnumerableToString(Environment.NewLine);
                    return diagnostics.IsEmpty;
                });
            }
            catch (CompilationErrorException cee)
            {
                // Errors that occurred before the call to script.Compile
                this.ErrorMessage = cee.Diagnostics.EnumerableToString(Environment.NewLine);
                return false;
            }
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear any old validation errors and re-validate
            this.ErrorMessage = string.Empty;
            this.IsValidating = true;

            if (await this.Validate())
            {
                // No errors so we can indicate a successful DialogResult
                this.DialogResult = true;
                e.Handled = true;
            }

            this.IsValidating = false;
        }

        private void AddUsingButton_Click(object sender, RoutedEventArgs e)
        {
            var addUsingDialog = new GetParameterWindow(this, "Add Using Clause", "Using", string.Empty);
            if (addUsingDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(addUsingDialog.ParameterValue))
            {
                this.Usings.Add(addUsingDialog.ParameterValue);
            }
        }

        private void RemoveUsingButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.SelectedUsingIndex >= 0 && this.SelectedUsingIndex < this.Usings.Count)
            {
                this.Usings.RemoveAt(this.SelectedUsingIndex);
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Psi.PsiStudio.Common;
    using Microsoft.Psi.Visualization.Datasets;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Win32;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PsiStudioContext context = PsiStudioContext.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            var args = Environment.GetCommandLineArgs();
            int argIndex = 1;
            bool forceRegistration = false;
            bool embedding = false;
            bool skipRegistration = false;
            string filename = null;
            while (argIndex < args.Length)
            {
                if (args[argIndex][0] == '-')
                {
                    if (args[argIndex] == "-ForceRegistration")
                    {
                        forceRegistration = true;
                    }
                    else if (args[argIndex] == "-SkipRegistration")
                    {
                        skipRegistration = true;
                    }
                    else if (args[argIndex] == "-Embedding")
                    {
                        embedding = true;
                    }
                }
                else
                {
                    filename = args[argIndex];
                }

                argIndex++;
            }

            if (!skipRegistration)
            {
                this.RegisterApplication(forceRegistration);
            }

            this.Loaded += (s, e) => this.Activate();

            this.context.VisualizationContainer = new VisualizationContainer();
            this.context.VisualizationContainer.Navigator.ViewRange.SetRange(DateTime.UtcNow, TimeSpan.FromSeconds(60));

            if (!embedding && !string.IsNullOrWhiteSpace(filename))
            {
                // register an async handler to open the dataset once the main window has finished loading
                this.Loaded += async (s, e) => await this.context.OpenDatasetAsync(filename);
            }

            this.DataContext = this.context;
        }

        private void RegisterApplication(bool forceRegistration)
        {
            var key = Microsoft.Win32.Registry.LocalMachine;
            Assembly asm = Assembly.GetExecutingAssembly();
            string psiExePath = Path.GetDirectoryName(asm.Location);
            string psiPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\{8F2D3209-E9CF-49A0-BCCB-18264AC9F676}\LocalServer32", null, null) as string;
            if (forceRegistration || psiPath == null || psiPath != asm.Location)
            {
                // Extract the .reg file from our resources and write it to a temp file
                Stream strm = asm.GetManifestResourceStream("Microsoft.Psi.PsiStudio.PsiStudio.reg");
                StreamReader strmReader = new StreamReader(strm);
                string tmpFile = Path.GetTempFileName();
                StreamWriter strmWriter = new StreamWriter(tmpFile);
                while (!strmReader.EndOfStream)
                {
                    string str = strmReader.ReadLine();
                    string modifiedPath = psiExePath.Replace("\\", "\\\\");
                    string str2 = str.Replace("[INSTALLFOLDER]", modifiedPath);

                    strmWriter.WriteLine(str2);
                }

                strmWriter.Close();
                strmReader.Close();

                // Now start "reg" using that temp file
                var proc = new System.Diagnostics.Process();
                try
                {
                    proc.StartInfo.FileName = "reg.exe";
                    proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    proc.StartInfo.CreateNoWindow = false;
                    proc.StartInfo.UseShellExecute = true;
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.Arguments = "import " + tmpFile + " /reg:64";
                    proc.Start();
                    proc.WaitForExit();

                    proc.StartInfo.FileName = "reg.exe";
                    proc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    proc.StartInfo.CreateNoWindow = false;
                    proc.StartInfo.UseShellExecute = true;
                    proc.StartInfo.Verb = "runas";
                    proc.StartInfo.Arguments = "import " + tmpFile + " /reg:32";
                    proc.Start();
                    proc.WaitForExit();
                }
                finally
                {
                    proc.Dispose();
                }
            }
        }

        private void AddPartition_Click(object sender, RoutedEventArgs e)
        {
            var session = ((MenuItem)sender).DataContext as SessionViewModel;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".psi";
            dlg.Filter = "Psi Store (.psi)|*.psi|Psi Annotation Store (.pas)|*.pas";
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                var fileInfo = new FileInfo(dlg.FileName);
                var name = fileInfo.Name.Split('.')[0];

                if (fileInfo.Extension == ".psi")
                {
                    session.AddStorePartition(name, fileInfo.DirectoryName);
                }
                else if (fileInfo.Extension == ".pas")
                {
                    session.AddAnnotationPartition(name, fileInfo.DirectoryName);
                }
                else
                {
                    throw new ApplicationException("Invalid file type selected when adding partition.");
                }

                this.context.DatasetViewModel.CurrentSessionViewModel = session;
                this.context.VisualizationContainer.ZoomToRange(session.OriginatingTimeInterval);
            }
        }

        private void VisualizeSession_Click(object sender, RoutedEventArgs e)
        {
            var session = ((MenuItem)sender).DataContext as SessionViewModel;
            this.context.DatasetViewModel.CurrentSessionViewModel = session;
            this.context.VisualizationContainer.Navigator.DataRange.SetRange(session.OriginatingTimeInterval);
            this.context.VisualizationContainer.ZoomToRange(session.OriginatingTimeInterval);
            this.context.VisualizationContainer.UpdateStoreBindings(session.PartitionViewModels.ToList());
        }

        private void VisualizePartition_Click(object sender, RoutedEventArgs e)
        {
            var partition = ((MenuItem)sender).DataContext as PartitionViewModel;
            this.context.DatasetViewModel.CurrentSessionViewModel = partition.SessionViewModel;
            this.context.VisualizationContainer.ZoomToRange(partition.SessionViewModel.OriginatingTimeInterval);
            this.context.VisualizationContainer.UpdateStoreBindings(new PartitionViewModel[] { partition });
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }

            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = default(Thickness);
            }
        }

        private void StreamTreeNode_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // If the left button is also pressed, then the user is probably wanting to
            // initiate a drag operation of the stream into the Visualization Container
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Get the Tree Item that sent the event
                StackPanel treeNode = sender as StackPanel;
                if (treeNode != null)
                {
                    IStreamTreeNode streamTreeNode = treeNode.DataContext as IStreamTreeNode;
                    if (streamTreeNode != null)
                    {
                        // Begin the Drag & Drop operation
                        DataObject data = new DataObject();
                        data.SetData(DragDropDataName.DragDropOperation, DragDropOperation.DragDropStream);
                        data.SetData(DragDropDataName.StreamTreeNode, streamTreeNode);

                        DragDrop.DoDragDrop(treeNode, data, DragDropEffects.Move);
                    }
                }
            }
        }
    }
}

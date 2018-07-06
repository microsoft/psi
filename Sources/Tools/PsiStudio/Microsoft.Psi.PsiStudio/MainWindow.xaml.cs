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
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Datasets;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;
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

            this.Closed += this.MainWindow_Closed;
            this.Loaded += this.MainWindow_Loaded;

            this.context.VisualizationContainer = new VisualizationContainer();
            this.context.VisualizationContainer.Navigator.ViewRange.SetRange(DateTime.UtcNow, TimeSpan.FromSeconds(60));

            if (!embedding && !string.IsNullOrWhiteSpace(filename))
            {
                this.context.OpenDataset(filename);
                this.TabControl.SelectedItem = this.Datasets;
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

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            // Ensure playback is stopped before exiting
            this.context.StopPlaying();

            // Explicitly dispose so that DataManager doesn't keep the app running for a while longer.
            DataManager.Instance?.Dispose();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Activate();
        }

        private void DeleteVisualization_Click(object sender, RoutedEventArgs e)
        {
            var visualizationPanel = this.VisualizationTreeView.SelectedItem as VisualizationPanel;
            var visualizationObject = this.VisualizationTreeView.SelectedItem as VisualizationObject;
            if (visualizationPanel != null)
            {
                visualizationPanel.Container.RemovePanel(visualizationPanel);
            }
            else if (visualizationObject != null)
            {
                visualizationObject.Panel.RemoveVisualizationObject(visualizationObject);
            }

            e.Handled = true;
        }

        private void VisualizationTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var visualizationPanel = this.VisualizationTreeView.SelectedItem as VisualizationPanel;
            var visualizationObject = this.VisualizationTreeView.SelectedItem as VisualizationObject;
            if (visualizationPanel != null)
            {
                visualizationPanel.Container.CurrentPanel = visualizationPanel;
            }
            else if (visualizationObject != null)
            {
                visualizationObject.Container.CurrentPanel = visualizationObject.Panel;
                visualizationObject.Panel.CurrentVisualizationObject = visualizationObject;
            }

            e.Handled = true;
        }

        private void AnnotationAdd_Click(object sender, RoutedEventArgs e)
        {
            this.context.AddAnnotation(this);
            e.Handled = true;
        }

        private void LoadLayout_Click(object sender, RoutedEventArgs e)
        {
            Win32.OpenFileDialog dlg = new Win32.OpenFileDialog();
            dlg.DefaultExt = ".plo";
            dlg.Filter = "Psi Layout (.plo)|*.plo";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                this.context.OpenLayout(filename);
                this.TabControl.SelectedItem = this.Visualizations;
            }
        }

        private void SaveLayout_Click(object sender, RoutedEventArgs e)
        {
            Win32.SaveFileDialog dlg = new Win32.SaveFileDialog();
            dlg.DefaultExt = ".plo";
            dlg.Filter = "Psi Layout (.plo)|*.plo";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                this.context.VisualizationContainer.Save(filename);
            }
        }

        private void OpenDataset_Click(object sender, RoutedEventArgs e)
        {
            Win32.OpenFileDialog dlg = new Win32.OpenFileDialog();
            dlg.DefaultExt = ".pds";
            dlg.Filter = "Psi Dataset (.pds)|*.pds";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                this.context.OpenDataset(filename);
                this.TabControl.SelectedItem = this.Datasets;
            }
        }

        private void OpenStore_Click(object sender, RoutedEventArgs e)
        {
            Win32.OpenFileDialog dlg = new Win32.OpenFileDialog();
            dlg.DefaultExt = ".psi";
            dlg.Filter = "Psi Store (.psi)|*.psi";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                this.context.OpenDataset(filename);
                this.TabControl.SelectedItem = this.Datasets;
            }
        }

        private void SaveDataset_Click(object sender, RoutedEventArgs e)
        {
            Win32.SaveFileDialog dlg = new Win32.SaveFileDialog();
            dlg.DefaultExt = ".pds";
            dlg.Filter = "Psi Dataset (.pds)|*.pds";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                string filename = dlg.FileName;
                this.context.DatasetViewModel.Save(filename);
            }
        }

        private void InsertTimelinePanel_Click(object sender, RoutedEventArgs e)
        {
            this.context.VisualizationContainer.AddPanel(new TimelineVisualizationPanel());
        }

        private void Insert2DPanel_Click(object sender, RoutedEventArgs e)
        {
            this.context.VisualizationContainer.AddPanel(new XYVisualizationPanel());
        }

        private void Insert3DPanel_Click(object sender, RoutedEventArgs e)
        {
            this.context.VisualizationContainer.AddPanel(new XYZVisualizationPanel());
        }

        private void PlaybackPlay_Click(object sender, RoutedEventArgs e)
        {
            if (this.context.VisualizationContainer.Navigator.NavigationMode == Visualization.Navigation.NavigationMode.Live ||
                this.context.DatasetViewModel?.CurrentSessionViewModel?.PartitionViewModels.FirstOrDefault() == null)
            {
                return;
            }

            this.context.Play();
        }

        private void ZoomToSessionExtents_Click(object sender, RoutedEventArgs e)
        {
            this.context.VisualizationContainer.Navigator.ZoomToDataRange();
        }

        private void ZoomToSelection_Click(object sender, RoutedEventArgs e)
        {
            this.context.VisualizationContainer.Navigator.ZoomToSelection();
        }

        private void PlaybackStopPlaying_Click(object sender, RoutedEventArgs e)
        {
            if (this.context.VisualizationContainer.Navigator.NavigationMode == Visualization.Navigation.NavigationMode.Live ||
                this.context.DatasetViewModel?.CurrentSessionViewModel?.PartitionViewModels.FirstOrDefault() == null)
            {
                return;
            }

            this.context.StopPlaying();
        }

        private void DatasetsTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
        }

        private void CloseDataset_Click(object sender, RoutedEventArgs e)
        {
        }

        private void RemovePartition_Click(object sender, RoutedEventArgs e)
        {
            var partition = ((MenuItem)sender).DataContext as PartitionViewModel;
            partition.RemovePartition();
        }

        private void RemoveSession_Click(object sender, RoutedEventArgs e)
        {
            var session = ((MenuItem)sender).DataContext as SessionViewModel;
            session.RemoveSession();
        }

        private void CreateSession_Click(object sender, RoutedEventArgs e)
        {
            var dataset = ((MenuItem)sender).DataContext as DatasetViewModel;
            dataset.CreateSession();
        }

        private void CreateSessionFromExistingStore_Click(object sender, RoutedEventArgs e)
        {
            var dataset = ((MenuItem)sender).DataContext as DatasetViewModel;

            Win32.OpenFileDialog dlg = new Win32.OpenFileDialog();
            dlg.DefaultExt = ".psi";
            dlg.Filter = "Psi Store (.psi)|*.psi";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                var fileInfo = new FileInfo(dlg.FileName);
                var name = fileInfo.Name.Split('.')[0];
                dataset.AddSessionFromExistingStore(name, name, fileInfo.DirectoryName);
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

        private void AddPartition_Click(object sender, RoutedEventArgs e)
        {
            var session = ((MenuItem)sender).DataContext as SessionViewModel;

            Win32.OpenFileDialog dlg = new Win32.OpenFileDialog();
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
    }
}

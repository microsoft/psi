// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.PsiStudio
{
    using System.Windows;
    using Microsoft.Psi.Visualization.Server;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <inheritdoc />
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            ComServer.Instance.Uninitialize();
        }

        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ComServer.Instance.Initialize(Guids.RemoteVisualizationServiceCLSID, new ServerClassFactory());
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1402 // File may only contain a single class.

namespace Microsoft.Psi.PsiStudio
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Psi.Data;
    using Microsoft.Psi.Visualization.Data;
    using Microsoft.Psi.Visualization.Datasets;
    using Microsoft.Psi.Visualization.Server;
    using Newtonsoft.Json;

    /// <summary>
    /// Class implements the service API singleton for remote access to PsiStudio.
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    [Guid(Guids.RemoteVisualizationServiceCLSIDString)]
    [ComVisible(true)]
    public class VisualizationService : ReferenceCountedObject, IRemoteVisualizationService
    {
        /// <summary>
        /// The <see cref="VisualizationService"/> singleton.
        /// </summary>
        public static readonly VisualizationService Instance;

        private SessionViewModel currentSessionViewModel;

        static VisualizationService()
        {
            VisualizationService.Instance = new VisualizationService();
        }

        /// <summary>
        /// Gets the current visualization container.
        /// </summary>
        public IRemoteVisualizationContainer CurrentContainer => PsiStudioContext.Instance.VisualizationContainer;

        /// <summary>
        /// COM registration function.
        /// </summary>
        /// <param name="t">Type of object being registered.</param>
        [ComRegisterFunction]
        public static void Register(Type t)
        {
            try
            {
                ComHelper.RegasmRegisterLocalServer(t);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// COM unregistration function.
        /// </summary>
        /// <param name="t">Type of object being unregistered.</param>
        [ComUnregisterFunction]
        public static void Unregister(Type t)
        {
            try
            {
                ComHelper.RegasmUnregisterLocalServer(t);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public void EnsureBinding(string jsonStreamBinding)
        {
            this.EnsureSession();
            this.EnsurePartition(JsonConvert.DeserializeObject<StreamBinding>(jsonStreamBinding));
        }

        private void EnsurePartition(StreamBinding streamBinding)
        {
            this.currentSessionViewModel.AddStorePartition(streamBinding.StoreName, streamBinding.StorePath, streamBinding.PartitionName);
        }

        private void EnsureSession()
        {
            var session = PsiStudioContext.Instance.DatasetViewModel.SessionViewModels.FirstOrDefault((s) => s.Name == Session.DefaultName);
            if (session == null)
            {
                session = PsiStudioContext.Instance.DatasetViewModel.CreateSession();
            }

            this.currentSessionViewModel = session;
        }
    }

    /// <summary>
    /// Class factory for the class Server.
    /// </summary>
    public class ServerClassFactory : StandardOleMarshalObject, IClassFactory
    {
        /// <inheritdoc />
        public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
        {
            ppvObject = IntPtr.Zero;
            if (pUnkOuter != IntPtr.Zero)
            {
                // The pUnkOuter parameter was non-NULL and the object does not support aggregation.
                Marshal.ThrowExceptionForHR(ComNative.CLASS_E_NOAGGREGATION);
            }

            if (riid == Guids.IRemoteVisualizationServiceIID || riid == new Guid(ComNative.IDispatchIIDString) || riid == new Guid(ComNative.IUnknownIIDString))
            {
                // Create the instance of the .NET object
                ppvObject = Marshal.GetComInterfaceForObject(VisualizationService.Instance, typeof(IRemoteVisualizationService));
            }
            else
            {
                // The object that ppvObject points to does not support the interface identified by riid.
                Marshal.ThrowExceptionForHR(ComNative.E_NOINTERFACE);
            }

            return 0;
        }

        /// <inheritdoc />
        public int LockServer(bool fLock)
        {
            return 0;
        }
    }
}

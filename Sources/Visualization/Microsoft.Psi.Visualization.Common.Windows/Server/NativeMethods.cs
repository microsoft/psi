// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Server
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Native methods
    /// </summary>
    internal class NativeMethods
    {
        /// <summary>
        /// Indicates a request to terminate an application, and is generated when the application calls the PostQuitMessage function.
        /// This message causes the GetMessage function to return zero.
        /// </summary>
        internal const int WM_QUIT = 0x0012;

        /// <summary>
        /// Registers an EXE class object with OLE so other applications can connect to it. EXE object applications should call CoRegisterClassObject on startup.
        /// It can also be used to register internal objects for use by the same EXE or other code (such as DLLs) that the EXE uses.
        /// </summary>
        /// <param name="rclsid">CLSID to be registered</param>
        /// <param name="pUnk">Pointer to the IUnknown interface on the class object whose availability is being published.</param>
        /// <param name="dwClsContext">Context in which the executable code is to be run.</param>
        /// <param name="flags">How connections are made to the class object.</param>
        /// <param name="lpdwRegister">Pointer to a value that identifies the class object registered.</param>
        /// <returns>HRESULT</returns>
        /// <remarks>
        /// PInvoking CoRegisterClassObject to register COM objects is not supported.
        /// </remarks>
        [DllImport("ole32.dll")]
        internal static extern int CoRegisterClassObject(ref Guid rclsid, [MarshalAs(UnmanagedType.Interface)] IClassFactory pUnk, CLSCTX dwClsContext, REGCLS flags, out uint lpdwRegister);

        /// <summary>
        /// Called by a server that can register multiple class objects to inform the SCM about all registered classes, and permits activation requests for those class objects.
        /// </summary>
        /// <returns>HRESULT</returns>
        /// <remarks>
        /// Servers that can register multiple class objects call CoResumeClassObjects once, after having first called CoRegisterClassObject, specifying
        /// REGCLS_LOCAL_SERVER | REGCLS_SUSPENDED for each CLSID the server supports. This function causes OLE to inform the SCM about all the registered classes, and
        /// begins letting activation requests into the server process.
        ///
        /// This reduces the overall registration time, and thus the server application startup time, by making a single call to the SCM, no matter how many CLSIDs are registered for
        /// the server. Another advantage is that if the server has multiple apartments with different CLSIDs registered in different apartments, or is a free-threaded server, no
        /// activation requests will come in until the server calls CoResumeClassObjects. This gives the server a chance to register all of its CLSIDs and get properly set up before having
        /// to deal with activation requests, and possibly shutdown requests.
        /// </remarks>
        [DllImport("ole32.dll")]
        internal static extern int CoResumeClassObjects();

        /// <summary>
        /// Informs OLE that a class object, previously registered with the CoRegisterClassObject function, is no longer available for use.
        /// </summary>
        /// <param name="dwRegister">Token previously returned from the CoRegisterClassObject function</param>
        /// <returns>HRESULT</returns>
        [DllImport("ole32.dll")]
        internal static extern int CoRevokeClassObject(uint dwRegister);

        /// <summary>
        /// Get current thread ID.
        /// </summary>
        /// <returns>Thread id.</returns>
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();

        /// <summary>
        /// The PostThreadMessage function posts a message to the message queue of the specified thread. It returns without waiting for the thread to process the message.
        /// </summary>
        /// <param name="idThread">Identifier of the thread to which the message is to be posted.</param>
        /// <param name="Msg">Specifies the type of message to be posted.</param>
        /// <param name="wParam">Specifies additional message-specific information (wParam).</param>
        /// <param name="lParam">Specifies additional message-specific information (lParam).</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("user32.dll")]
        internal static extern bool PostThreadMessage(uint idThread, uint Msg, UIntPtr wParam, IntPtr lParam);
    }
}
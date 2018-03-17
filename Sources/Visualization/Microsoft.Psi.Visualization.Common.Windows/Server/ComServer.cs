// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Server
{
    using System;
    using System.Threading;

    /// <summary>
    /// ComServer encapsulates the skeleton of an out-of-process COM server in C#. The class implements the singleton design pattern and it's thread-safe.
    /// To start the server, call CoMServer.Instance.Run(). If the server is running, the function returns directly. Inside the Run method, it registers
    /// the class factories for the COM classes to be exposed from the COM server, and starts the message loop to wait for the drop of lock count to zero.
    /// When lock count equals zero, it revokes the registrations and quits the server.
    ///
    /// The lock count of the server is incremented when a COM object is created, and it's decremented when the object is released (GC-ed).
    /// </summary>
    public sealed class ComServer
    {
        /// <summary>
        /// The singleton instance of the <see cref="ComServer"/>.
        /// </summary>
        public static readonly ComServer Instance = new ComServer();

        private uint mainThreadId = 0;
        private int lockCount = 0;
        private uint cookieServer;

        /// <summary>
        /// Initialize is responsible for registering the COM class factories for the COM classes to be exposed from the server,
        /// and initializing the key member variables of the COM server (e.g. mainThreadId and lockCount).
        /// </summary>
        /// <param name="clsid">The server CLSID.</param>
        /// <param name="factory">The server class factory.</param>
        public void Initialize(Guid clsid, IClassFactory factory)
        {
            // Register the Server class object
            int result = NativeMethods.CoRegisterClassObject(ref clsid, factory, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.MULTIPLEUSE | REGCLS.SUSPENDED, out this.cookieServer);
            if (result != 0)
            {
                throw new ApplicationException("CoRegisterClassObject failed w/err 0x" + result.ToString("X"));
            }

            // Inform the SCM about all the registered classes, and begins letting activation requests into the server process.
            result = NativeMethods.CoResumeClassObjects();
            if (result != 0)
            {
                // Revoke the registration of Server on failure
                if (this.cookieServer != 0)
                {
                    NativeMethods.CoRevokeClassObject(this.cookieServer);
                }

                throw new ApplicationException("CoResumeClassObjects failed w/err 0x" + result.ToString("X"));
            }

            // Records the ID of the thread that runs the COM server so that the server knows where to post the WM_QUIT message to exit the message loop.
            this.mainThreadId = NativeMethods.GetCurrentThreadId();

            // Records the count of the active COM objects in the server. When LockCount drops to zero, the server can be shut down.
            this.lockCount = 0;
        }

        /// <summary>
        /// Uninitialize is called to revoke the registration of the COM classes exposed from the server, and perform the cleanups.
        /// </summary>
        public void Uninitialize()
        {
            // Revoke the registration of Server
            if (this.cookieServer != 0)
            {
                NativeMethods.CoRevokeClassObject(this.cookieServer);
            }
        }

        /// <summary>
        /// Increase the lock count
        /// </summary>
        /// <returns>The new lock count after the increment</returns>
        /// <remarks>The method is thread-safe.</remarks>
        public int Lock()
        {
            return Interlocked.Increment(ref this.lockCount);
        }

        /// <summary>
        /// Decrease the lock count. When the lock count drops to zero, post the WM_QUIT message to the message loop in the main thread to shut down the COM server.
        /// </summary>
        /// <returns>The new lock count after the increment</returns>
        public int Unlock()
        {
            int count = Interlocked.Decrement(ref this.lockCount);

            // If lock drops to zero, attempt to terminate the server.
            if (count == 0)
            {
                // Post the WM_QUIT message to the main thread
                NativeMethods.PostThreadMessage(this.mainThreadId, NativeMethods.WM_QUIT, UIntPtr.Zero, IntPtr.Zero);
            }

            return count;
        }
    }
}

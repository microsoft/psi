// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1310 // Field names must not contain underscore

namespace Microsoft.Psi.Visualization.Server
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Values from the CLSCTX enumeration are used in activation calls to indicate the execution contexts in which an object is to be run.
    /// These values are also used in calls to CoRegisterClassObject to indicate the set of execution contexts in which a class object is to
    /// be made available for requests to construct instances.
    /// </summary>
    [Flags]
    public enum CLSCTX : uint
    {
        /// <summary>
        /// The code that creates and manages objects of this class is a DLL that runs in the same process as the caller of the function specifying the class context.
        /// </summary>
        CLSCTX_INPROC_SERVER = 0x1,

        /// <summary>
        /// The code that manages objects of this class is an in-process handler. This is a DLL that runs in the client process and implements client-side structures of this
        /// class when instances of the class are accessed remotely.
        /// </summary>
        CLSCTX_INPROC_HANDLER = 0x2,

        /// <summary>
        /// The EXE code that creates and manages objects of this class runs on same machine but is loaded in a separate process space.
        /// </summary>
        CLSCTX_LOCAL_SERVER = 0x4,

        /// <summary>
        /// Obsolete.
        /// </summary>
        CLSCTX_INPROC_SERVER16 = 0x8,

        /// <summary>
        /// A remote context. The LocalServer32 or LocalService code that creates and manages objects of this class is run on a different computer.
        /// </summary>
        CLSCTX_REMOTE_SERVER = 0x10,

        /// <summary>
        /// Obsolete.
        /// </summary>
        CLSCTX_INPROC_HANDLER16 = 0x20,

        /// <summary>
        /// Reserved.
        /// </summary>
        CLSCTX_RESERVED1 = 0x40,

        /// <summary>
        /// Reserved.
        /// </summary>
        CLSCTX_RESERVED2 = 0x80,

        /// <summary>
        /// Reserved.
        /// </summary>
        CLSCTX_RESERVED3 = 0x100,

        /// <summary>
        /// Reserved.
        /// </summary>
        CLSCTX_RESERVED4 = 0x200,

        /// <summary>
        /// Disaables the downloading of code from the directory service or the Internet. This flag cannot be set at the same time as CLSCTX_ENABLE_CODE_DOWNLOAD.
        /// </summary>
        CLSCTX_NO_CODE_DOWNLOAD = 0x400,

        /// <summary>
        /// Reserved.
        /// </summary>
        CLSCTX_RESERVED5 = 0x800,

        /// <summary>
        /// Specify if you want the activation to fail if it uses custom marshalling.
        /// </summary>
        CLSCTX_NO_CUSTOM_MARSHAL = 0x1000,

        /// <summary>
        /// Enables the downloading of code from the directory service or the Internet. This flag cannot be set at the same time as CLSCTX_NO_CODE_DOWNLOAD.
        /// </summary>
        CLSCTX_ENABLE_CODE_DOWNLOAD = 0x2000,

        /// <summary>
        /// The CLSCTX_NO_FAILURE_LOG can be used to override the logging of failures in CoCreateInstanceEx.
        ///
        /// If the ActivationFailureLoggingLevel is created, the following values can determine the status of event logging:
        ///     0 = Discretionary logging. Log by default, but clients can override by specifying CLSCTX_NO_FAILURE_LOG in CoCreateInstanceEx.
        ///     1 = Always log all failures no matter what the client specified.
        ///     2 = Never log any failures no matter what client specified. If the registry entry is missing, the default is 0. If you need to control customer applications,
        ///         it is recommended that you set this value to 0 and write the client code to override failures. It is strongly recommended that you do not set the value to 2.
        ///         If event logging is disabled, it is more difficult to diagnose problems.
        /// </summary>
        CLSCTX_NO_FAILURE_LOG = 0x4000,

        /// <summary>
        /// Disables activate-as-activator (AAA) activations for this activation only. This flag overrides the setting of the EOAC_DISABLE_AAA flag from the
        /// EOLE_AUTHENTICATION_CAPABILITIES enumeration. This flag cannot be set at the same time as CLSCTX_ENABLE_AAA. Any activation where a server process would be launched
        /// under the caller's identity is known as an activate-as-activator (AAA) activation. Disabling AAA activations allows an application that runs under a privileged
        /// account (such as LocalSystem) to help prevent its identity from being used to launch untrusted components. Library applications that use activation calls should
        /// always set this flag during those calls. This helps prevent the library application from being used in an escalation-of-privilege security attack. This is the only
        /// way to disable AAA activations in a library application because the EOAC_DISABLE_AAA flag from the EOLE_AUTHENTICATION_CAPABILITIES enumeration is applied only to
        /// the server process and not to the library application.
        /// </summary>
        CLSCTX_DISABLE_AAA = 0x8000,

        /// <summary>
        /// Enables activate-as-activator (AAA) activations for this activation only. This flag overrides the setting of the EOAC_DISABLE_AAA flag from the
        /// EOLE_AUTHENTICATION_CAPABILITIES enumeration. This flag cannot be set at the same time as CLSCTX_DISABLE_AAA. Any activation where a server process would be launched
        /// under the caller's identity is known as an activate-as-activator (AAA) activation. Enabling this flag allows an application to transfer its identity to an activated
        /// component.
        /// </summary>
        CLSCTX_ENABLE_AAA = 0x10000,

        /// <summary>
        /// Begin this activation from the default context of the current apartment.
        /// </summary>
        CLSCTX_FROM_DEFAULT_CONTEXT = 0x20000,

        /// <summary>
        /// Activate or connect to a 32-bit version of the server; fail if one is not registered.
        /// </summary>
        CLSCTX_ACTIVATE_32_BIT_SERVER = 0x40000,

        /// <summary>
        /// Activate or connect to a 64 bit version of the server; fail if one is not registered.
        /// </summary>
        CLSCTX_ACTIVATE_64_BIT_SERVER = 0x80000,

        /// <summary>
        /// CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER
        /// </summary>
        CLSCTX_INPROC = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER,

        /// <summary>
        /// CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER
        /// </summary>
        CLSCTX_SERVER = CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER | CLSCTX_REMOTE_SERVER,

        /// <summary>
        /// CLSCTX_SERVER | CLSCTX_INPROC_HANDLER
        /// </summary>
        CLSCTX_ALL = CLSCTX_SERVER | CLSCTX_INPROC_HANDLER
    }

    /// <summary>
    /// The REGCLS enumeration defines values used in CoRegisterClassObject to control the type of connections to a class object.
    /// </summary>
    [Flags]
    public enum REGCLS : uint
    {
        /// <summary>
        /// After an application is connected to a class object with CoGetClassObject, the class object is removed from public view so that no other applications can connect to
        /// it. This value is commonly used for single document interface (SDI) applications. Specifying this value does not affect the responsibility of the object application
        /// to call CoRevokeClassObject; it must always call CoRevokeClassObject when it is finished with an object class.
        /// </summary>
        SINGLEUSE = 0,

        /// <summary>
        /// Multiple applications can connect to the class object through calls to CoGetClassObject. If both the REGCLS_MULTIPLEUSE and CLSCTX_LOCAL_SERVER are set in a call to
        /// CoRegisterClassObject, the class object is also automatically registered as an in-process server, whether CLSCTX_INPROC_SERVER is explicitly set.
        /// </summary>
        MULTIPLEUSE = 1,

        /// <summary>
        /// Useful for registering separate CLSCTX_LOCAL_SERVER and CLSCTX_INPROC_SERVER class factories through calls to CoGetClassObject. If REGCLS_MULTI_SEPARATE is set, each
        /// execution context must be set separately; CoRegisterClassObject does not automatically register an out-of-process server (for which CLSCTX_LOCAL_SERVER is set) as an
        /// in-process server. This allows the EXE to create multiple instances of the object for in-process needs, such as self embeddings, without disturbing its
        /// CLSCTX_LOCAL_SERVER registration. If an EXE registers a REGCLS_MULTI_SEPARATE class factory and a CLSCTX_INPROC_SERVER class factory, instance creation calls that
        /// specify CLSCTX_INPROC_SERVER in the CLSCTX parameter executed by the EXE would be satisfied locally without approaching the SCM. This mechanism is useful when the
        /// EXE uses functions such as OleCreate and OleLoad to create embeddings, but at the same does not wish to launch a new instance of itself for the self-embedding case.
        /// The distinction is important for embeddings because the default handler aggregates the proxy manager by default and the application should override this default
        /// behavior by calling OleCreateEmbeddingHelper for the self-embedding case.
        ///
        /// If your application need not distinguish between the local and inproc case, you need not register your class factory using REGCLS_MULTI_SEPARATE. In fact, the
        /// application incurs an extra network round trip to the SCM when it registers its MULTIPLEUSE class factory as MULTI_SEPARATE and does not register another class
        /// factory as INPROC_SERVER.
        /// </summary>
        MULTI_SEPARATE = 2,

        /// <summary>
        /// Suspends registration and activation requests for the specified CLSID until there is a call to CoResumeClassObjects. This is used typically to register the CLSIDs
        /// for servers that can register multiple class objects to reduce the overall registration time, and thus the server application startup time, by making a single call
        /// to the SCM, no matter how many CLSIDs are registered for the server.
        ///
        /// Note: This flag prevents COM activation errors from a possible race condition between an application shutting down and that application attempting to register a COM
        /// class.
        /// </summary>
        SUSPENDED = 4,

        /// <summary>
        /// The class object is a surrogate process used to run DLL servers. The class factory registered by the surrogate process is not the actual class factory implemented by
        /// the DLL server, but a generic class factory implemented by the surrogate. This generic class factory delegates instance creation and marshaling to the class factory
        /// of the DLL server running in the surrogate. For further information on DLL surrogates, see the DllSurrogate registry value.
        /// </summary>
        SURROGATE = 8,
    }

    /// <summary>
    /// You must implement this interface for every class that you register in the system registry and to which you assign a CLSID, so objects of that class can be created.
    /// http://msdn.microsoft.com/en-us/library/ms694364.aspx
    /// </summary>
    [ComImport]
    [ComVisible(false)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid(ComNative.IClassFactoryIIDString)]
    public interface IClassFactory
    {
        /// <summary>
        /// Creates an uninitialized object.
        /// </summary>
        /// <param name="pUnkOuter">Outer unkown.</param>
        /// <param name="riid">
        /// Reference to the identifier of the interface to be used to communicate with the newly created object. If pUnkOuter is NULL, this parameter is frequently the IID of the initializing interface.
        /// </param>
        /// <param name="ppvObject">
        /// Address of pointer variable that receives the interface pointer requested in riid.
        /// </param>
        /// <returns>S_OK means success.</returns>
        [PreserveSig]
        int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

        /// <summary>
        /// Locks object application open in memory.
        /// </summary>
        /// <param name="fLock">If TRUE, increments the lock count; if FALSE, decrements the lock count.</param>
        /// <returns>S_OK means success.</returns>
        [PreserveSig]
        int LockServer(bool fLock);
    }

    /// <summary>
    /// Constants and p/Invoke methods to interoperate with COM.
    /// </summary>
    public static class ComNative
    {
        /// <summary>
        /// Interface Id of IClassFactory
        /// </summary>
        public const string IClassFactoryIIDString = "00000001-0000-0000-C000-000000000046";

        /// <summary>
        /// Interface Id of IUnknown
        /// </summary>
        public const string IUnknownIIDString = "00000000-0000-0000-C000-000000000046";

        /// <summary>
        /// Interface Id of IDispatch
        /// </summary>
        public const string IDispatchIIDString = "00020400-0000-0000-C000-000000000046";

        /// <summary>
        /// Class does not support aggregation (or class object is remote)
        /// </summary>
        public const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);

        /// <summary>
        /// No such interface supported
        /// </summary>
        public const int E_NOINTERFACE = unchecked((int)0x80004002);

        /// <summary>
        /// Unspecified failure
        /// </summary>
        public const int E_FAIL = unchecked((int)0x80004005);

        /// <summary>
        /// Invalid pointer.
        /// </summary>
        public const int E_POINTER = unchecked((int)0x80000005);
    }
}
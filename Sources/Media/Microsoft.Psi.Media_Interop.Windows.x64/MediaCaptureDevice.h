// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"

#include "VideoFormats.h"
#include "CaptureFormat.h"
#include "VideoProperty.h"
#include "ManagedCameraControlProperty.h"
#include "SourceReaderCallback.h"
#include "MediaFoundationUtility.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    /// Class used to represent an RGB camera capture device available from Media Foundation.
    /// <example>
    /// To use the class,
    /// @code
    ///   // initialize and connect to camera
    ///   MediaCaptureDevice.Initialize()  // starts Media Foundation
    ///   foreach (var device in MediaCaptureDevice.AllDevices)   // cycle through all the available devices
    ///            {
    ///                if (device.FriendlyName.Equals("mydevicename"))   // check the device's friendly name
    ///                if (!device.Attach())   return;                   // attach to device, return if unable to 
    ///            }
    ///
    ///    // grab image, which calls OnImageAvailable	
    ///	  ReadSampleDelegate^ myCallback;
    ///    myCallback = gcnew ReadSampleDelegate(OnImageAvailable);
    ///    rgbCamera->CaptureSample(myCallback);
    ///
    ///	  // where the callback is given by
    ///	  void OnImageAvailable(System::IntPtr buffer, int cbLength) {}
    /// @endcode
    /// </example>
    /// </summary>
    public ref class MediaCaptureDevice
    {
    private:
		/// <summary>
		/// Is capture device being used in a shared mode?
		/// </summary>
		bool m_useSharedMode;

        /// <summary>
        /// How many milliseconds in a second
        /// </summary>
        static const int MillisecondsPerSecond = 1000;
        
        /// <summary>
        /// Name of the device
        /// </summary>
        String^ m_name;

        /// <summary>
        /// Unique identifier for this device
        /// </summary>
        String^ m_symbolicLink;

        /// <summary>
        /// The underlying MF Source Reader
        /// </summary>
        IMFSourceReader *m_pSourceReader;

        /// <summary>
        /// The underlying MF Source
        /// </summary>
        IMFMediaSource *_pMediaSource;

        /// <summary>
        /// The underlying MF Activate
        /// </summary>
        IMFActivate *m_pActivate;

        /// <summary>
        /// The source reader callback which wraps the MF Source Reader
        /// </summary>
        SourceReaderCallback* m_callback;

        /// <summary>
        /// The time the last frame arrived
        /// </summary>
        LONGLONG m_lastFrameTime;

        /// <summary>
        /// Frequency of the performance counter
        /// </summary>
        __int64 m_performanceCounterFrequency;

        /// <summary>
        /// The requested output frame rate numerator
        /// </summary>
        int m_desiredRateNumerator;

        /// <summary>
        /// The requested output frame rate denominator
        /// </summary>
        int m_desiredRateDenominator;

    internal:
        MediaCaptureDevice(IMFActivate *pActivate);
        void InitializeFromActivate(IMFActivate *pActivate, String^ name);
        void InitilaizePerformanceCounterFrequency();
        IMFActivate *GetActivate(String^ symbolicLink, bool useInSharedMode);

        /// <summary>
        /// Handler for read sample completion
        /// </summary>
        ReadSampleDelegate^ m_readSampleCallback;

        /// <summary>
        /// GC Handle for read sample completion
        /// </summary>
        GCHandle^ m_readSampleHandlerHandle;

        /// <summary>
        /// Internal pinned managed Handler for read sample completion
        /// </summary>
        ReadSampleDelegate^ m_readSampleDelegateInternal;

        void ReadSampleThunk(IntPtr data, int cbLength, LONGLONG timestamp);

    public:
        MediaCaptureDevice(String^ name, String^ symbolicLink, bool useInSharedMode);

        ~MediaCaptureDevice();

        /// <summary>
        ///  This is used to initialize the MediaFoundation library and 
        ///  should be called once before any use of the a MediaFoundation
        ///  capability.
        /// </summary>        
        static void Initialize()
        {
            HRESULT hr = S_OK;

            hr = MFStartup(MF_VERSION, MFSTARTUP_LITE);
            MF_THROWHR(hr);
        }


        /// <summary>
        ///  This is used to uninitialize the MediaFoundation library and 
        ///  should be called once at application shutdown.
        /// </summary>        
        static void Uninitialize()
        {
            HRESULT hr = S_OK;

            hr = MFShutdown();
            MF_THROWHR(hr);
        }

        
        bool Attach(bool useInSharedMode);
        void Shutdown();
        void CaptureSample(ReadSampleDelegate^ handler);
        bool SetProperty(VideoProperty prop, int nValue, VideoPropertyFlags flags);
        bool GetProperty(VideoProperty prop, int% nValue, int% flags);
        bool SetProperty(ManagedCameraControlProperty prop, int nValue, ManagedCameraControlPropertyFlags flags);
        bool GetProperty(ManagedCameraControlProperty prop, int% nValue, int% flags);
		bool GetRange(VideoProperty prop, long %min, long %max, long %stepSize, long %defaultValue, int %flag);
		bool GetRange(ManagedCameraControlProperty prop, long %min, long %max, long %stepSize, long %defaultValue, int %flag);

        /// <summary>
        ///  Gets a list of all available capture devices
        /// </summary>
        static property IEnumerable<MediaCaptureDevice^>^ AllDevices
#ifdef DOXYGEN
	  ;
#else
        {
            IEnumerable<MediaCaptureDevice^>^ get();
        }
#endif

        /// <summary>
        ///  Gets the friendly name of this device
        /// </summary>        
        property String^ FriendlyName
#ifdef DOXYGEN
	  ;
#else
        {
            String^ get()
            {
                return m_name;
            }
        }
#endif

        /// <summary>
        ///  Gets the unique identifier for this device.
        /// </summary>        
        property String^ SymbolicLink
#ifdef DOXYGEN
	  ;
#else
        {
            String^ get()
            {
                return m_symbolicLink;
            }
        }
#endif

        /// <summary>
        ///  Gets a boolean indicating if the device is currently attached.
        /// </summary>        
        property bool fAttached
#ifdef DOXYGEN
	  ;
#else
        {
            bool get()
            {
                return m_pSourceReader != NULL;
            }
        }
#endif

        /// <summary>
        ///  Gets the list of capture formats supported
        /// </summary> 
        /// <returns>List of supported capture formats</returns>
        property IEnumerable<CaptureFormat^>^ Formats
#ifdef DOXYGEN
	  ;
#else
        {
            IEnumerable<CaptureFormat^>^ get();
        }
#endif

        /// <summary>
        ///  Gets the list of video property values supported
        /// </summary>        
        /// <returns>List of supported video property values</returns>
        property IEnumerable<VideoPropertyValue^>^ VideoProperties
#ifdef DOXYGEN
	  ;
#else
        {
            IEnumerable<VideoPropertyValue^>^ get();
        }
#endif

        /// <summary>
        ///  Gets the list of camera control property values supported
        /// </summary>        
        /// <returns>List of supported camera control property values</returns>
        property IEnumerable<ManagedCameraControlPropertyValue^>^ ManagedCameraControlProperties
#ifdef DOXYGEN
	  ;
#else
        {
            IEnumerable<ManagedCameraControlPropertyValue^>^ get();
        }
#endif

        /// <summary>
        ///  Gets or sets the capture format currently used by the device.
        /// </summary>
        /// <returns>Current capture format</returns>
        property CaptureFormat^ CurrentFormat
#ifdef DOXYGEN
	  ;
#else
        {
            CaptureFormat^ get();
            void set(CaptureFormat^value);
        }
#endif

    };

}}}

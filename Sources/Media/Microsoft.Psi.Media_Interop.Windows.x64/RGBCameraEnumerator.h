// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"

#include "MediaFoundationUtility.h"

#include "MediaCaptureDevice.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    /// RGB Camera Enumerator which enumerates through all the camera devices.
    /// </summary>
    ref class RGBCameraEnumerator : IEnumerator<MediaCaptureDevice^>
    {
    private:

        /// <summary>
        /// The list of camera devices.
        /// </summary>
        IMFActivate **_ppDevices;

        /// <summary>
        /// Count of camera devices.
        /// </summary>
        int m_nCount;

        /// <summary>
        /// Index of the current camera device.
        /// </summary>
        int m_nIndex;

    public:

        RGBCameraEnumerator();
        ~RGBCameraEnumerator();
        virtual bool MoveNext();
        virtual void Reset();

        /// <summary>
        /// Current camera for the old IEnumerator before generics.
        /// </summary>
        virtual property Object^ OldCurrent
#ifdef DOXYGEN
	  ;
#else
        {
            Object^ get() = System::Collections::IEnumerator::Current::get
            {
                return Current::get();
            }
        }
#endif

        /// <summary>
        /// Current camera for IEnumerator&lt;T&gt;.
        /// </summary>
        virtual property MediaCaptureDevice^ Current
#ifdef DOXYGEN
	  ;
#else
        {
            MediaCaptureDevice^ get();
        }
#endif
    };
}}}

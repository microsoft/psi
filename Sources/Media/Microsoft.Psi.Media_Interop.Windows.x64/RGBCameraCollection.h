// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"

#include "MediaFoundationUtility.h"
#include "MediaCaptureDevice.h"
#include "RGBCameraEnumerator.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    /// RGB Camera collection.
    /// </summary>
    public ref class RGBCameraCollection : IEnumerable<MediaCaptureDevice^>
    {
    public:

        /// <summary>
        /// GetEnumerator for the old IEnumerator before generics.
        /// </summary>
        virtual System::Collections::IEnumerator^ OldGetEnumerator()
#ifdef DOXYGEN
	  ;
#else
	  = System::Collections::IEnumerable::GetEnumerator
        {
            return GetEnumerator();
        }
#endif

        /// <summary>
        /// GetEnumerator for IEnumerator&lt;T&gt;.
        /// </summary>
        virtual IEnumerator<MediaCaptureDevice^>^ GetEnumerator()
        {
            return gcnew RGBCameraEnumerator();
        }
    };
}}}

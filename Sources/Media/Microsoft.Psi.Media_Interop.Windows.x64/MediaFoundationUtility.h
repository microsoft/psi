// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"
#include "Resources.h"

namespace res = System::Resources;

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    /// Utility class which converts HRESULTS to exceptions.
    /// </summary>
    ref class MediaFoundationUtility
    {
    public:
        /// <summary>
        /// Throws exception corresponding to the hr value
        /// </summary>
        static void ThrowExceptionForHR(HRESULT hr)
        {
            switch (hr)
            {
            case MF_E_INVALIDMEDIATYPE:
                throw gcnew InvalidOperationException(Resources::InvalidMediaType);

            case MF_E_ATTRIBUTENOTFOUND:
                throw gcnew KeyNotFoundException(Resources::AttributeNotFound);

            case MF_E_HW_MFT_FAILED_START_STREAMING:
                throw gcnew InvalidOperationException(Resources::HwMftFailedStartStreaming);

            default:
                Marshal::ThrowExceptionForHR(hr);
            }
        }

        static String^ GetStringProperty(IMFAttributes* pAttributes, REFGUID key);
    };       
}}}

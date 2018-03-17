// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
#include "StdAfx.h"
using namespace System::Runtime::InteropServices;

#include "RGBCameraEnumerator.h"
#include "SourceReaderCallback.h"
#include "MediaCaptureDevice.h"

#include "ks.h"
#include "ksmedia.h"
#include "MediaFoundationUtility.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    ///  This looks up a specified string attribute on an attribute object
    /// </summary>
    /// <param name="pAttributes">The attribute store to read from</param>
    /// <param name="key">The attribute key to look up</param>
    /// <returns>The string representation of the attribute. </returns>
    /// @throws KeyNotFoundException - no attribute was found
    String^ MediaFoundationUtility::GetStringProperty(IMFAttributes* pAttributes, REFGUID key)
    {
        HRESULT hr = S_OK;
        PROPVARIANT var = {0};

        try
        {
            hr = pAttributes->GetItem(key, &var);
            MF_THROWHR(hr);

            if (var.vt != VT_LPWSTR)
            {
                return nullptr;
            }
            return gcnew String(var.pwszVal);
        }
        finally
        {
            PropVariantClear(&var);
        }
    }
}}}

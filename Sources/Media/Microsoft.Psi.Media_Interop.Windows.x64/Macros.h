// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#define MF_EXIT() __leave

#define MF_CHKPTR(hr, ptr) if (ptr == NULL) {hr = E_POINTER; MF_EXIT();}
#define MF_THROWPTR(ptr) if (ptr == NULL) throw gcnew NullReferenceException()

#define MF_CHKHR(hr) if (FAILED(hr)) MF_EXIT()
#define MF_THROWHR(hr) if (FAILED(hr)) MediaFoundationUtility::ThrowExceptionForHR(hr)

#define MF_RELEASE(ptr) if (ptr != NULL) {ptr->Release(); ptr = NULL;}

#define FromGUID(g) Guid(g.Data1, g.Data2, g.Data3, g.Data4[0], g.Data4[1], g.Data4[2], g.Data4[3], g.Data4[4], g.Data4[5], g.Data4[6], g.Data4[7])

#define SAFE_DELETE_ARRAY(pArray) if (pArray != NULL) {delete [] pArray; pArray = NULL; }
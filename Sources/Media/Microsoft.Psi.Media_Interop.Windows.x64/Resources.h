// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"

namespace res = System::Resources;

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    /// Resources for camera interop
    /// </summary>
    ref class Resources
    {
    public:
     
        /// <summary>
        /// Gets the InvalidMediaType resource
        /// </summary>
        static property String^ InvalidMediaType
#ifdef DOXYGEN
	  ;
#else
        {
            String^ get()
            {
                return "InvalidMediaType";
            }
        }
#endif

        /// <summary>
        /// Gets the AttributeNotFound resource
        /// </summary>
        static property String^ AttributeNotFound
#ifdef DOXYGEN
	  ;
#else
        {
            String^ get()
            {
                return "AttributeNotFound";
            }
        }
#endif

        /// <summary>
        /// Gets the HwMftFailedStartStreaming resource
        /// </summary>        
        static property String^ HwMftFailedStartStreaming
#ifdef DOXYGEN
	  ;
#else
        {
            String^ get()
            {
                return "HwMftFailedStartStreaming";
            }
        }
#endif
    };
}}}

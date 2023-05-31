// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "Managed.h"

#include "ks.h"
#include "ksmedia.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

    /// <summary>
    /// Managed Camera Control Properties enumeration
    /// </summary>
    public enum class ManagedCameraControlProperty
    {
        /// <summary>
        /// Pan RW O
        /// </summary>       
        Pan = KSPROPERTY_CAMERACONTROL_PAN,             

        /// <summary>
        /// Tilt RW O
        /// </summary>  
        Tilt = KSPROPERTY_CAMERACONTROL_TILT,                      

        /// <summary>
        /// Roll RW O
        /// </summary>
        Roll = KSPROPERTY_CAMERACONTROL_ROLL,                    

        /// <summary>
        /// Zoom RW O
        /// </summary>
        Zoom = KSPROPERTY_CAMERACONTROL_ZOOM,                      

        /// <summary>
        /// Exposure RW O
        /// </summary>
        Exposure = KSPROPERTY_CAMERACONTROL_EXPOSURE, 

        /// <summary>
        /// Iris RW O
        /// </summary>
        Iris = KSPROPERTY_CAMERACONTROL_IRIS,
        
        /// <summary>
        /// Focus RW O
        /// </summary>
        Focus = KSPROPERTY_CAMERACONTROL_FOCUS, 
                
        /// <summary>
        /// ScanMode RW O
        /// </summary>
        ScanMode = KSPROPERTY_CAMERACONTROL_SCANMODE,
        
        /// <summary>
        /// Privacy RW O
        /// </summary>
        Privacy =  KSPROPERTY_CAMERACONTROL_PRIVACY,                  
        
        /// <summary>
        /// PanTilt RW O
        /// </summary>
        PanTilt = KSPROPERTY_CAMERACONTROL_PANTILT,                  
        
        /// <summary>
        /// PanRelative RW O
        /// </summary>
        PanRelative = KSPROPERTY_CAMERACONTROL_PAN_RELATIVE,             
        
        /// <summary>
        /// TiltRelative RW O
        /// </summary>
        TiltRelative = KSPROPERTY_CAMERACONTROL_TILT_RELATIVE,            
        
        /// <summary>
        /// RollRelative RW O
        /// </summary>
        RollRelative = KSPROPERTY_CAMERACONTROL_ROLL_RELATIVE,            
        
        /// <summary>
        /// ZoomRelative RW O
        /// </summary>
        ZoomRelative = KSPROPERTY_CAMERACONTROL_ZOOM_RELATIVE,            
        
        /// <summary>
        /// ExposureRelative RW O
        /// </summary>
        ExposureRelative = KSPROPERTY_CAMERACONTROL_EXPOSURE_RELATIVE,        
        
        /// <summary>
        /// IrisRelative RW O
        /// </summary>
        IrisRelative = KSPROPERTY_CAMERACONTROL_IRIS_RELATIVE,            
        
        /// <summary>
        /// FocusRelative RW O
        /// </summary>
        FocusRelative = KSPROPERTY_CAMERACONTROL_FOCUS_RELATIVE,           
        
        /// <summary>
        /// PanTiltRelative RW O
        /// </summary>
        PanTiltRelative = KSPROPERTY_CAMERACONTROL_PANTILT_RELATIVE,         
        
        /// <summary>
        /// FocalLength R O
        /// </summary>
        FocalLength = KSPROPERTY_CAMERACONTROL_FOCAL_LENGTH,                
        
        /// <summary>
        /// AutoExposurePriority RW O
        /// </summary>
        AutoExposurePriority = KSPROPERTY_CAMERACONTROL_AUTO_EXPOSURE_PRIORITY  
    };

    /// <summary>
    /// Camera Control Property flags enumeration
    /// </summary>
    public enum class ManagedCameraControlPropertyFlags
    {
        /// <summary>
        /// Auto settings
        /// </summary>
        Auto = KSPROPERTY_CAMERACONTROL_FLAGS_AUTO,

        /// <summary>
        /// Manual settings
        /// </summary>
        Manual = KSPROPERTY_CAMERACONTROL_FLAGS_MANUAL
    };

    /// <summary>
    /// Camera Control Property Value Structure.
    /// </summary>
    public ref struct ManagedCameraControlPropertyValue
    {
    public:
        /// <summary>
        /// Camera Control Property.
        /// </summary>
        property ManagedCameraControlProperty m_Property;

        /// <summary>
        /// Value of the camera control property
        /// </summary>
        property int nValue;

        /// <summary>
        /// Minimum of the camera control property
        /// </summary>
        property int nMinimum;

        /// <summary>
        /// Maximum of the camera control property
        /// </summary>
        property int nMaximum;

        /// <summary>
        /// Steppping delta of the camera control property
        /// </summary>
        property int nSteppingDelta;

        /// <summary>
        /// Default value of the camera control property
        /// </summary>
        property int nDefault;

        /// <summary>
        /// Camera control property flags
        /// </summary>
        property ManagedCameraControlPropertyFlags m_Flags;
    };
}}}

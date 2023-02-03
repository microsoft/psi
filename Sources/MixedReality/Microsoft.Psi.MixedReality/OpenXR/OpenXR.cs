// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1602 // Enumeration items should be documented
#pragma warning disable SA1310 // Field names should not contain underscore
namespace Microsoft.Psi.MixedReality.OpenXR
{
    using System;
    using System.Runtime.InteropServices;
    using global::StereoKit;

    /// <summary>
    /// Describes which hand the tracker is tracking.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrHandEXT.html.
    /// </remarks>
    internal enum XrHandEXT
    {
        XR_HAND_LEFT_EXT = 1,
        XR_HAND_RIGHT_EXT = 2,
        XR_HAND_MAX_ENUM_EXT = 0x7FFFFFFF,
    }

    /// <summary>
    /// The set of hand joints to track.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrHandJointSetEXT.html.
    /// </remarks>
    internal enum XrHandJointSetEXT
    {
        XR_HAND_JOINT_SET_DEFAULT_EXT = 0,
        XR_HAND_JOINT_SET_MAX_ENUM_EXT = 0x7FFFFFFF,
    }

    /// <summary>
    /// Result codes.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrResult.html.
    /// </remarks>
    internal enum XrResult
    {
        XR_SUCCESS = 0,
        XR_TIMEOUT_EXPIRED = 1,
        XR_SESSION_LOSS_PENDING = 3,
        XR_EVENT_UNAVAILABLE = 4,
        XR_SPACE_BOUNDS_UNAVAILABLE = 7,
        XR_SESSION_NOT_FOCUSED = 8,
        XR_FRAME_DISCARDED = 9,
        XR_ERROR_VALIDATION_FAILURE = -1,
        XR_ERROR_RUNTIME_FAILURE = -2,
        XR_ERROR_OUT_OF_MEMORY = -3,
        XR_ERROR_API_VERSION_UNSUPPORTED = -4,
        XR_ERROR_INITIALIZATION_FAILED = -6,
        XR_ERROR_FUNCTION_UNSUPPORTED = -7,
        XR_ERROR_FEATURE_UNSUPPORTED = -8,
        XR_ERROR_EXTENSION_NOT_PRESENT = -9,
        XR_ERROR_LIMIT_REACHED = -10,
        XR_ERROR_SIZE_INSUFFICIENT = -11,
        XR_ERROR_HANDLE_INVALID = -12,
        XR_ERROR_INSTANCE_LOST = -13,
        XR_ERROR_SESSION_RUNNING = -14,
        XR_ERROR_SESSION_NOT_RUNNING = -16,
        XR_ERROR_SESSION_LOST = -17,
        XR_ERROR_SYSTEM_INVALID = -18,
        XR_ERROR_PATH_INVALID = -19,
        XR_ERROR_PATH_COUNT_EXCEEDED = -20,
        XR_ERROR_PATH_FORMAT_INVALID = -21,
        XR_ERROR_PATH_UNSUPPORTED = -22,
        XR_ERROR_LAYER_INVALID = -23,
        XR_ERROR_LAYER_LIMIT_EXCEEDED = -24,
        XR_ERROR_SWAPCHAIN_RECT_INVALID = -25,
        XR_ERROR_SWAPCHAIN_FORMAT_UNSUPPORTED = -26,
        XR_ERROR_ACTION_TYPE_MISMATCH = -27,
        XR_ERROR_SESSION_NOT_READY = -28,
        XR_ERROR_SESSION_NOT_STOPPING = -29,
        XR_ERROR_TIME_INVALID = -30,
        XR_ERROR_REFERENCE_SPACE_UNSUPPORTED = -31,
        XR_ERROR_FILE_ACCESS_ERROR = -32,
        XR_ERROR_FILE_CONTENTS_INVALID = -33,
        XR_ERROR_FORM_FACTOR_UNSUPPORTED = -34,
        XR_ERROR_FORM_FACTOR_UNAVAILABLE = -35,
        XR_ERROR_API_LAYER_NOT_PRESENT = -36,
        XR_ERROR_CALL_ORDER_INVALID = -37,
        XR_ERROR_GRAPHICS_DEVICE_INVALID = -38,
        XR_ERROR_POSE_INVALID = -39,
        XR_ERROR_INDEX_OUT_OF_RANGE = -40,
        XR_ERROR_VIEW_CONFIGURATION_TYPE_UNSUPPORTED = -41,
        XR_ERROR_ENVIRONMENT_BLEND_MODE_UNSUPPORTED = -42,
        XR_ERROR_NAME_DUPLICATED = -44,
        XR_ERROR_NAME_INVALID = -45,
        XR_ERROR_ACTIONSET_NOT_ATTACHED = -46,
        XR_ERROR_ACTIONSETS_ALREADY_ATTACHED = -47,
        XR_ERROR_LOCALIZED_NAME_DUPLICATED = -48,
        XR_ERROR_LOCALIZED_NAME_INVALID = -49,
        XR_ERROR_GRAPHICS_REQUIREMENTS_CALL_MISSING = -50,
        XR_ERROR_RUNTIME_UNAVAILABLE = -51,
        XR_ERROR_ANDROID_THREAD_SETTINGS_ID_INVALID_KHR = -1000003000,
        XR_ERROR_ANDROID_THREAD_SETTINGS_FAILURE_KHR = -1000003001,
        XR_ERROR_CREATE_SPATIAL_ANCHOR_FAILED_MSFT = -1000039001,
        XR_ERROR_SECONDARY_VIEW_CONFIGURATION_TYPE_NOT_ENABLED_MSFT = -1000053000,
        XR_ERROR_CONTROLLER_MODEL_KEY_INVALID_MSFT = -1000055000,
        XR_ERROR_REPROJECTION_MODE_UNSUPPORTED_MSFT = -1000066000,
        XR_ERROR_COMPUTE_NEW_SCENE_NOT_COMPLETED_MSFT = -1000097000,
        XR_ERROR_SCENE_COMPONENT_ID_INVALID_MSFT = -1000097001,
        XR_ERROR_SCENE_COMPONENT_TYPE_MISMATCH_MSFT = -1000097002,
        XR_ERROR_SCENE_MESH_BUFFER_ID_INVALID_MSFT = -1000097003,
        XR_ERROR_SCENE_COMPUTE_FEATURE_INCOMPATIBLE_MSFT = -1000097004,
        XR_ERROR_SCENE_COMPUTE_CONSISTENCY_MISMATCH_MSFT = -1000097005,
        XR_ERROR_DISPLAY_REFRESH_RATE_UNSUPPORTED_FB = -1000101000,
        XR_ERROR_COLOR_SPACE_UNSUPPORTED_FB = -1000108000,
        XR_ERROR_UNEXPECTED_STATE_PASSTHROUGH_FB = -1000118000,
        XR_ERROR_FEATURE_ALREADY_CREATED_PASSTHROUGH_FB = -1000118001,
        XR_ERROR_FEATURE_REQUIRED_PASSTHROUGH_FB = -1000118002,
        XR_ERROR_NOT_PERMITTED_PASSTHROUGH_FB = -1000118003,
        XR_ERROR_INSUFFICIENT_RESOURCES_PASSTHROUGH_FB = -1000118004,
        XR_ERROR_UNKNOWN_PASSTHROUGH_FB = -1000118050,
        XR_ERROR_RENDER_MODEL_KEY_INVALID_FB = -1000119000,
        XR_RENDER_MODEL_UNAVAILABLE_FB = 1000119020,
        XR_ERROR_MARKER_NOT_TRACKED_VARJO = -1000124000,
        XR_ERROR_MARKER_ID_INVALID_VARJO = -1000124001,
        XR_ERROR_SPATIAL_ANCHOR_NAME_NOT_FOUND_MSFT = -1000142001,
        XR_ERROR_SPATIAL_ANCHOR_NAME_INVALID_MSFT = -1000142002,
        XR_RESULT_MAX_ENUM = 0x7FFFFFFF,
    }

    /// <summary>
    /// Values for type members of structs.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrStructureType.html.
    /// </remarks>
    internal enum XrStructureType
    {
        XR_TYPE_UNKNOWN = 0,
        XR_TYPE_API_LAYER_PROPERTIES = 1,
        XR_TYPE_EXTENSION_PROPERTIES = 2,
        XR_TYPE_INSTANCE_CREATE_INFO = 3,
        XR_TYPE_SYSTEM_GET_INFO = 4,
        XR_TYPE_SYSTEM_PROPERTIES = 5,
        XR_TYPE_VIEW_LOCATE_INFO = 6,
        XR_TYPE_VIEW = 7,
        XR_TYPE_SESSION_CREATE_INFO = 8,
        XR_TYPE_SWAPCHAIN_CREATE_INFO = 9,
        XR_TYPE_SESSION_BEGIN_INFO = 10,
        XR_TYPE_VIEW_STATE = 11,
        XR_TYPE_FRAME_END_INFO = 12,
        XR_TYPE_HAPTIC_VIBRATION = 13,
        XR_TYPE_EVENT_DATA_BUFFER = 16,
        XR_TYPE_EVENT_DATA_INSTANCE_LOSS_PENDING = 17,
        XR_TYPE_EVENT_DATA_SESSION_STATE_CHANGED = 18,
        XR_TYPE_ACTION_STATE_BOOLEAN = 23,
        XR_TYPE_ACTION_STATE_FLOAT = 24,
        XR_TYPE_ACTION_STATE_VECTOR2F = 25,
        XR_TYPE_ACTION_STATE_POSE = 27,
        XR_TYPE_ACTION_SET_CREATE_INFO = 28,
        XR_TYPE_ACTION_CREATE_INFO = 29,
        XR_TYPE_INSTANCE_PROPERTIES = 32,
        XR_TYPE_FRAME_WAIT_INFO = 33,
        XR_TYPE_COMPOSITION_LAYER_PROJECTION = 35,
        XR_TYPE_COMPOSITION_LAYER_QUAD = 36,
        XR_TYPE_REFERENCE_SPACE_CREATE_INFO = 37,
        XR_TYPE_ACTION_SPACE_CREATE_INFO = 38,
        XR_TYPE_EVENT_DATA_REFERENCE_SPACE_CHANGE_PENDING = 40,
        XR_TYPE_VIEW_CONFIGURATION_VIEW = 41,
        XR_TYPE_SPACE_LOCATION = 42,
        XR_TYPE_SPACE_VELOCITY = 43,
        XR_TYPE_FRAME_STATE = 44,
        XR_TYPE_VIEW_CONFIGURATION_PROPERTIES = 45,
        XR_TYPE_FRAME_BEGIN_INFO = 46,
        XR_TYPE_COMPOSITION_LAYER_PROJECTION_VIEW = 48,
        XR_TYPE_EVENT_DATA_EVENTS_LOST = 49,
        XR_TYPE_INTERACTION_PROFILE_SUGGESTED_BINDING = 51,
        XR_TYPE_EVENT_DATA_INTERACTION_PROFILE_CHANGED = 52,
        XR_TYPE_INTERACTION_PROFILE_STATE = 53,
        XR_TYPE_SWAPCHAIN_IMAGE_ACQUIRE_INFO = 55,
        XR_TYPE_SWAPCHAIN_IMAGE_WAIT_INFO = 56,
        XR_TYPE_SWAPCHAIN_IMAGE_RELEASE_INFO = 57,
        XR_TYPE_ACTION_STATE_GET_INFO = 58,
        XR_TYPE_HAPTIC_ACTION_INFO = 59,
        XR_TYPE_SESSION_ACTION_SETS_ATTACH_INFO = 60,
        XR_TYPE_ACTIONS_SYNC_INFO = 61,
        XR_TYPE_BOUND_SOURCES_FOR_ACTION_ENUMERATE_INFO = 62,
        XR_TYPE_INPUT_SOURCE_LOCALIZED_NAME_GET_INFO = 63,
        XR_TYPE_COMPOSITION_LAYER_CUBE_KHR = 1000006000,
        XR_TYPE_INSTANCE_CREATE_INFO_ANDROID_KHR = 1000008000,
        XR_TYPE_COMPOSITION_LAYER_DEPTH_INFO_KHR = 1000010000,
        XR_TYPE_VULKAN_SWAPCHAIN_FORMAT_LIST_CREATE_INFO_KHR = 1000014000,
        XR_TYPE_EVENT_DATA_PERF_SETTINGS_EXT = 1000015000,
        XR_TYPE_COMPOSITION_LAYER_CYLINDER_KHR = 1000017000,
        XR_TYPE_COMPOSITION_LAYER_EQUIRECT_KHR = 1000018000,
        XR_TYPE_DEBUG_UTILS_OBJECT_NAME_INFO_EXT = 1000019000,
        XR_TYPE_DEBUG_UTILS_MESSENGER_CALLBACK_DATA_EXT = 1000019001,
        XR_TYPE_DEBUG_UTILS_MESSENGER_CREATE_INFO_EXT = 1000019002,
        XR_TYPE_DEBUG_UTILS_LABEL_EXT = 1000019003,
        XR_TYPE_GRAPHICS_BINDING_OPENGL_WIN32_KHR = 1000023000,
        XR_TYPE_GRAPHICS_BINDING_OPENGL_XLIB_KHR = 1000023001,
        XR_TYPE_GRAPHICS_BINDING_OPENGL_XCB_KHR = 1000023002,
        XR_TYPE_GRAPHICS_BINDING_OPENGL_WAYLAND_KHR = 1000023003,
        XR_TYPE_SWAPCHAIN_IMAGE_OPENGL_KHR = 1000023004,
        XR_TYPE_GRAPHICS_REQUIREMENTS_OPENGL_KHR = 1000023005,
        XR_TYPE_GRAPHICS_BINDING_OPENGL_ES_ANDROID_KHR = 1000024001,
        XR_TYPE_SWAPCHAIN_IMAGE_OPENGL_ES_KHR = 1000024002,
        XR_TYPE_GRAPHICS_REQUIREMENTS_OPENGL_ES_KHR = 1000024003,
        XR_TYPE_GRAPHICS_BINDING_VULKAN_KHR = 1000025000,
        XR_TYPE_SWAPCHAIN_IMAGE_VULKAN_KHR = 1000025001,
        XR_TYPE_GRAPHICS_REQUIREMENTS_VULKAN_KHR = 1000025002,
        XR_TYPE_GRAPHICS_BINDING_D3D11_KHR = 1000027000,
        XR_TYPE_SWAPCHAIN_IMAGE_D3D11_KHR = 1000027001,
        XR_TYPE_GRAPHICS_REQUIREMENTS_D3D11_KHR = 1000027002,
        XR_TYPE_GRAPHICS_BINDING_D3D12_KHR = 1000028000,
        XR_TYPE_SWAPCHAIN_IMAGE_D3D12_KHR = 1000028001,
        XR_TYPE_GRAPHICS_REQUIREMENTS_D3D12_KHR = 1000028002,
        XR_TYPE_SYSTEM_EYE_GAZE_INTERACTION_PROPERTIES_EXT = 1000030000,
        XR_TYPE_EYE_GAZE_SAMPLE_TIME_EXT = 1000030001,
        XR_TYPE_VISIBILITY_MASK_KHR = 1000031000,
        XR_TYPE_EVENT_DATA_VISIBILITY_MASK_CHANGED_KHR = 1000031001,
        XR_TYPE_SESSION_CREATE_INFO_OVERLAY_EXTX = 1000033000,
        XR_TYPE_EVENT_DATA_MAIN_SESSION_VISIBILITY_CHANGED_EXTX = 1000033003,
        XR_TYPE_COMPOSITION_LAYER_COLOR_SCALE_BIAS_KHR = 1000034000,
        XR_TYPE_SPATIAL_ANCHOR_CREATE_INFO_MSFT = 1000039000,
        XR_TYPE_SPATIAL_ANCHOR_SPACE_CREATE_INFO_MSFT = 1000039001,
        XR_TYPE_COMPOSITION_LAYER_IMAGE_LAYOUT_FB = 1000040000,
        XR_TYPE_COMPOSITION_LAYER_ALPHA_BLEND_FB = 1000041001,
        XR_TYPE_VIEW_CONFIGURATION_DEPTH_RANGE_EXT = 1000046000,
        XR_TYPE_GRAPHICS_BINDING_EGL_MNDX = 1000048004,
        XR_TYPE_SPATIAL_GRAPH_NODE_SPACE_CREATE_INFO_MSFT = 1000049000,
        XR_TYPE_SYSTEM_HAND_TRACKING_PROPERTIES_EXT = 1000051000,
        XR_TYPE_HAND_TRACKER_CREATE_INFO_EXT = 1000051001,
        XR_TYPE_HAND_JOINTS_LOCATE_INFO_EXT = 1000051002,
        XR_TYPE_HAND_JOINT_LOCATIONS_EXT = 1000051003,
        XR_TYPE_HAND_JOINT_VELOCITIES_EXT = 1000051004,
        XR_TYPE_SYSTEM_HAND_TRACKING_MESH_PROPERTIES_MSFT = 1000052000,
        XR_TYPE_HAND_MESH_SPACE_CREATE_INFO_MSFT = 1000052001,
        XR_TYPE_HAND_MESH_UPDATE_INFO_MSFT = 1000052002,
        XR_TYPE_HAND_MESH_MSFT = 1000052003,
        XR_TYPE_HAND_POSE_TYPE_INFO_MSFT = 1000052004,
        XR_TYPE_SECONDARY_VIEW_CONFIGURATION_SESSION_BEGIN_INFO_MSFT = 1000053000,
        XR_TYPE_SECONDARY_VIEW_CONFIGURATION_STATE_MSFT = 1000053001,
        XR_TYPE_SECONDARY_VIEW_CONFIGURATION_FRAME_STATE_MSFT = 1000053002,
        XR_TYPE_SECONDARY_VIEW_CONFIGURATION_FRAME_END_INFO_MSFT = 1000053003,
        XR_TYPE_SECONDARY_VIEW_CONFIGURATION_LAYER_INFO_MSFT = 1000053004,
        XR_TYPE_SECONDARY_VIEW_CONFIGURATION_SWAPCHAIN_CREATE_INFO_MSFT = 1000053005,
        XR_TYPE_CONTROLLER_MODEL_KEY_STATE_MSFT = 1000055000,
        XR_TYPE_CONTROLLER_MODEL_NODE_PROPERTIES_MSFT = 1000055001,
        XR_TYPE_CONTROLLER_MODEL_PROPERTIES_MSFT = 1000055002,
        XR_TYPE_CONTROLLER_MODEL_NODE_STATE_MSFT = 1000055003,
        XR_TYPE_CONTROLLER_MODEL_STATE_MSFT = 1000055004,
        XR_TYPE_VIEW_CONFIGURATION_VIEW_FOV_EPIC = 1000059000,
        XR_TYPE_HOLOGRAPHIC_WINDOW_ATTACHMENT_MSFT = 1000063000,
        XR_TYPE_COMPOSITION_LAYER_REPROJECTION_INFO_MSFT = 1000066000,
        XR_TYPE_COMPOSITION_LAYER_REPROJECTION_PLANE_OVERRIDE_MSFT = 1000066001,
        XR_TYPE_ANDROID_SURFACE_SWAPCHAIN_CREATE_INFO_FB = 1000070000,
        XR_TYPE_COMPOSITION_LAYER_SECURE_CONTENT_FB = 1000072000,
        XR_TYPE_INTERACTION_PROFILE_ANALOG_THRESHOLD_VALVE = 1000079000,
        XR_TYPE_HAND_JOINTS_MOTION_RANGE_INFO_EXT = 1000080000,
        XR_TYPE_LOADER_INIT_INFO_ANDROID_KHR = 1000089000,
        XR_TYPE_VULKAN_INSTANCE_CREATE_INFO_KHR = 1000090000,
        XR_TYPE_VULKAN_DEVICE_CREATE_INFO_KHR = 1000090001,
        XR_TYPE_VULKAN_GRAPHICS_DEVICE_GET_INFO_KHR = 1000090003,
        XR_TYPE_COMPOSITION_LAYER_EQUIRECT2_KHR = 1000091000,
        XR_TYPE_SCENE_OBSERVER_CREATE_INFO_MSFT = 1000097000,
        XR_TYPE_SCENE_CREATE_INFO_MSFT = 1000097001,
        XR_TYPE_NEW_SCENE_COMPUTE_INFO_MSFT = 1000097002,
        XR_TYPE_VISUAL_MESH_COMPUTE_LOD_INFO_MSFT = 1000097003,
        XR_TYPE_SCENE_COMPONENTS_MSFT = 1000097004,
        XR_TYPE_SCENE_COMPONENTS_GET_INFO_MSFT = 1000097005,
        XR_TYPE_SCENE_COMPONENT_LOCATIONS_MSFT = 1000097006,
        XR_TYPE_SCENE_COMPONENTS_LOCATE_INFO_MSFT = 1000097007,
        XR_TYPE_SCENE_OBJECTS_MSFT = 1000097008,
        XR_TYPE_SCENE_COMPONENT_PARENT_FILTER_INFO_MSFT = 1000097009,
        XR_TYPE_SCENE_OBJECT_TYPES_FILTER_INFO_MSFT = 1000097010,
        XR_TYPE_SCENE_PLANES_MSFT = 1000097011,
        XR_TYPE_SCENE_PLANE_ALIGNMENT_FILTER_INFO_MSFT = 1000097012,
        XR_TYPE_SCENE_MESHES_MSFT = 1000097013,
        XR_TYPE_SCENE_MESH_BUFFERS_GET_INFO_MSFT = 1000097014,
        XR_TYPE_SCENE_MESH_BUFFERS_MSFT = 1000097015,
        XR_TYPE_SCENE_MESH_VERTEX_BUFFER_MSFT = 1000097016,
        XR_TYPE_SCENE_MESH_INDICES_UINT32_MSFT = 1000097017,
        XR_TYPE_SCENE_MESH_INDICES_UINT16_MSFT = 1000097018,
        XR_TYPE_SERIALIZED_SCENE_FRAGMENT_DATA_GET_INFO_MSFT = 1000098000,
        XR_TYPE_SCENE_DESERIALIZE_INFO_MSFT = 1000098001,
        XR_TYPE_EVENT_DATA_DISPLAY_REFRESH_RATE_CHANGED_FB = 1000101000,
        XR_TYPE_VIVE_TRACKER_PATHS_HTCX = 1000103000,
        XR_TYPE_EVENT_DATA_VIVE_TRACKER_CONNECTED_HTCX = 1000103001,
        XR_TYPE_SYSTEM_FACIAL_TRACKING_PROPERTIES_HTC = 1000104000,
        XR_TYPE_FACIAL_TRACKER_CREATE_INFO_HTC = 1000104001,
        XR_TYPE_FACIAL_EXPRESSIONS_HTC = 1000104002,
        XR_TYPE_SYSTEM_COLOR_SPACE_PROPERTIES_FB = 1000108000,
        XR_TYPE_HAND_TRACKING_MESH_FB = 1000110001,
        XR_TYPE_HAND_TRACKING_SCALE_FB = 1000110003,
        XR_TYPE_HAND_TRACKING_AIM_STATE_FB = 1000111001,
        XR_TYPE_HAND_TRACKING_CAPSULES_STATE_FB = 1000112000,
        XR_TYPE_FOVEATION_PROFILE_CREATE_INFO_FB = 1000114000,
        XR_TYPE_SWAPCHAIN_CREATE_INFO_FOVEATION_FB = 1000114001,
        XR_TYPE_SWAPCHAIN_STATE_FOVEATION_FB = 1000114002,
        XR_TYPE_FOVEATION_LEVEL_PROFILE_CREATE_INFO_FB = 1000115000,
        XR_TYPE_KEYBOARD_SPACE_CREATE_INFO_FB = 1000116009,
        XR_TYPE_KEYBOARD_TRACKING_QUERY_FB = 1000116004,
        XR_TYPE_SYSTEM_KEYBOARD_TRACKING_PROPERTIES_FB = 1000116002,
        XR_TYPE_TRIANGLE_MESH_CREATE_INFO_FB = 1000117001,
        XR_TYPE_SYSTEM_PASSTHROUGH_PROPERTIES_FB = 1000118000,
        XR_TYPE_PASSTHROUGH_CREATE_INFO_FB = 1000118001,
        XR_TYPE_PASSTHROUGH_LAYER_CREATE_INFO_FB = 1000118002,
        XR_TYPE_COMPOSITION_LAYER_PASSTHROUGH_FB = 1000118003,
        XR_TYPE_GEOMETRY_INSTANCE_CREATE_INFO_FB = 1000118004,
        XR_TYPE_GEOMETRY_INSTANCE_TRANSFORM_FB = 1000118005,
        XR_TYPE_PASSTHROUGH_STYLE_FB = 1000118020,
        XR_TYPE_PASSTHROUGH_COLOR_MAP_MONO_TO_RGBA_FB = 1000118021,
        XR_TYPE_PASSTHROUGH_COLOR_MAP_MONO_TO_MONO_FB = 1000118022,
        XR_TYPE_EVENT_DATA_PASSTHROUGH_STATE_CHANGED_FB = 1000118030,
        XR_TYPE_RENDER_MODEL_PATH_INFO_FB = 1000119000,
        XR_TYPE_RENDER_MODEL_PROPERTIES_FB = 1000119001,
        XR_TYPE_RENDER_MODEL_BUFFER_FB = 1000119002,
        XR_TYPE_RENDER_MODEL_LOAD_INFO_FB = 1000119003,
        XR_TYPE_SYSTEM_RENDER_MODEL_PROPERTIES_FB = 1000119004,
        XR_TYPE_BINDING_MODIFICATIONS_KHR = 1000120000,
        XR_TYPE_VIEW_LOCATE_FOVEATED_RENDERING_VARJO = 1000121000,
        XR_TYPE_FOVEATED_VIEW_CONFIGURATION_VIEW_VARJO = 1000121001,
        XR_TYPE_SYSTEM_FOVEATED_RENDERING_PROPERTIES_VARJO = 1000121002,
        XR_TYPE_COMPOSITION_LAYER_DEPTH_TEST_VARJO = 1000122000,
        XR_TYPE_SYSTEM_MARKER_TRACKING_PROPERTIES_VARJO = 1000124000,
        XR_TYPE_EVENT_DATA_MARKER_TRACKING_UPDATE_VARJO = 1000124001,
        XR_TYPE_MARKER_SPACE_CREATE_INFO_VARJO = 1000124002,
        XR_TYPE_SPATIAL_ANCHOR_PERSISTENCE_INFO_MSFT = 1000142000,
        XR_TYPE_SPATIAL_ANCHOR_FROM_PERSISTED_ANCHOR_CREATE_INFO_MSFT = 1000142001,
        XR_TYPE_SWAPCHAIN_IMAGE_FOVEATION_VULKAN_FB = 1000160000,
        XR_TYPE_SWAPCHAIN_STATE_ANDROID_SURFACE_DIMENSIONS_FB = 1000161000,
        XR_TYPE_SWAPCHAIN_STATE_SAMPLER_OPENGL_ES_FB = 1000162000,
        XR_TYPE_SWAPCHAIN_STATE_SAMPLER_VULKAN_FB = 1000163000,
        XR_TYPE_COMPOSITION_LAYER_SPACE_WARP_INFO_FB = 1000171000,
        XR_TYPE_SYSTEM_SPACE_WARP_PROPERTIES_FB = 1000171001,
        XR_TYPE_DIGITAL_LENS_CONTROL_ALMALENCE = 1000196000,
        XR_TYPE_PASSTHROUGH_KEYBOARD_HANDS_INTENSITY_FB = 1000203002,
        XR_TYPE_GRAPHICS_BINDING_VULKAN2_KHR = XR_TYPE_GRAPHICS_BINDING_VULKAN_KHR,
        XR_TYPE_SWAPCHAIN_IMAGE_VULKAN2_KHR = XR_TYPE_SWAPCHAIN_IMAGE_VULKAN_KHR,
        XR_TYPE_GRAPHICS_REQUIREMENTS_VULKAN2_KHR = XR_TYPE_GRAPHICS_REQUIREMENTS_VULKAN_KHR,
        XR_STRUCTURE_TYPE_MAX_ENUM = 0x7FFFFFFF,
    }

    /// <summary>
    /// Values for types of actions.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrActionType.html.
    /// </remarks>
    internal enum XrActionType
    {
        XR_ACTION_TYPE_BOOLEAN_INPUT = 1,
        XR_ACTION_TYPE_FLOAT_INPUT = 2,
        XR_ACTION_TYPE_VECTOR2F_INPUT = 3,
        XR_ACTION_TYPE_POSE_INPUT = 4,
        XR_ACTION_TYPE_VIBRATION_OUTPUT = 100,
        XR_ACTION_TYPE_MAX_ENUM = 0x7FFFFFFF,
    }

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    /// <summary>
    /// Information to create a hand joints handle.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrHandTrackerCreateInfoEXT.html.
    /// </remarks>
    internal struct XrHandTrackerCreateInfoEXT
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public XrHandEXT Hand;
        public XrHandJointSetEXT HandJointSet;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Information to create an action set.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrActionSetCreateInfo.html.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct XrActionSetCreateInfo
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string ActionSetName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string LocalizedActionSetName;
        public uint Priority;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Information to create an action.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrActionCreateInfo.html.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct XrActionCreateInfo
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string ActionName;
        public XrActionType ActionType;
        public uint CountSubactionPaths;
        public IntPtr SubactionPaths;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string LocalizedActionName;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Suggested binding for a single action.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrActionSuggestedBinding.html.
    /// </remarks>
    internal struct XrActionSuggestedBinding
    {
#pragma warning disable SA1600 // Elements should be documented
        public ulong Action;
        public ulong Binding;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Suggested bindings for an interaction profile.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrInteractionProfileSuggestedBinding.html.
    /// </remarks>
    internal struct XrInteractionProfileSuggestedBinding
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public ulong InteractionProfile;
        public uint CountSuggestedBindings;
        public IntPtr SuggestedBindings;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Information to attach action sets to a session.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrSessionActionSetsAttachInfo.html.
    /// </remarks>
    internal struct XrSessionActionSetsAttachInfo
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public uint CountActionSets;
        public IntPtr ActionSets;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Creation info for an action space.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrActionSpaceCreateInfo.html.
    /// </remarks>
    internal struct XrActionSpaceCreateInfo
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public ulong Action;
        public ulong SubactionPath;
        public XrPosef PoseInActionSpace;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Describes an active action set.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrActiveActionSet.html.
    /// </remarks>
    internal struct XrActiveActionSet
    {
#pragma warning disable SA1600 // Elements should be documented
        public ulong ActionSet;
        public ulong SubactionPath;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Information to sync actions.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrActionsSyncInfo.html.
    /// </remarks>
    internal struct XrActionsSyncInfo
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public uint CountActiveActionSets;
        public IntPtr ActiveActionSets;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Pose action metadata.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrActionStatePose.html.
    /// </remarks>
    internal struct XrActionStatePose
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public bool IsActive;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Information to get action state.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrActionStateGetInfo.html.
    /// </remarks>
    internal struct XrActionStateGetInfo
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public ulong Action;
        public ulong SubactionPath;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Contains info about a space.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrSpaceLocation.html.
    /// </remarks>
    internal struct XrSpaceLocation
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public ulong LocationFlags;
        public XrPosef Pose;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Eye gaze sample time structure.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrEyeGazeSampleTimeEXT.html.
    /// </remarks>
    internal struct XrEyeGazeSampleTimeEXT
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public long Time;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Describes the information to locate hand joints.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrHandJointsLocateInfoEXT.html.
    /// </remarks>
    internal struct XrHandJointsLocateInfoEXT
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public ulong BaseSpace;
        public long Time;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Returns the hand joint locations.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrHandJointLocationsEXT.html.
    /// </remarks>
    internal struct XrHandJointLocationsEXT
    {
#pragma warning disable SA1600 // Elements should be documented
        public XrStructureType Type;
        public IntPtr Next;
        public bool IsActive;
        public uint JointCount;
        public IntPtr JointLocations;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Describes the location and radius of a hand joint.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrHandJointLocationEXT.html.
    /// </remarks>
    internal struct XrHandJointLocationEXT
    {
#pragma warning disable SA1600 // Elements should be documented
        public ulong LocationFlags;
        public XrPosef Pose;
        public float Radius;
#pragma warning restore SA1600 // Elements should be documented
    }

    /// <summary>
    /// Location and orientation in a space.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrPosef.html
    /// We use the StereoKit <see cref="Quat"/> and <see cref="Vec3"/> types for convenience
    /// because they have the necessary structure already.
    /// </remarks>
    internal struct XrPosef
    {
#pragma warning disable SA1600 // Elements should be documented
        public Quat Orientation;
        public Vec3 Position;
#pragma warning restore SA1600 // Elements should be documented
    }
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

    /// <summary>
    /// Space location flags.
    /// </summary>
    /// <remarks>
    /// https://www.khronos.org/registry/OpenXR/specs/1.0/man/html/XrSpaceLocationFlags.html.
    /// </remarks>
    internal static class XrSpaceLocationFlags
    {
        /// <summary>
        /// <see cref="XR_SPACE_LOCATION_ORIENTATION_VALID_BIT"/> Indicates that the pose field's orientation field contains valid data.
        /// For a space location tracking a device with its own inertial tracking, <see cref="XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT"/>
        /// should remain set when this bit is set. Applications must not read the pose field's orientation if this flag is unset.
        /// </summary>
        internal const uint XR_SPACE_LOCATION_ORIENTATION_VALID_BIT = 0x00000001;

        /// <summary>
        /// <see cref="XR_SPACE_LOCATION_POSITION_VALID_BIT"/> indicates that the pose field's position field contains valid data.
        /// When a space location loses tracking, runtimes should continue to provide valid but untracked position values that are
        /// inferred or last-known, so long as it's still meaningful for the application to use that position, clearing
        /// <see cref="XR_SPACE_LOCATION_POSITION_TRACKED_BIT"/> until positional tracking is recovered.
        /// Applications must not read the pose field's position if this flag is unset.
        /// </summary>
        internal const uint XR_SPACE_LOCATION_POSITION_VALID_BIT = 0x00000002;

        /// <summary>
        /// <see cref="XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT"/> indicates that the pose field's orientation field represents
        /// an actively tracked orientation. For a space location tracking a device with its own inertial tracking, this bit should
        /// remain set when <see cref="XR_SPACE_LOCATION_ORIENTATION_VALID_BIT"/> is set. For a space location tracking an object
        /// whose orientation is no longer known during tracking loss (e.g. an observed QR code), runtimes should continue to provide
        /// valid but untracked orientation values, so long as it's still meaningful for the application to use that orientation.
        /// </summary>
        internal const uint XR_SPACE_LOCATION_ORIENTATION_TRACKED_BIT = 0x00000004;

        /// <summary>
        /// <see cref="XR_SPACE_LOCATION_POSITION_TRACKED_BIT"/> indicates that the pose field's position field represents an actively
        /// tracked position. When a space location loses tracking, runtimes should continue to provide valid but untracked position
        /// values that are inferred or last-known, e.g. based on neck model updates, inertial dead reckoning, or a last-known position,
        /// so long as it's still meaningful for the application to use that position.
        /// </summary>
        internal const uint XR_SPACE_LOCATION_POSITION_TRACKED_BIT = 0x00000008;
    }
}
#pragma warning restore SA1310 // Field names should not contain underscore
#pragma warning restore SA1602 // Enumeration items should be documented
#pragma warning restore SA1649 // File name should match first type name
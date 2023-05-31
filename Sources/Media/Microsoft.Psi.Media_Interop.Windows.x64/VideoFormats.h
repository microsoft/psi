// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once

#include "managed.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

/// <summary>
/// Forward declaration for video formats.
/// </summary>
ref class VideoFormats;

/// <summary>
/// Video format which stores associates between guids and names
/// </summary>
public ref class VideoFormat : IEquatable<VideoFormat ^>
{
private:
	static VideoFormat();

internal:

    /// <summary>
    /// Initializes a new instance of the <c>VideoFormat</c> class.
    /// </summary>
    /// <param name="guid">GUID of media type</param>
    /// <param name="name">FourCC of media type</param>
    VideoFormat(Guid guid, String^ name) : m_guid(guid),m_name(name)
    {
    };

private:
    /// <summary>
    /// Guid of media type
    /// </summary>
    Guid m_guid;

    /// <summary>
    /// FourCC of media type
    /// </summary>
    String^ m_name;

    static array<VideoFormat^>^ videoFormats;
public:

    /// <summary>
    /// Gets the Guid
    /// </summary>
    property System::Guid Guid
#ifdef DOXYGEN
      ;
#else
    {
        System::Guid get()
        {
            return m_guid;
        }
    }
#endif

    /// <summary>
    /// Gets the FourCC Name
    /// </summary>
    property String^ Name
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

    virtual bool Equals(Object ^obj) override;
    virtual bool Equals(VideoFormat ^other);
    static bool operator ==(VideoFormat^ left, VideoFormat^ right);
    static bool operator !=(VideoFormat^left, VideoFormat^ right);
    virtual int GetHashCode() override;
    static VideoFormat^ FromGuid(System::Guid guid);
    static VideoFormat^ FromName(String^ name);
};

/// <summary>
/// The video formats supported
/// </summary>
public ref class VideoFormats abstract sealed
{
public:
    /// <summary>
    /// Gets a VideoFormat instance for the GUID MFVideoFormat_YUY2
    /// </summary>
    static property VideoFormat^ VideoFormatYUY2 {VideoFormat^ get();}    

    /// <summary>
    /// Gets a VideoFormat instance for the GUID MFVideoFormat_MJPG
    /// </summary>
    static property VideoFormat^ VideoFormatMJPG {VideoFormat^ get();}
};
}}}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#include "StdAfx.h"

#include "VideoFormats.h"

namespace Microsoft {
namespace Psi {
namespace Media_Interop {

static VideoFormat::VideoFormat()
{
    videoFormats = gcnew array<VideoFormat^> {
        VideoFormats::VideoFormatYUY2,
        VideoFormats::VideoFormatMJPG
    };
}

/// <summary>
/// Returns true if obj is equal to this, false otherwise
/// </summary>
/// <param name="obj">The object to the compared</param>
bool VideoFormat::Equals(Object ^other)
{
    if (nullptr == other)
    {
        return false;
    }
    return Equals((VideoFormat ^)other);
}

/// <summary>
/// Returns true if obj is equal to this, false otherwise
/// </summary>
/// <param name="other">The VideoFormat to be compared</param>
/// <returns>True if equal, false otherwise</returns>
bool VideoFormat::Equals(VideoFormat ^other)
 {
    if (nullptr == other)
    {
        return false;
    }

    if (Object::ReferenceEquals(this, other))
    {
        return true;
    }

    return this->Guid == other->Guid &&
        0 == String::Compare(this->Name, other->Name, CultureInfo::InvariantCulture, System::Globalization::CompareOptions::IgnoreCase);
}

/// <summary>
/// Returns true if left is equal to right, false otherwise
/// </summary>
/// <param name="left">The first VideoFormat to be compared</param>
/// <param name="right">The second VideoFormat to be compared</param>
/// <returns>True if equal, false otherwise</returns>
bool VideoFormat::operator ==(VideoFormat^ left, VideoFormat^ right)
{
    if (Object::ReferenceEquals(left, nullptr) && 
        Object::ReferenceEquals(right, nullptr))
    {
        return true;
    }
    else if (Object::ReferenceEquals(left, nullptr))
    {
        return false;
    }
    else
    {
        return left->Equals(right);
    }
}

/// <summary>
/// Returns true if left is not equal to right, false otherwise
/// </summary>
/// <param name="left">The first VideoFormat to be compared</param>
/// <param name="right">The second VideoFormat to be compared</param>
/// <returns>True if not equal, false otherwise</returns>
bool VideoFormat::operator !=(VideoFormat^left, VideoFormat^ right)
{
    if (Object::ReferenceEquals(left, nullptr) && 
        Object::ReferenceEquals(right, nullptr))
    {
        return false;
    }
    else if (Object::ReferenceEquals(left, nullptr))
    {
        return true;
    }
    else
    {
        return !left->Equals(right);
    }
}

/// <summary>
/// Returns the hash code corresponding to the video format.
/// </summary>
/// <returns>Hashcode</returns>
int VideoFormat::GetHashCode()
{
    return m_guid.GetHashCode();
}

/// <summary>
/// Creates a video format from the given guid
/// </summary>
/// <param name="guid">The guid for which video format needs to be constructed</param>
/// <returns>video format from the given guid</returns>
VideoFormat^ VideoFormat::FromGuid(System::Guid guid)
{
    for each (VideoFormat^ format in videoFormats)
    {
        if (format->Guid == guid)
        {
            return format;
        }
    }

    return gcnew VideoFormat(guid, String::Empty);
};

/// <summary>
/// Creates a video format from the given fourcc name
/// </summary>
/// <param name="name">The fourcc name for which video format needs to be constructed</param>
/// <returns>video format from the given fourcc name</returns>
VideoFormat^ VideoFormat::FromName(String^ name)
{
    for each (VideoFormat^ format in videoFormats)
    {
        if (format->Name == name)
        {
            return format;
        }
    }

    return gcnew VideoFormat(System::Guid::Empty, String::Empty);
}

VideoFormat^ VideoFormats::VideoFormatYUY2::get() 
{ 
    return gcnew VideoFormat(FromGUID(::MFVideoFormat_YUY2), gcnew String("YUY2")); 
}

VideoFormat^ VideoFormats::VideoFormatMJPG::get() 
{ 
    return gcnew VideoFormat(FromGUID(::MFVideoFormat_MJPG), gcnew String("MJPG")); 
}

}}}

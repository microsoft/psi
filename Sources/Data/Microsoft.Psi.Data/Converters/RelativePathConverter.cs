// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Data.Converters
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides methods to convert a full path to a relative path and vice-versa during JSON serialization.
    /// </summary>
    /// <remarks>
    /// The root path to which converted paths should be made relative is passed in the serializer StreamingContext.
    /// In addition, the StreamingContext state File flag should be set, or no conversion will take place.
    /// </remarks>
    public class RelativePathConverter : JsonConverter<string>
    {
        /// <inheritdoc/>
        public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // read the original path string from
            JToken token = JToken.Load(reader);
            string value = token.ToString();

            // only perform path conversion for file serialization
            if (serializer.Context.State.HasFlag(StreamingContextStates.File))
            {
                // serializer context contains the path to the file from which we are deserializing (set when serializer was created)
                string filePath = (string)serializer.Context.Context;
                string rootPath = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(rootPath))
                {
                    // Combine the root and relative Uri to get the full path.
                    // Trailing directory separator char is required in order for
                    // relative directory paths to be correctly computed.
                    var rootUri = new Uri(AppendDirectorySeparatorChar(rootPath));
                    var valueUri = new Uri(AppendDirectorySeparatorChar(value), UriKind.RelativeOrAbsolute);

                    // if value is a relative URI, convert it to an absolute URI (relative to root URI)
                    if (!valueUri.IsAbsoluteUri)
                    {
                        var absoluteUri = new Uri(rootUri, valueUri);
                        value = absoluteUri.LocalPath;
                    }
                }
            }

            // ensure consistent directory separator char
            value = RemoveTrailingDirectorySeparatorChar(value);
            return value.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
        {
            // only perform path conversion for file serialization
            if (serializer.Context.State.HasFlag(StreamingContextStates.File))
            {
                // serializer context contains the path to the file to which we are serializing (set when serializer was created)
                string filePath = (string)serializer.Context.Context;
                string rootPath = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(rootPath))
                {
                    // Compute the relative Uri relative to the root path.
                    // Trailing directory separator char is required in order for
                    // relative directory paths to be correctly computed.
                    var rootUri = new Uri(AppendDirectorySeparatorChar(rootPath));
                    var valueUri = new Uri(AppendDirectorySeparatorChar(value), UriKind.RelativeOrAbsolute);

                    // if value is an absolute URI, attempt to convert it to a relative URI (relative to root URI)
                    if (valueUri.IsAbsoluteUri)
                    {
                        var relativeUri = rootUri.MakeRelativeUri(valueUri);

                        if (relativeUri.IsAbsoluteUri)
                        {
                            // if it was not possible to make the URI relative, use the full local path
                            value = relativeUri.LocalPath;
                        }
                        else
                        {
                            // unescape the relative path
                            value = Uri.UnescapeDataString(relativeUri.ToString());
                        }
                    }
                }
            }

            // ensure consistent directory separator char
            value = RemoveTrailingDirectorySeparatorChar(value);
            writer.WriteValue(value.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!string.IsNullOrEmpty(path) &&
                path[path.Length - 1] != Path.DirectorySeparatorChar &&
                path[path.Length - 1] != Path.AltDirectorySeparatorChar)
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        private static string RemoveTrailingDirectorySeparatorChar(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return path;
        }
    }
}

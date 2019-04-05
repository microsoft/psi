// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Static methods shared by the store reader and writer.
    /// </summary>
    internal static class StoreCommon
    {
        internal static readonly string CatalogFileName = "Catalog";
        internal static readonly string IndexFileName = "Index";
        internal static readonly string DataFileName = "Data";
        internal static readonly string LargeDataFileName = "LargeData";

        internal static string GetIndexFileName(string appName)
        {
            return appName + "." + IndexFileName;
        }

        internal static string GetCatalogFileName(string appName)
        {
            return appName + "." + CatalogFileName;
        }

        internal static string GetDataFileName(string appName)
        {
            return appName + "." + DataFileName;
        }

        internal static string GetLargeDataFileName(string appName)
        {
            return appName + "." + LargeDataFileName;
        }

        internal static bool TryGetPathToLatestVersion(string appName, string rootPath, out string fullPath)
        {
            fullPath = null;
            if (rootPath == null)
            {
                return true;
            }

            var fileName = GetDataFileName(appName);
            fullPath = Path.GetFullPath(rootPath);
            if (!Directory.EnumerateFiles(fullPath, fileName + "*").Any())
            {
                fullPath = Directory.EnumerateDirectories(fullPath, appName + ".*").OrderByDescending(d => Directory.GetCreationTimeUtc(d)).FirstOrDefault();
            }

            return fullPath != null;
        }

        internal static string GetPathToLatestVersion(string appName, string rootPath)
        {
            if (!TryGetPathToLatestVersion(appName, rootPath, out string path))
            {
                throw new InvalidOperationException($"No matching files found: {rootPath} \\[{appName}.*\\]{appName}*");
            }

            return path;
        }
    }
}

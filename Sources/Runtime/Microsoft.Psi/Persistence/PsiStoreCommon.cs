// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Static methods shared by the store reader and writer.
    /// </summary>
    internal static class PsiStoreCommon
    {
        internal static readonly string CatalogFileName = "Catalog";
        internal static readonly string IndexFileName = "Index";
        internal static readonly string DataFileName = "Data";
        internal static readonly string LargeDataFileName = "LargeData";
        internal static readonly string LivePsiStoreFileName = "Live";

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

        internal static string GetLivePsiStoreFileName(string appName)
        {
            return appName + "." + LivePsiStoreFileName;
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

            if (!Directory.Exists(fullPath))
            {
                return false;
            }

            if (!Directory.EnumerateFiles(fullPath, fileName + "*").Any())
            {
                fullPath = Directory.EnumerateDirectories(fullPath, appName + ".*").OrderByDescending(d => Directory.GetCreationTimeUtc(d)).FirstOrDefault();
            }

            return fullPath != null;
        }

        internal static long GetSize(string storeName, string storePath)
            => EnumerateStoreFiles(storeName, storePath).Select(fi => fi.Length).Sum();

        internal static IEnumerable<FileInfo> EnumerateStoreFiles(string storeName, string storePath)
        {
            string escapedStoreName = Regex.Escape(storeName);
            foreach (var fileName in Directory.EnumerateFiles(storePath))
            {
                var fileInfo = new FileInfo(fileName);
                if (Regex.Match(fileInfo.Name, $@"^{escapedStoreName}\.Catalog_\d\d\d\d\d\d\.psi$").Success ||
                    Regex.Match(fileInfo.Name, $@"^{escapedStoreName}\.Data_\d\d\d\d\d\d\.psi$").Success ||
                    Regex.Match(fileInfo.Name, $@"^{escapedStoreName}\.LargeData_\d\d\d\d\d\d\.psi$").Success ||
                    Regex.Match(fileInfo.Name, $@"^{escapedStoreName}\.Index_\d\d\d\d\d\d\.psi$").Success)
                {
                    yield return fileInfo;
                }
            }
        }

        internal static IEnumerable<(string Name, string Session, string Path)> EnumerateStores(string path, bool recursively = true)
        {
            return EnumerateStores(path, path, recursively);
        }

        /// <summary>
        /// Returns a deterministic hash code for the given string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>A deterministic hash of the string.</returns>
        internal static int GetDeterministicHashCode(this string str)
        {
            // Inspired by string.GetHashCode()
            // See https://referencesource.microsoft.com/#mscorlib/system/string.cs,0a17bbac4851d0d4
            // We are not using string.GetHashCode because
            // 1) It promises to not be stable between versions and
            // 2) We only have room for 30 bits when storing the type schema IDs
            unsafe
            {
                fixed (char* src = str)
                {
                    int hash1 = (5381 << 16) + 5381;
                    int hash2 = hash1;
                    int* pint = (int*)src;
                    int len = str.Length;
                    while (len > 2)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                        hash2 = ((hash2 << 5) + hash2 + (hash2 >> 27)) ^ pint[1];
                        pint += 2;
                        len -= 4;
                    }

                    if (len > 0)
                    {
                        hash1 = ((hash1 << 5) + hash1 + (hash1 >> 27)) ^ pint[0];
                    }

                    return hash1 + (hash2 * 1566083941);
                }
            }
        }

        private static IEnumerable<(string Name, string Session, string Path)> EnumerateStores(string rootPath, string currentPath, bool recursively)
        {
            // scan for any psi catalog files
            var catalogFileSuffix = $".{CatalogFileName}_000000.psi";
            var catalogFileSearchPattern = $"*{catalogFileSuffix}";

            foreach (var filename in Directory.EnumerateFiles(currentPath, catalogFileSearchPattern))
            {
                var fi = new FileInfo(filename);
                var storeName = fi.Name.Substring(0, fi.Name.Length - catalogFileSuffix.Length);
                var sessionName = (currentPath == rootPath) ? filename : Path.Combine(currentPath, filename).Substring(rootPath.Length);
                sessionName = sessionName.Substring(0, sessionName.Length - fi.Name.Length);
                sessionName = sessionName.Trim('\\');
                yield return (storeName, sessionName, currentPath);
            }

            if (recursively)
            {
                // now go through subfolders
                foreach (var directory in Directory.EnumerateDirectories(currentPath))
                {
                    foreach (var store in EnumerateStores(rootPath, Path.Combine(currentPath, directory), true))
                    {
                        yield return store;
                    }
                }
            }
        }
    }
}

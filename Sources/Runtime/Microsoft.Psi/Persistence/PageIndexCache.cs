// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Psi.Data;

    internal class PageIndexCache : IDisposable
    {
        private readonly object syncRoot = new object();
        private readonly string name;
        private IndexEntry[] pageIndex = new IndexEntry[0];
        private InfiniteFileReader indexReader;

        public PageIndexCache(string name, string path)
        {
            this.name = name;
            this.indexReader = new InfiniteFileReader(path, PsiStoreCommon.GetIndexFileName(name));
        }

        public void Dispose()
        {
            if (this.indexReader != null)
            {
                this.indexReader.Dispose();
                this.indexReader = null;
            }
        }

        public IndexEntry Search(DateTime time, bool useOriginatingTime)
        {
            // make a local copy to avoid a lock. The list is immutable, but the member variable can change (can point to a new list after an update).
            var indexList = this.pageIndex;

            // if this is a live index, make sure all the data published so far is loaded
            if (indexList.Length == 0 || CompareTime(time, indexList[indexList.Length - 1], useOriginatingTime) > 0)
            {
                this.Update();
                indexList = this.pageIndex;
            }

            if (indexList.Length == 0)
            {
                return default(IndexEntry);
            }

            int startIndex = 0;
            int endIndex = indexList.Length;
            if (CompareTime(time, indexList[0], useOriginatingTime) <= 0)
            {
                return indexList[0];
            }

            int midIndex = 0;
            while (startIndex < endIndex - 1)
            {
                midIndex = startIndex + ((endIndex - startIndex) / 2);
                var compResult = CompareTime(time, indexList[midIndex], useOriginatingTime);
                if (compResult > 0)
                {
                    startIndex = midIndex;
                }
                else
                {
                    endIndex = midIndex;
                }
            }

            return indexList[startIndex];
        }

        private static int CompareTime(DateTime time, IndexEntry entry, bool useOriginatingTime)
        {
            return useOriginatingTime ? time.CompareTo(entry.OriginatingTime) : time.CompareTo(entry.CreationTime);
        }

        private void Update()
        {
            if (this.indexReader == null || !Monitor.TryEnter(this.syncRoot))
            {
                // someone else is updating the list already
                return;
            }

            if (this.indexReader != null)
            {
                List<IndexEntry> newList = new List<IndexEntry>();
                while (this.indexReader.MoveNext())
                {
                    IndexEntry indexEntry;
                    unsafe
                    {
                        this.indexReader.Read((byte*)&indexEntry, sizeof(IndexEntry));
                    }

                    newList.Add(indexEntry);
                }

                if (!PsiStoreMonitor.IsStoreLive(this.name, this.indexReader.Path))
                {
                    this.indexReader.Dispose();
                    this.indexReader = null;
                }

                if (newList.Count > 0)
                {
                    var newIndex = new IndexEntry[this.pageIndex.Length + newList.Count];
                    Array.Copy(this.pageIndex, newIndex, this.pageIndex.Length);
                    newList.CopyTo(newIndex, this.pageIndex.Length);
                    this.pageIndex = newIndex;
                }
            }

            Monitor.Exit(this.syncRoot);
        }
    }
}

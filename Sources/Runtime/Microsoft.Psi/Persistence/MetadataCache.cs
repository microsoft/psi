// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Psi.Common;

    internal class MetadataCache : IDisposable
    {
        private readonly object syncRoot = new ();
        private readonly string name;
        private readonly string path;
        private readonly Action<IEnumerable<Metadata>, RuntimeInfo> entriesAdded;
        private volatile Dictionary<string, PsiStreamMetadata> streamDescriptors = new ();
        private volatile Dictionary<int, PsiStreamMetadata> streamDescriptorsById = new ();
        private InfiniteFileReader catalogReader;
        private TimeInterval messageCreationTimeInterval;
        private TimeInterval messageOriginatingTimeInterval;
        private TimeInterval streamTimeInterval;
        private RuntimeInfo runtimeInfo;

        public MetadataCache(string name, string path, Action<IEnumerable<Metadata>, RuntimeInfo> entriesAdded)
        {
            this.name = name;
            this.path = path;
            this.catalogReader = new InfiniteFileReader(path, PsiStoreCommon.GetCatalogFileName(name));
            this.entriesAdded = entriesAdded;

            // assume v0 for backwards compat. Update will fix this up if the file is newer.
            this.runtimeInfo = new RuntimeInfo(0);
            this.Update();
        }

        public RuntimeInfo RuntimeInfo => this.runtimeInfo;

        public IEnumerable<PsiStreamMetadata> AvailableStreams
        {
            get
            {
                this.Update();
                return this.streamDescriptors.Values;
            }
        }

        public TimeInterval MessageCreationTimeInterval
        {
            get
            {
                this.Update();
                return this.messageCreationTimeInterval;
            }
        }

        public TimeInterval MessageOriginatingTimeInterval
        {
            get
            {
                this.Update();
                return this.messageOriginatingTimeInterval;
            }
        }

        public TimeInterval StreamTimeInterval
        {
            get
            {
                this.Update();
                return this.streamTimeInterval;
            }
        }

        public void Dispose()
        {
            if (this.catalogReader != null)
            {
                lock (this.syncRoot)
                {
                    this.catalogReader.Dispose();
                    this.catalogReader = null;
                }
            }
        }

        public bool TryGet(string name, out PsiStreamMetadata metadata)
        {
            if (!this.streamDescriptors.ContainsKey(name) || !this.streamDescriptors[name].IsClosed)
            {
                this.Update();
            }

            return this.streamDescriptors.TryGetValue(name, out metadata);
        }

        public bool TryGet(int id, out PsiStreamMetadata metadata)
        {
            if (!this.streamDescriptorsById.ContainsKey(id) || !this.streamDescriptorsById[id].IsClosed)
            {
                this.Update();
            }

            return this.streamDescriptorsById.TryGetValue(id, out metadata);
        }

        public void Update()
        {
            if (this.catalogReader == null)
            {
                return;
            }

            // since the cache is possibly shared by several store readers,
            // we need to lock before making changes
            lock (this.syncRoot)
            {
                if (this.catalogReader == null || !this.catalogReader.HasMoreData())
                {
                    return;
                }

                byte[] buffer = new byte[1024]; // will resize as needed
                var newMetadata = new List<Metadata>();
                var newStreamDescriptors = new Dictionary<string, PsiStreamMetadata>(this.streamDescriptors);
                var newStreamDescriptorsById = new Dictionary<int, PsiStreamMetadata>(this.streamDescriptorsById);
                while (this.catalogReader.MoveNext())
                {
                    var count = this.catalogReader.ReadBlock(ref buffer);
                    var br = new BufferReader(buffer, count);
                    var meta = Metadata.Deserialize(br);
                    if (meta.Kind == MetadataKind.RuntimeInfo)
                    {
                        // we expect this to be first in the file (or completely missing in v0 files)
                        this.runtimeInfo = meta as RuntimeInfo;

                        // Need to review this. The issue was that the RemoteExporter is not writing
                        // out the RuntimeInfo to the stream. This causes the RemoteImporter side of things to
                        // never see a RuntimeInfo metadata object and thus it assumes that the stream is using
                        // version 0.0 of serialization (i.e. non-data-contract version) which causes a mismatch
                        // in the serialization resulting in throw from TypeSchema.ValidateCompatibleWith. This
                        // change fixes the issue.
                        newMetadata.Add(meta);
                    }
                    else
                    {
                        newMetadata.Add(meta);
                        if (meta.Kind == MetadataKind.StreamMetadata)
                        {
                            var sm = meta as PsiStreamMetadata;
                            sm.StoreName = this.name;
                            sm.StorePath = this.path;

                            // the same meta entry will appear multiple times (written on open and on close).
                            // The last one wins.
                            newStreamDescriptors[sm.Name] = sm;
                            newStreamDescriptorsById[sm.Id] = sm;
                        }
                    }
                }

                // compute the time ranges
                this.messageCreationTimeInterval = GetTimeRange(newStreamDescriptors.Values, meta => meta.MessageCreationTimeInterval);
                this.messageOriginatingTimeInterval = GetTimeRange(newStreamDescriptors.Values, meta => meta.MessageOriginatingTimeInterval);
                this.streamTimeInterval = GetTimeRange(newStreamDescriptors.Values, meta => meta.StreamTimeInterval);

                // clean up if the catalog is closed and we really reached the end
                if (!PsiStoreMonitor.IsStoreLive(this.name, this.path) && !this.catalogReader.HasMoreData())
                {
                    this.catalogReader.Dispose();
                    this.catalogReader = null;
                }

                // swap the caches
                this.streamDescriptors = newStreamDescriptors;
                this.streamDescriptorsById = newStreamDescriptorsById;

                // let the registered delegates know about the change
                if (newMetadata.Count > 0 && this.entriesAdded != null)
                {
                    this.entriesAdded(newMetadata, this.runtimeInfo);
                }
            }
        }

        private static TimeInterval GetTimeRange(IEnumerable<PsiStreamMetadata> descriptors, Func<PsiStreamMetadata, TimeInterval> timeIntervalSelector)
        {
            DateTime left = DateTime.MaxValue;
            DateTime right = DateTime.MinValue;
            if (descriptors.Count() == 0)
            {
                return TimeInterval.Empty;
            }

            descriptors = descriptors.Where(d => d.MessageCount > 0);
            if (descriptors.Count() == 0)
            {
                return TimeInterval.Empty;
            }

            foreach (var streamInfo in descriptors)
            {
                left = descriptors.Select(d => timeIntervalSelector(d).Left).Min();
                right = descriptors.Select(d => timeIntervalSelector(d).Right).Max();
            }

            if (left > right)
            {
                throw new Exception("The metadata appears to be invalid because the start time is greater than the end time: start = {left}, end = {right}");
            }

            return new TimeInterval(left, right);
        }
    }
}

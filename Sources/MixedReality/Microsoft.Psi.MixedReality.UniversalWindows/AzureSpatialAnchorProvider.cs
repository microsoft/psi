// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.MixedReality
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using MathNet.Spatial.Euclidean;
    using Microsoft.Azure.SpatialAnchors;
    using Windows.Perception.Spatial;

    /// <summary>
    /// Represents a provider for Azure cloud spatial anchors.
    /// </summary>
    public class AzureSpatialAnchorProvider : ISpatialAnchorProvider
    {
        private readonly CloudSpatialAnchorSession spatialAnchorSession;
        private readonly ConcurrentDictionary<string, CloudSpatialAnchor> spatialAnchors = new ();
        private Exception error;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSpatialAnchorProvider"/> class.
        /// </summary>
        /// <param name="accountId">Account-level ID for the Azure Spatial Anchors service.</param>
        /// <param name="accountDomain">Account domain for the Azure Spatial Anchors service.</param>
        /// <param name="accountKey">Account key for the Azure Spatial Anchors service.</param>
        /// <param name="logLevel">Logging level for the Azure Spatial Anchors session log events.</param>
        public AzureSpatialAnchorProvider(string accountId, string accountDomain, string accountKey, SessionLogLevel logLevel = SessionLogLevel.Error)
        {
            // Create the cloud spatial anchor session
            this.spatialAnchorSession = new CloudSpatialAnchorSession();
            this.spatialAnchorSession.Configuration.AccountId = accountId;
            this.spatialAnchorSession.Configuration.AccountDomain = accountDomain;
            this.spatialAnchorSession.Configuration.AccountKey = accountKey;
            this.spatialAnchorSession.LogLevel = logLevel;
            this.spatialAnchorSession.AnchorLocated += this.OnAnchorLocated;
            this.spatialAnchorSession.LocateAnchorsCompleted += this.OnLocateAnchorsCompleted;
            this.spatialAnchorSession.OnLogDebug += this.OnLogDebug;
            this.spatialAnchorSession.SessionUpdated += this.OnSessionUpdated;
            this.spatialAnchorSession.Error += this.OnError;
            this.spatialAnchorSession.Start();
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem)
        {
            CloudSpatialAnchor cloudAnchor = null;

            // SpatialAnchor.TryCreateRelativeTo could return null if either the maximum number of
            // spatial anchors has been reached, or if the world coordinate system could not be located.
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(spatialCoordinateSystem);

            if (spatialAnchor != null)
            {
                cloudAnchor = this.CreateCloudAnchor(spatialAnchor);
            }

            return (spatialAnchor, cloudAnchor?.Identifier);
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, CoordinateSystem coordinateSystem)
        {
            var spatialCoordinateSystem = coordinateSystem.TryConvertPsiCoordinateSystemToSpatialCoordinateSystem();
            return (spatialCoordinateSystem != null) ? this.TryCreateSpatialAnchor(id, spatialCoordinateSystem) : default;
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, Vector3 translation)
        {
            CloudSpatialAnchor cloudAnchor = null;

            // SpatialAnchor.TryCreateRelativeTo could return null if either the maximum number of
            // spatial anchors has been reached, or if the world coordinate system could not be located.
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(spatialCoordinateSystem, translation);

            if (spatialAnchor != null)
            {
                cloudAnchor = this.CreateCloudAnchor(spatialAnchor);
            }

            return (spatialAnchor, cloudAnchor?.Identifier);
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, Vector3 translation, System.Numerics.Quaternion rotation)
        {
            CloudSpatialAnchor cloudAnchor = null;

            // SpatialAnchor.TryCreateRelativeTo could return null if either the maximum number of
            // spatial anchors has been reached, or if the world coordinate system could not be located.
            var spatialAnchor = SpatialAnchor.TryCreateRelativeTo(spatialCoordinateSystem, translation, rotation);

            if (spatialAnchor != null)
            {
                cloudAnchor = this.CreateCloudAnchor(spatialAnchor);
            }

            return (spatialAnchor, cloudAnchor?.Identifier);
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryCreateSpatialAnchor(string id, SpatialCoordinateSystem spatialCoordinateSystem, CoordinateSystem relativeOffset)
        {
            Matrix4x4.Decompose(relativeOffset.RebaseToHoloLensSystemMatrix(), out _, out var rotation, out var translation);
            return this.TryCreateSpatialAnchor(id, spatialCoordinateSystem, translation, rotation);
        }

        /// <inheritdoc/>
        public (SpatialAnchor anchor, string id) TryUpdateSpatialAnchor(string id, CoordinateSystem coordinateSystem)
        {
            SpatialAnchor spatialAnchor = null;

            if (this.spatialAnchors.TryGetValue(id, out var cloudAnchor))
            {
                spatialAnchor = cloudAnchor.LocalAnchor;

                if (spatialAnchor.CoordinateSystem.TryConvertSpatialCoordinateSystemToPsiCoordinateSystem() != coordinateSystem)
                {
                    this.RemoveSpatialAnchor(id);
                    if (coordinateSystem != null)
                    {
                        (spatialAnchor, id) = this.TryCreateSpatialAnchor(id, coordinateSystem);
                    }
                }
            }

            return (spatialAnchor, id);
        }

        /// <inheritdoc/>
        public void RemoveSpatialAnchor(string id)
        {
            // Check for and throw the most recent session error
            this.ThrowIfError();

            if (this.spatialAnchors.TryRemove(id, out var anchor))
            {
                Exception exception = null;
                Task.Run(async () =>
                {
                    try
                    {
                        await this.spatialAnchorSession.DeleteAnchorAsync(anchor);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }
                }).Wait();

                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, SpatialAnchor> GetAllSpatialAnchors()
        {
            return this.spatialAnchors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.LocalAnchor);
        }

        /// <inheritdoc/>
        public Dictionary<string, CoordinateSystem> GetAllSpatialAnchorCoordinateSystems()
        {
            // Spatial anchors may not always be locatable at all points in time, so the result may contain null values
            return new Dictionary<string, CoordinateSystem>(
                this.spatialAnchors
                    .Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.LocalAnchor.CoordinateSystem.TryConvertSpatialCoordinateSystemToPsiCoordinateSystem())));
        }

        /// <inheritdoc/>
        public SpatialAnchor TryGetSpatialAnchor(string id)
        {
            // Check for and throw the most recent session error
            this.ThrowIfError();

            // Cloud spatial anchor identifiers should be valid GUIDs
            if (!Guid.TryParse(id, out _))
            {
                throw new ArgumentException($"The requested spatial anchor {id} is not a valid GUID", nameof(id));
            }

            // If the anchor is not in the dictionary, it may be because we are still looking for it
            if (!this.spatialAnchors.TryGetValue(id, out var cloudAnchor))
            {
                try
                {
                    // Only one active watcher is allowed at a time, so stop any active watchers
                    this.StopActiveWatchers();

                    // Add a null entry to spatialAnchors dictionary to indicate that we are looking for this
                    // anchor id. The value will be updated when the anchor is found or the watcher completes.
                    this.spatialAnchors[id] = null;

                    // New criteria for all anchor ids that we are currently still looking for (including the new id)
                    var anchorLocateCriteria = new AnchorLocateCriteria
                    {
                        Identifiers = this.spatialAnchors.Where(kvp => kvp.Value is null).Select(kvp => kvp.Key).ToArray(),
                        Strategy = LocateStrategy.AnyStrategy,
                    };

                    // Create the new watcher
                    var watcher = this.spatialAnchorSession.CreateWatcher(anchorLocateCriteria);
                    Trace.WriteLine($"Watcher id: {watcher.Identifier} created for anchor ids: {string.Join("\r\n    ", this.spatialAnchors.Keys)}");
                }
                catch (COMException e)
                {
                    Trace.WriteLine($"Failed to create watcher for anchor id: {id} (COMException: {e.HResult:X})");
                }
            }

            return cloudAnchor?.LocalAnchor;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.spatialAnchorSession.Stop();
            this.spatialAnchorSession.AnchorLocated -= this.OnAnchorLocated;
            this.spatialAnchorSession.LocateAnchorsCompleted -= this.OnLocateAnchorsCompleted;
            ((IDisposable)this.spatialAnchorSession).Dispose();
        }

        /// <summary>
        /// Handler for the AnchorLocated event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event arguments.</param>
        private void OnAnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            switch (args.Status)
            {
                case LocateAnchorStatus.Located:
                    Trace.WriteLine($"Anchor id: {args.Identifier} located");

                    // Add the located anchor to the dictionary
                    this.spatialAnchors[args.Identifier] = args.Anchor;
                    break;

                case LocateAnchorStatus.AlreadyTracked:
                    Trace.WriteLine($"Anchor id: {args.Identifier} already tracked");
                    break;

                case LocateAnchorStatus.NotLocatedAnchorDoesNotExist:
                    Trace.WriteLine($"Anchor id: {args.Identifier} does not exist");

                    // Remove the pending null entry from the dictionary
                    this.spatialAnchors.TryRemove(args.Identifier, out _);
                    break;

                case LocateAnchorStatus.NotLocated:
                    Trace.WriteLine($"Anchor id: {args.Identifier} not located");

                    // Remove the pending null entry from the dictionary
                    this.spatialAnchors.TryRemove(args.Identifier, out _);
                    break;
            }
        }

        /// <summary>
        /// Handler for the LocateAnchorsCompleted event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event arguments.</param>
        private void OnLocateAnchorsCompleted(object sender, LocateAnchorsCompletedEventArgs args)
        {
            Trace.WriteLine($"LocateAnchorsCompleted. Watcher id: {args.Watcher.Identifier} was " + (args.Cancelled ? "cancelled" : "stopped"));
        }

        /// <summary>
        /// Handler for the Error event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event arguments.</param>
        private void OnError(object sender, SessionErrorEventArgs args)
        {
            Trace.WriteLine($"Error code {args.ErrorCode}: {args.ErrorMessage}");
            this.SetError(new Exception($"Cloud spatial anchor service error: {args.ErrorCode}\r\n{args.ErrorMessage}"));
        }

        /// <summary>
        /// Handler for the SessionUpdated event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event arguments.</param>
        private void OnSessionUpdated(object sender, SessionUpdatedEventArgs args)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{nameof(args.Status.RecommendedForCreateProgress)}: {args.Status.RecommendedForCreateProgress}");
            sb.AppendLine($"{nameof(args.Status.ReadyForCreateProgress)}: {args.Status.ReadyForCreateProgress}");
            sb.AppendLine($"{nameof(args.Status.SessionCreateHash)}: {args.Status.SessionCreateHash}");
            sb.AppendLine($"{nameof(args.Status.SessionLocateHash)}: {args.Status.SessionLocateHash}");
            sb.AppendLine($"{nameof(args.Status.UserFeedback)}: {args.Status.UserFeedback}");
            Trace.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Handler for the LogDebug event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The event arguments.</param>
        private void OnLogDebug(object sender, OnLogDebugEventArgs args)
        {
            Trace.WriteLine(args.Message);
        }

        /// <summary>
        /// Creates a new cloud spatial anchor.
        /// </summary>
        /// <param name="spatialAnchor">The local spatial anchor from which to create the cloud spatial anchor.</param>
        /// <returns>The cloud spatial anchor that was created.</returns>
        private CloudSpatialAnchor CreateCloudAnchor(SpatialAnchor spatialAnchor)
        {
            // Check for and throw the most recent session error
            this.ThrowIfError();

            // Create a cloud spatial anchor with the application-specific identifier
            var cloudAnchor = new CloudSpatialAnchor
            {
                LocalAnchor = spatialAnchor,
            };

            Exception exception = null;

            // Try to save the anchor to the cloud
            Task.Run(async () =>
            {
                try
                {
                    await this.spatialAnchorSession.CreateAnchorAsync(cloudAnchor);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }).Wait();

            // Re-throw any exception that occurred while saving the anchor to the cloud
            if (exception != null)
            {
                throw exception;
            }

            this.spatialAnchors[cloudAnchor.Identifier] = cloudAnchor;
            Trace.WriteLine($"Anchor id: {cloudAnchor.Identifier} created");
            return cloudAnchor;
        }

        /// <summary>
        /// Stops any watchers that are currently active.
        /// </summary>
        private void StopActiveWatchers()
        {
            foreach (var watcher in this.spatialAnchorSession.GetActiveWatchers())
            {
                watcher.Stop();
            }
        }

        /// <summary>
        /// Sets an error condition which will cause all further provider calls to throw an exception.
        /// </summary>
        /// <param name="e">An exception representing the error condition.</param>
        private void SetError(Exception e)
        {
            this.error = e;
        }

        /// <summary>
        /// Checks if an error condition exists and if so throws the exception.
        /// </summary>
        private void ThrowIfError()
        {
            if (this.error != null)
            {
                throw this.error;
            }
        }
    }
}

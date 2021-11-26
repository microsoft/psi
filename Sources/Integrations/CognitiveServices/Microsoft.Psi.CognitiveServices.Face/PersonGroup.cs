// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Face
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.CognitiveServices.Vision.Face;
    using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
    using Microsoft.Psi.Data;

    /// <summary>
    /// Provides access to methods for creating and manipulating person groups.
    /// </summary>
    public static class PersonGroup
    {
        // Recognition02 is best since MAR 2019 (see https://westus.dev.cognitive.microsoft.com/docs/services/563879b61984550e40cbbe8d/operations/563879b61984550f30395244)
        private const string RecognitionModel = Azure.CognitiveServices.Vision.Face.Models.RecognitionModel.Recognition02;

        /// <summary>
        /// Creates a person group.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <param name="endpoint">The Azure service endpoint.</param>
        /// <param name="groupId">The group id.</param>
        /// <param name="groupName">The group name.</param>
        /// <param name="deleteExisting">Whether to delete existing group (if any).</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        /// <param name="throttleMs">The time to wait between calls.</param>
        /// <returns>Async task.</returns>
        public static async Task Create(string subscriptionKey, string endpoint, string groupId, string groupName, bool deleteExisting, Action<string> loggingCallback = null, int throttleMs = 5000)
        {
            using (var client = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey)) { Endpoint = endpoint })
            {
                loggingCallback?.Invoke($"Checking whether person group exists ({groupId}, {groupName})");
                bool groupExists = (await client.PersonGroup.ListAsync()).Where(g => g.PersonGroupId == groupId).Count() != 0;
                await Task.Delay(throttleMs);
                if (groupExists && deleteExisting)
                {
                    loggingCallback?.Invoke($"Deleting existing person group ({groupId}, {groupName})");
                    await client.PersonGroup.DeleteAsync(groupId);
                    await Task.Delay(throttleMs);
                }

                if (!groupExists || deleteExisting)
                {
                    loggingCallback?.Invoke($"Creating person group ({groupId}, {groupName})");
                    await client.PersonGroup.CreateAsync(groupId, groupName, recognitionModel: RecognitionModel);
                    await Task.Delay(throttleMs);
                }
            }
        }

        /// <summary>
        /// Creates a person group.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <param name="endpoint">The Azure service endpoint.</param>
        /// <param name="groupId">The group id.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        /// <param name="throttleMs">The time to wait between calls.</param>
        /// <returns>Async task.</returns>
        public static async Task Delete(string subscriptionKey, string endpoint, string groupId, Action<string> loggingCallback = null, int throttleMs = 5000)
        {
            using (var client = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey)) { Endpoint = endpoint })
            {
                try
                {
                    loggingCallback?.Invoke($"Deleting person group ('{groupId}').");
                    await client.PersonGroup.DeleteAsync(groupId);
                    loggingCallback?.Invoke($"Done.");
                }
                catch (APIErrorException ex)
                {
                    if (ex.Response.ReasonPhrase == "Not Found")
                    {
                        loggingCallback?.Invoke($"Person group does not exist ('{groupId}').");
                    }

                    throw;
                }

                await Task.Delay(throttleMs);
            }
        }

        /// <summary>
        /// Add faces to person group.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <param name="endpoint">The Azure service endpoint.</param>
        /// <param name="groupId">The group id.</param>
        /// <param name="directory">The directory in which to look for images (organize by per-person subdirectories).</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        /// <param name="throttleMs">The time to wait between calls.</param>
        /// <returns>Async task.</returns>
        public static async Task AddFaces(string subscriptionKey, string endpoint, string groupId, string directory, Action<string> loggingCallback = null, int throttleMs = 5000)
        {
            using (var client = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey)) { Endpoint = endpoint })
            {
                foreach (var sub in Directory.GetDirectories(directory))
                {
                    var name = Path.GetFileName(sub);
                    loggingCallback?.Invoke($"Adding person '{name}'");
                    var person = await client.PersonGroupPerson.CreateAsync(groupId, name);
                    await Task.Delay(throttleMs);
                    foreach (var file in Directory.GetFiles(sub))
                    {
                        try
                        {
                            var face = await client.PersonGroupPerson.AddFaceFromStreamAsync(groupId, person.PersonId, File.OpenRead(file));
                            loggingCallback?.Invoke($"  Face {Path.GetFileName(file)} ({face.PersistedFaceId})");
                            await Task.Delay(throttleMs);
                        }
                        catch (APIErrorException ex)
                        {
                            loggingCallback?.Invoke($"  ERROR: {Path.GetFileName(file)} ERROR: {ex.Body.Error.Message}"); // e.g. more than single face
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a person group.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <param name="endpoint">The Azure service endpoint.</param>
        /// <param name="groupId">The group id.</param>
        /// <param name="loggingCallback">An optional callback to which human-friendly information will be logged.</param>
        /// <param name="throttleMs">The time to wait between calls.</param>
        /// <returns>Async task.</returns>
        public static async Task Train(string subscriptionKey, string endpoint, string groupId, Action<string> loggingCallback = null, int throttleMs = 5000)
        {
            using (var client = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey)) { Endpoint = endpoint })
            {
                await client.PersonGroup.TrainAsync(groupId);
                while (true)
                {
                    var trainingStatus = await client.PersonGroup.GetTrainingStatusAsync(groupId);
                    await Task.Delay(throttleMs + 1000);
                    if (trainingStatus.Status == TrainingStatusType.Nonstarted)
                    {
                        loggingCallback?.Invoke("Waiting...");
                    }
                    else if (trainingStatus.Status == TrainingStatusType.Running)
                    {
                        loggingCallback?.Invoke("Training...");
                    }
                    else if (trainingStatus.Status == TrainingStatusType.Succeeded)
                    {
                        loggingCallback?.Invoke("Done.");
                        break;
                    }
                    else if (trainingStatus.Status == TrainingStatusType.Failed)
                    {
                        loggingCallback?.Invoke($"Failed: {trainingStatus.Message}");
                        break;
                    }
                }
            }
        }
    }
}

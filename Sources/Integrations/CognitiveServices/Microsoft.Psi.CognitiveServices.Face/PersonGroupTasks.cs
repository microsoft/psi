// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Face
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Psi.Imaging;

    /// <summary>
    /// Provides a set of tasks to prepare Azure face recognition person groups.
    /// </summary>
    public static class PersonGroupTasks
    {
        /// <summary>
        /// Creates a person group, adds faces and trains.
        /// </summary>
        /// <remarks>PsiStoreTool exec -t TrainPersonGroup -a my_azure_key;https://my_azure_domain.cognitiveservices.azure.com/;my_group_id;my_group_name;my_face_images;true;2000.</remarks>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <param name="endpoint">The Azure service endpoint.</param>
        /// <param name="groupId">The group id.</param>
        /// <param name="groupName">The group name.</param>
        /// <param name="directory">The directory in which to look for images (organize by per-person subdirectories).</param>
        /// <param name="deleteExisting">Whether to delete existing group (if any).</param>
        /// <param name="throttleMs">The time to wait between calls.</param>
        [Task(nameof(TrainPersonGroup), "Create, add faces to, and train person group in Azure.")]
        public static void TrainPersonGroup(string subscriptionKey, string endpoint, string groupId, string groupName, string directory, bool deleteExisting, int throttleMs)
        {
            PersonGroup.Create(subscriptionKey, endpoint, groupId, groupName, deleteExisting, Console.WriteLine, throttleMs).Wait();
            PersonGroup.AddFaces(subscriptionKey, endpoint, groupId, directory, Console.WriteLine, throttleMs).Wait();
            PersonGroup.Train(subscriptionKey, endpoint, groupId, Console.WriteLine, throttleMs).Wait();
        }

        /// <summary>
        /// Deletes a person group.
        /// </summary>
        /// <remarks>PsiStoreTool exec -t DeletePersonGroup -a my_azure_key;https://my_azure_domain.cognitiveservices.azure.com/;my_group_id;2000.</remarks>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <param name="endpoint">The Azure service endpoint.</param>
        /// <param name="groupId">The group id.</param>
        /// <param name="throttleMs">The time to wait between calls.</param>
        [Task(nameof(DeletePersonGroup), "Delete person group in Azure.")]
        public static void DeletePersonGroup(string subscriptionKey, string endpoint, string groupId, int throttleMs)
        {
            PersonGroup.Delete(subscriptionKey, endpoint, groupId, Console.WriteLine, throttleMs).Wait();
        }

        /// <summary>
        /// Tests a person group.
        /// </summary>
        /// <remarks>PsiStoreTool exec -t TestPersonGroup -a my_azure_key;https://my_azure_domain.cognitiveservices.azure.com/;my_group_id;my_test_images.</remarks>
        /// <param name="subscriptionKey">The subscription key.</param>
        /// <param name="endpoint">The Azure service endpoint.</param>
        /// <param name="groupId">The group id.</param>
        /// <param name="directory">The directory from which to look for test images.</param>
        [Task(nameof(TestPersonGroup), "Test person group in Azure.")]
        public static void TestPersonGroup(string subscriptionKey, string endpoint, string groupId, string directory)
        {
            using (var pipeline = Pipeline.Create())
            {
                var files = Generators.Sequence(pipeline, Directory.GetFiles(directory), TimeSpan.FromTicks(1));
                files
                    .Select(file => ImagePool.GetOrCreate(new Bitmap(File.OpenRead(file))))
                    .RecognizeFace(new FaceRecognizerConfiguration(subscriptionKey, endpoint, groupId))
                    .Join(files)
                    .Do(x =>
                    {
                        Console.WriteLine($"File: {Path.GetFileName(x.Item2)}");
                        foreach (var candidates in x.Item1)
                        {
                            foreach (var face in candidates)
                            {
                                Console.WriteLine($"  Face: {face.Name} {face.Confidence}");
                            }
                        }
                    });
                pipeline.Run();
            }
        }
    }
}

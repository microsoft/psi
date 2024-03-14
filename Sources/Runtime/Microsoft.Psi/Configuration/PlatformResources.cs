// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Global registry of named platform resources.
    /// </summary>
    public static class PlatformResources
    {
        private static readonly string Default = nameof(Default);
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> Resources = new ();

        /// <summary>
        /// Gets the default resource of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <returns>The default resource of the specified type.</returns>
        public static T GetDefault<T>() => (T)Resources[typeof(T)][Default];

        /// <summary>
        /// Gets a resource of a specified type by name.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="name">The name of the resource.</param>
        /// <returns>The resource with the specified name.</returns>
        public static T Get<T>(string name) => (T)Resources[typeof(T)][name];

        /// <summary>
        /// Registers the default resource of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="value">The value of the resource.</param>
        public static void RegisterDefault<T>(T value) => Register(Default, value);

        /// <summary>
        /// Registers a named resource of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="name">The name of the resource.</param>
        /// <param name="value">The value of the resource.</param>
        public static void Register<T>(string name, T value)
        {
            if (!Resources.ContainsKey(typeof(T)))
            {
                Resources.AddOrUpdate(typeof(T), new ConcurrentDictionary<string, object>(), (_, d) => d);
            }

            Resources[typeof(T)].AddOrUpdate(name, value, (_, _) => throw new Exception("A resource with the same type and name already exists"));
        }

        /// <summary>
        /// Tries to get the default resource of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="value">The value of the resource.</param>
        /// <returns>True if a default resource of the specified type exists, or false otherwise.</returns>
        public static bool TryGetDefault<T>(out T value) => TryGet(Default, out value);

        /// <summary>
        /// Tries to get a named resource of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="name">The name of the resource.</param>
        /// <param name="value">The value of the resource.</param>
        /// <returns>True if a resource with the specified name exists, or false otherwise.</returns>
        public static bool TryGet<T>(string name, out T value)
        {
            if (!Resources.ContainsKey(typeof(T)) || !Resources[typeof(T)].ContainsKey(name))
            {
                value = default;
                return false;
            }
            else
            {
                value = (T)Resources[typeof(T)][name];
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether a default resource with the specified type exists.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <returns>True if the resource contains the specified name, otherwise false.</returns>
        public static bool ContainsDefault<T>() => Contains<T>(Default);

        /// <summary>
        /// Gets a value indicating whether a named resource with the specified type exists.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="name">The name of the resource.</param>
        /// <returns>True if the named resource of the specified type exists, otherwise false.</returns>
        public static bool Contains<T>(string name) => Resources.ContainsKey(typeof(T)) && Resources[typeof(T)].ContainsKey(name);

        /// <summary>
        /// Gets an enumeration with the names of resources of a specified type.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <returns>An enumeration with the names of resources of a specified type.</returns>
        public static IEnumerable<string> Enumerate<T>()
            => Resources.ContainsKey(typeof(T)) ? Resources[typeof(T)].Keys : Enumerable.Empty<string>();
    }
}

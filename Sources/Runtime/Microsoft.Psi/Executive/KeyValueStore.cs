// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Executive
{
    using System.Collections.Concurrent;

    /// <summary>
    /// Global store for key/value pairs that can be shared between components via the ApplicationCatalog.
    /// Adding a value with an existing name overrides the previous value.
    /// </summary>
    internal class KeyValueStore
    {
        public static readonly string GlobalNamespace = "global";
        private readonly ConcurrentDictionary<string, object> globalNamespace = new ();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, object>> namespaces = new ();

        public KeyValueStore()
        {
            this.namespaces[GlobalNamespace] = this.globalNamespace;
        }

        public T Get<T>(string namespaceName, string name)
        {
            return (T)this.namespaces[namespaceName][name];
        }

        public void Set<T>(string namespaceName, string name, T value)
        {
            if (!this.namespaces.ContainsKey(namespaceName))
            {
                this.namespaces[namespaceName] = new ConcurrentDictionary<string, object>();
            }

            this.namespaces[namespaceName][name] = value;
        }

        public bool TryGet<T>(string namespaceName, string name, out T value)
        {
            if (!this.namespaces.ContainsKey(namespaceName) || !this.namespaces[namespaceName].ContainsKey(name))
            {
                value = default;
                return false;
            }

            value = (T)this.namespaces[namespaceName][name];
            return true;
        }

        public bool Contains(string namespaceName, string name)
        {
            return this.namespaces.ContainsKey(namespaceName) && this.namespaces[namespaceName].ContainsKey(name);
        }
    }
}

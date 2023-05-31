// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Xml;
    using Microsoft.Psi.Visualization.VisualizationObjects;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Represents a data contract resolver for visualization types.
    /// </summary>
    public class VisualizationTypeResolver : DataContractResolver
    {
        private Dictionary<string, Type> typeMap = new Dictionary<string, Type>();

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizationTypeResolver"/> class.
        /// </summary>
        public VisualizationTypeResolver()
        {
            var allTypes = this.GetType().Assembly.GetTypes();
            allTypes.Where(type => type.IsSubclassOf(typeof(VisualizationObject))).ToList().ForEach((type) => this.AddType(type));
            allTypes.Where(type => type.IsSubclassOf(typeof(VisualizationPanel))).ToList().ForEach((type) => this.AddType(type));
            this.AddType(typeof(VisualizationObject));
            this.AddType(typeof(VisualizationPanel));
            this.AddType(typeof(VisualizationContainer));
        }

        /// <inheritdoc />
        public override bool TryResolveType(Type type, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
        {
            if (this.typeMap.ContainsKey(typeName))
            {
                return this.typeMap[typeName];
            }
            else
            {
                return knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, knownTypeResolver);
            }
        }

        private void AddType(Type type)
        {
            this.typeMap[type.Name] = type;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Annotations
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents a view model of an annotation schema.
    /// </summary>
    public class AnnotationSchemaRegistryViewModel : ObservableObject
    {
        private static AnnotationSchemaRegistryViewModel defaultRegistry = new AnnotationSchemaRegistryViewModel(AnnotationSchemaRegistry.Default);
        private ObservableCollection<AnnotationSchemaViewModel> internalSchemas;
        private ReadOnlyObservableCollection<AnnotationSchemaViewModel> schemas;
        private AnnotationSchemaRegistry registry;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSchemaRegistryViewModel"/> class.
        /// </summary>
        /// <param name="registry">The underlying annotation schema registry.</param>
        public AnnotationSchemaRegistryViewModel(AnnotationSchemaRegistry registry)
        {
            this.internalSchemas = new ObservableCollection<AnnotationSchemaViewModel>();
            this.schemas = new ReadOnlyObservableCollection<AnnotationSchemaViewModel>(this.internalSchemas);
            this.registry = registry;
            foreach (var item in this.registry.Schemas)
            {
                this.internalSchemas.Add(new AnnotationSchemaViewModel(item));
            }
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="AnnotationSchemaRegistryViewModel"/>.
        /// </summary>
        public static AnnotationSchemaRegistryViewModel Default => AnnotationSchemaRegistryViewModel.defaultRegistry;

        /// <summary>
        /// Gets the collection of annotation schemas in the registry.
        /// </summary>
        public ReadOnlyObservableCollection<AnnotationSchemaViewModel> Schemas => this.schemas;

        /// <summary>
        /// Registers an annotation schema with the registry.
        /// </summary>
        /// <param name="schema">The annotation schema to register.</param>
        public void Register(AnnotationSchema schema)
        {
            this.registry.Register(schema);
            this.internalSchemas.Add(new AnnotationSchemaViewModel(schema));
        }

        /// <summary>
        /// Unregisters an annotation schema with the registry.
        /// </summary>
        /// <param name="schema">The annotation schema to unregister.</param>
        public void Unregister(AnnotationSchema schema)
        {
            var schemaViewModel = this.internalSchemas.FirstOrDefault(s => s.Schema == schema);
            if (schemaViewModel == null)
            {
                throw new ArgumentOutOfRangeException("schema", $"Schema ({schema.Name}) not found in registry.");
            }

            this.internalSchemas.Remove(schemaViewModel);
            this.registry.Unregister(schema);
        }
    }
}

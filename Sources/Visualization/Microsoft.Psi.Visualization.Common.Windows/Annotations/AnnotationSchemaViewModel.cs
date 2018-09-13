// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Annotations
{
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents a view model of an annotation schema.
    /// </summary>
    public class AnnotationSchemaViewModel : ObservableObject
    {
        private ObservableCollection<AnnotationSchemaValueViewModel> internalSchemaValues;
        private ReadOnlyObservableCollection<AnnotationSchemaValueViewModel> schemaValues;
        private AnnotationSchema schema;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSchemaViewModel"/> class.
        /// </summary>
        /// <param name="schema">The underlying annotation schema.</param>
        public AnnotationSchemaViewModel(AnnotationSchema schema)
        {
            this.internalSchemaValues = new ObservableCollection<AnnotationSchemaValueViewModel>();
            this.schemaValues = new ReadOnlyObservableCollection<AnnotationSchemaValueViewModel>(this.internalSchemaValues);
            this.schema = schema;
            foreach (var item in this.schema.Values)
            {
                this.internalSchemaValues.Add(new AnnotationSchemaValueViewModel(item));
            }
        }

        /// <summary>
        /// Gets or sets the name of the annotation schema.
        /// </summary>
        public string Name
        {
            get => this.schema.Name;
            set
            {
                if (this.schema.Name != value)
                {
                    this.RaisePropertyChanging(nameof(this.Name));
                    this.schema.Name = value;
                    this.RaisePropertyChanged(nameof(this.Name));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the annotation schema is dynamic (i.e. new values can be added).
        /// </summary>
        public bool Dynamic
        {
            get => this.schema.Dynamic;
            set
            {
                if (this.schema.Dynamic != value)
                {
                    this.RaisePropertyChanging(nameof(this.Dynamic));
                    this.schema.Dynamic = value;
                    this.RaisePropertyChanged(nameof(this.Dynamic));
                }
            }
        }

        /// <summary>
        /// Gets the collection of annotation schema values that define this annotation schema.
        /// </summary>
        [Browsable(false)]
        public ReadOnlyObservableCollection<AnnotationSchemaValueViewModel> Values => this.schemaValues;

        /// <summary>
        /// Gets the underlying annotation schema.
        /// </summary>
        public AnnotationSchema Schema => this.schema;
    }
}

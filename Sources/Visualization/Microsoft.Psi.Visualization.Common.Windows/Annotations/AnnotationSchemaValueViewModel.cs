// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Annotations
{
    using System.Drawing;
    using Microsoft.Psi.Data.Annotations;
    using Microsoft.Psi.Visualization.Base;

    /// <summary>
    /// Represents an annotation schema value that can be used to construct an annotation schema.
    /// </summary>
    public class AnnotationSchemaValueViewModel : ObservableObject
    {
        private AnnotationSchemaValue schemaValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationSchemaValueViewModel"/> class.
        /// </summary>
        /// <param name="schemaValue">The annotation schema value.</param>
        public AnnotationSchemaValueViewModel(AnnotationSchemaValue schemaValue)
        {
            this.schemaValue = schemaValue;
        }

        /// <summary>
        /// Gets or sets the color to use when displaying annotations of this value.
        /// </summary>
        public Color Color
        {
            get => this.schemaValue.Color;
            set
            {
                if (this.schemaValue.Color != value)
                {
                    this.RaisePropertyChanging(nameof(this.Color));
                    this.schemaValue.Color = value;
                    this.RaisePropertyChanged(nameof(this.Color));
                }
            }
        }

        /// <summary>
        /// Gets or sets the description of this annotation schema value.
        /// </summary>
        public string Description
        {
            get => this.schemaValue.Description;
            set
            {
                if (this.schemaValue.Description != value)
                {
                    this.RaisePropertyChanging(nameof(this.Description));
                    this.schemaValue.Description = value;
                    this.RaisePropertyChanged(nameof(this.Description));
                }
            }
        }

        /// <summary>
        /// Gets or sets the keyboard shortcut of this annotation schema value.
        /// </summary>
        public string Shortcut
        {
            get => this.schemaValue.Shortcut;
            set
            {
                if (this.schemaValue.Shortcut != value)
                {
                    this.RaisePropertyChanging(nameof(this.Shortcut));
                    this.schemaValue.Shortcut = value;
                    this.RaisePropertyChanged(nameof(this.Shortcut));
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of this annotation schema value.
        /// </summary>
        public string Value
        {
            get => this.schemaValue.Value;
            set
            {
                if (this.schemaValue.Value != value)
                {
                    this.RaisePropertyChanging(nameof(this.Value));
                    this.schemaValue.Value = value;
                    this.RaisePropertyChanged(nameof(this.Value));
                }
            }
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Views.Visuals2D
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Psi.Data.Annotations;

    /// <summary>
    /// Provides a way to apply context menu style for annotated event visualization object view.
    /// </summary>
    public class AnnotationContextMenuStyleSelector : StyleSelector
    {
        /// <summary>
        /// Gets or sets annotation menu style.
        /// </summary>
        public Style AnnotationStyle { get; set; }

        /// <summary>
        /// Gets or sets command menu style.
        /// </summary>
        public Style CommandStyle { get; set; }

        /// <summary>
        /// Gets or sets separator menu style.
        /// </summary>
        public Style SeparatorStyle { get; set; }

        /// <inheritdoc />
        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item == null)
            {
                return this.SeparatorStyle;
            }
            else if (item is AnnotationSchema)
            {
                return this.AnnotationStyle;
            }
            else if (item is Tuple<string, ICommand, object>)
            {
                return this.CommandStyle;
            }

            return base.SelectStyle(item, container);
        }
    }
}

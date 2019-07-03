// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Helpers
{
    using System;
    using System.Windows;
    using System.Windows.Markup;

    /// <summary>
    /// Helper methods for use with XAML.
    /// </summary>
    public static class XamlHelper
    {
        /// <summary>
        /// Creates a data template give a view model type and a view type.
        /// </summary>
        /// <param name="viewModelType">The view model type.</param>
        /// <param name="viewType">The view type.</param>
        /// <returns>The newly created data template.</returns>
        public static DataTemplate CreateTemplate(Type viewModelType, Type viewType)
        {
            const string xamlTemplate = "<DataTemplate><v:{0} /></DataTemplate>";
            var xaml = string.Format(xamlTemplate, viewType.Name);

            var context = new ParserContext
            {
                XamlTypeMapper = new XamlTypeMapper(new string[0]),
            };
            context.XamlTypeMapper.AddMappingProcessingInstruction("vm", viewModelType.Namespace, viewModelType.Assembly.FullName);
            context.XamlTypeMapper.AddMappingProcessingInstruction("v", viewType.Namespace, viewType.Assembly.FullName);

            context.XmlnsDictionary.Add(string.Empty, "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
            context.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
            context.XmlnsDictionary.Add("vm", "vm");
            context.XmlnsDictionary.Add("v", "v");

            var template = (DataTemplate)XamlReader.Parse(xaml, context);
            return template;
        }
    }
}

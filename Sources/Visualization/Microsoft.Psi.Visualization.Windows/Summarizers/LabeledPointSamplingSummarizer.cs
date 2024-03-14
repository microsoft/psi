// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.Summarizers
{
    using System;
    using System.Windows;

    /// <summary>
    /// Implements a labeled point sampling summarizer.
    /// </summary>
    [Summarizer]
    public class LabeledPointSamplingSummarizer : SamplingSummarizer<Tuple<Point, string, string>>
    {
    }
}

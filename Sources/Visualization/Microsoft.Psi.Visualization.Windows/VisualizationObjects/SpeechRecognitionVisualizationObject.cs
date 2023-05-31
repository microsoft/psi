// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;
    using Microsoft.Psi.Visualization.VisualizationPanels;

    /// <summary>
    /// Implements a visualization object for <see cref="IStreamingSpeechRecognitionResult"/>.
    /// </summary>
    [VisualizationObject("Speech Recognition Results")]
    [VisualizationPanelType(VisualizationPanelType.Timeline)]
    public class SpeechRecognitionVisualizationObject : StreamIntervalVisualizationObject<IStreamingSpeechRecognitionResult>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(SpeechRecognitionVisualizationObjectView));
    }
}

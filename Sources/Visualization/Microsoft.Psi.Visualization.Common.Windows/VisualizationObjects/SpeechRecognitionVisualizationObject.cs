// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.Visualization.VisualizationObjects
{
    using System.Runtime.Serialization;
    using System.Windows;
    using Microsoft.Psi.Speech;
    using Microsoft.Psi.Visualization.Helpers;
    using Microsoft.Psi.Visualization.Views.Visuals2D;

    /// <summary>
    /// Represents a speech recognition visualization object.
    /// </summary>
    [VisualizationObject("Visualize Speech Recognition Data")]
    public class SpeechRecognitionVisualizationObject : TimelineVisualizationObject<IStreamingSpeechRecognitionResult>
    {
        /// <inheritdoc />
        [IgnoreDataMember]
        public override DataTemplate DefaultViewTemplate => XamlHelper.CreateTemplate(this.GetType(), typeof(SpeechRecognitionVisualizationObjectView));
    }
}

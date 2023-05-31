// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Language
{
    /// <summary>
    /// Represents the configuration for the <see cref="PersonalityChat"/> component.
    /// </summary>
    /// <remarks>
    /// Use this class to configure a new instance of the <see cref="PersonalityChat"/> component.
    /// Refer to the properties in this class for more information on the various configuration options.
    /// </remarks>
    public sealed class PersonalityChatConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersonalityChatConfiguration"/> class.
        /// </summary>
        public PersonalityChatConfiguration()
        {
            this.PersonalityChatSubscriptionID = null;
        }

        /// <summary>
        /// Gets or sets the subscription ID.
        /// </summary>
        public string PersonalityChatSubscriptionID { get; set; }
     }
}

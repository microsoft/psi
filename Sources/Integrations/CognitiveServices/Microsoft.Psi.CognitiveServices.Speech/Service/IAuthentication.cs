// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    /// <summary>
    /// IAuthentication defines our interface for the different types of authentication.
    /// </summary>
    internal interface IAuthentication
    {
        /// <summary>
        /// Gets the access token from the auth provider using the supplied information.
        /// </summary>
        /// <returns>the access token.</returns>
        string GetAccessToken();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        void Dispose();
    }
}

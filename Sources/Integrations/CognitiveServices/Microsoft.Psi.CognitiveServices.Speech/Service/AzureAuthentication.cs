// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Psi.CognitiveServices.Speech.Service
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides methods to get a valid O-auth token. Adapted from the following sample code:
    /// https://docs.microsoft.com/en-us/azure/cognitive-services/speech/how-to/how-to-authentication.
    /// </summary>
    public class AzureAuthentication : IDisposable, IAuthentication
    {
        // Access token expires every 10 minutes. Renew it every 8 minutes.
        private const int RefreshTokenDuration = 8;

        /// <summary>
        /// The token service URL.
        /// </summary>
        private static readonly string FetchTokenUri = "https://{0}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
        private string subscriptionKey;
        private string region;
        private string token;
        private Timer accessTokenRenewer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureAuthentication"/> class.
        /// </summary>
        /// <param name="subscriptionKey">The subscription key with which to request a token.</param>
        /// <param name="region">The region that is associated to the subscription key.</param>
        public AzureAuthentication(string subscriptionKey, string region)
        {
            this.subscriptionKey = subscriptionKey;
            this.region = region;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.accessTokenRenewer != null)
            {
                this.accessTokenRenewer.Dispose();
                this.accessTokenRenewer = null;
            }

            this.subscriptionKey = null;
            this.token = null;
        }

        /// <summary>
        /// Gets the current valid token, fetching a new token if necessary.
        /// </summary>
        /// <returns>The valid token.</returns>
        public string GetAccessToken()
        {
            // fetch a token the first time this is called
            if (this.token == null)
            {
                this.token = this.FetchToken(FetchTokenUri, this.subscriptionKey, this.region).Result;

                // renew the token on set duration.
                this.accessTokenRenewer = new Timer(
                    new TimerCallback(this.OnTokenExpiredCallback),
                    this,
                    TimeSpan.FromMinutes(RefreshTokenDuration),
                    TimeSpan.FromMilliseconds(-1));
            }

            return this.token;
        }

        private void RenewAccessToken()
        {
            this.token = this.FetchToken(FetchTokenUri, this.subscriptionKey, this.region).Result;
            Debug.WriteLine("Renewed token.");
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                this.RenewAccessToken();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    this.accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }

        private async Task<string> FetchToken(string fetchUri, string subscriptionKey, string region)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);

                fetchUri = string.Format(fetchUri, region);

                var result = await client.PostAsync(fetchUri, null);
                Debug.WriteLine(string.Format("Token Uri: {0}", fetchUri));

                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}

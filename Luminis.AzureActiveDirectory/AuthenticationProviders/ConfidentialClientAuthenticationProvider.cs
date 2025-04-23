// <copyright file="ConfidentialClientAuthenticationProvider.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory.AuthenticationProviders
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using Microsoft.Kiota.Abstractions;
    using Microsoft.Kiota.Abstractions.Authentication;

    /// <summary>
    /// Implements the <see cref="IAuthenticationProvider"/>.
    /// </summary>
    internal class ConfidentialClientAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string[] appScopes;
        private readonly string tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfidentialClientAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="clientId">The clientId.</param>
        /// <param name="clientSecret">The clientSecret.</param>
        /// <param name="appScopes">The scopes.</param>
        /// <param name="tenantId">The tanantId.</param>
        public ConfidentialClientAuthenticationProvider(string clientId, string clientSecret, string[] appScopes, string tenantId)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            this.appScopes = appScopes;
            this.tenantId = tenantId;
        }

        /// <summary>
        /// Adds authentication on the request.
        /// </summary>
        /// <param name="request">The request information.</param>
        /// <param name="additionalAuthenticationContext">Additional context for authentication.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object> additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            var clientApplication = ConfidentialClientApplicationBuilder.Create(this.clientId)
                               .WithClientSecret(this.clientSecret)
                               .WithClientId(this.clientId)
                               .WithTenantId(this.tenantId)
                               .Build();

            var result = await clientApplication.AcquireTokenForClient(this.appScopes).ExecuteAsync();

            request.Headers?.Add("Authorization", $"Bearer {result.AccessToken}");
        }
    }
}

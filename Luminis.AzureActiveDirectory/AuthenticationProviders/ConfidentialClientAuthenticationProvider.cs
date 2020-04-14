// <copyright file="ConfidentialClientAuthenticationProvider.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory.AuthenticationProviders
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Identity.Client;

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
        /// <param name="request">The request.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var clientApplication = ConfidentialClientApplicationBuilder.Create(this.clientId)
                           .WithClientSecret(this.clientSecret)
                           .WithClientId(this.clientId)
                           .WithTenantId(this.tenantId)

                           .Build();
            var result = await clientApplication.AcquireTokenForClient(this.appScopes).ExecuteAsync();
            request.Headers.Add("Authorization", result.CreateAuthorizationHeader());
        }
    }
}

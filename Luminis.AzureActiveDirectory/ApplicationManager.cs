// <copyright file="ApplicationManager.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Luminis.AzureActiveDirectory.AuthenticationProviders;
    using Luminis.AzureActiveDirectory.Models;
    using Microsoft.Graph;

    /// <summary>
    /// An application manager for Azure Active Directory.
    /// </summary>
    public class ApplicationManager : IApplicationManager
    {
        // See also: https://docs.microsoft.com/en-us/azure/active-directory-b2c/manage-user-accounts-graph-api?tabs=applications
        // Do not forget to enter 'Grant admin consent for Standaardmap: https://docs.microsoft.com/en-us/graph/auth-v2-service
        private const string AppScopes = "https://graph.microsoft.com/.default";
        private readonly GraphServiceClient graphClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationManager"/> class.
        /// </summary>
        /// <param name="clientId">The clientId.</param>
        /// <param name="clientSecret">The clientSecret.</param>
        /// <param name="tenantId">The tenantId.</param>
        public ApplicationManager(string clientId, string clientSecret, string tenantId)
        {
            var authenticationProvider = new ConfidentialClientAuthenticationProvider(clientId, clientSecret, AppScopes.Split(';'), tenantId);
            this.graphClient = new GraphServiceClient(authenticationProvider);
        }

        /// <summary>
        /// Get all applications.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<List<ApplicationInfo>> GetAllApplicationsAsync()
        {
            var result = new List<ApplicationInfo>();
            var applicationList = await this.graphClient.Applications.GetAsync();

            applicationList.Value?.ForEach(application =>
            {
                var applicationInfo = (ApplicationInfo)application;

                result.Add(applicationInfo);
            });

            return result;
        }
    }
}

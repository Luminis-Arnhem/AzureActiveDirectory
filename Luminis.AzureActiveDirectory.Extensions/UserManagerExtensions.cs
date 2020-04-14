// <copyright file="UserManagerExtensions.cs" company="Qirion BV">
// Copyright (c) Qirion BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Luminis.AzureActiveDirectory;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Helper method for checking user exists in group.
    /// </summary>
    public static class UserManagerExtensions
    {
        /// <summary>
        /// Checks if the user has right for this applicationid and this company.
        /// </summary>
        /// <param name="userManager">usermanager to use.</param>
        /// <param name="userId">id of logged in user.</param>
        /// <param name="groupName">Name of group userId should is expected to be in.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<bool> IsUserInGroup(this IUserManager userManager, string userId, string groupName)
        {
            var groups = await userManager.GetGroupsForUser(userId);
            return groups.Any(g => g.Name.Equals(groupName, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Extension method to add luminis active directory to dependency container.
        /// </summary>
        /// <param name="services">container to add to.</param>
        /// <param name="tenantId">tenantId of AAD</param>
        /// <param name="clientId">clientId of AAD</param>
        /// <param name="clientSecret">client secreat of AAD.</param>
        public static void AddLuminisActiveDirectory(this IServiceCollection services, string tenantId, string clientId, string clientSecret)
        {
            services.AddSingleton<IUserManager>(new UserManager(clientId, clientSecret, tenantId));
        }
    }
}

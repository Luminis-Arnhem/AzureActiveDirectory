// <copyright file="IApplicationManager.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Luminis.AzureActiveDirectory.Models;

    /// <summary>
    /// The interface for the application manager.
    /// </summary>
    public interface IApplicationManager
    {
        /// <summary>
        /// Gets a list of applications.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<List<ApplicationInfo>> GetAllApplicationsAsync();
    }
}

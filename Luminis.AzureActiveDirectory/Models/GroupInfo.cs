// <copyright file="GroupInfo.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory.Models
{
    using Microsoft.Graph.Models;

    /// <summary>
    /// Group information.
    /// </summary>
    public class GroupInfo
    {
        /// <summary>
        /// Gets or sets the id of the group.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Maps a Group to a GroupInfo object.
        /// </summary>
        /// <param name="group">The graph group to map.</param>
        public static explicit operator GroupInfo(Group group)
        {
            return new GroupInfo
            {
                Id = group.Id,
                Name = group.DisplayName,
            };
        }
    }
}

// <copyright file="UserInfo.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory.Models
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Graph;

    /// <summary>
    /// User information.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserInfo"/> class.
        /// </summary>
        public UserInfo()
        {
            this.Groups = new List<GroupInfo>();
        }

        /// <summary>
        /// Gets or sets the id of the user.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets first name.
        /// </summary>
        public string FirsName { get; set; }

        /// <summary>
        /// Gets or sets lastname.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets display name. This is usually the combination of the user's first name, middle initial and last name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets or set the username. This is de username which is used for login. In most cases it is an email adress.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets or set the timestamp of the last signin action.
        /// </summary>
        public DateTimeOffset? LastSignedIn { get; set; }

        /// <summary>
        /// Gets groups the user is member of.
        /// </summary>
        public List<GroupInfo> Groups { get; set; }

        /// <summary>
        /// Maps a User to a UserInfo object.
        /// </summary>
        /// <param name="user">The graph user to map.</param>
        public static explicit operator UserInfo(User user)
        {
            return new UserInfo
            {
                Id = user.Id,
                FirsName = user.GivenName,
                LastName = user.Surname,
                DisplayName = user.DisplayName != "unknown" ? user.DisplayName : $"{user.GivenName} {user.Surname}",
                Username = user.UserPrincipalName,
            };
        }
    }
}

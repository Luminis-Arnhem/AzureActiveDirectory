// <copyright file="UserInfo.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Graph.Models;

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
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets display name. This is usually the combination of the user's first name, middle initial and last name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets or set the user principal name. This is the username which is used for login. In most cases it is an email adress.
        /// </summary>
        public string UserPrincipalName { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last signin action.
        /// </summary>
        public DateTimeOffset? LastSignedIn { get; set; }

        /// <summary>
        /// Gets or sets the groups the user is a member of.
        /// </summary>
        public List<GroupInfo> Groups { get; set; }

        /// <summary>
        /// Gets or sets the e-mail address the user uses to sign into the B2C account.
        /// </summary>
        public string B2cSignInEmail { get; set; }

        /// <summary>
        /// Gets or sets the external user state of the user.
        /// </summary>
        public string ExternalUserState { get; set; }

        /// <summary>
        /// Maps a User to a UserInfo object.
        /// </summary>
        /// <param name="user">The graph user to map.</param>
        public static explicit operator UserInfo(User user)
        {
            return new UserInfo
            {
                Id = user.Id,
                FirstName = user.GivenName,
                LastName = user.Surname,
                DisplayName = user.DisplayName != "unknown" ? user.DisplayName : $"{user.GivenName} {user.Surname}",
                UserPrincipalName = user.UserPrincipalName,
                B2cSignInEmail = user.Identities?.FirstOrDefault(x => x.SignInType == "emailAddress")?.IssuerAssignedId,
                ExternalUserState = user.ExternalUserState,
            };
        }
    }
}

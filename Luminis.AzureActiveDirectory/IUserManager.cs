// <copyright file="IUserManager.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Luminis.AzureActiveDirectory.Models;

    /// <summary>
    /// A user manager.
    /// </summary>
    public interface IUserManager
    {
        /// <summary>
        /// Gets the user details for the given user.
        /// </summary>
        /// <param name="userId">The user to get the details for.</param>
        /// <param name="includeSignInData">Indicates signin data should be retrieve from Azure AD. <remarks>Take care, only logins of the last 7 days are retrieved.</remarks></param>
        /// <param name="includeGroups">Indicates the groups the user belongs to should also be retrieved.</param>
        /// <remarks>Take care, only logins of the last 7 days are retrieved.</remarks>
        /// <returns>A <see cref="UserInfo"/> object, holding the user details.</returns>
        Task<UserInfo> GetUserInfo(string userId, bool includeSignInData = false, bool includeGroups = false);

        /// <summary>
        /// Returns a list of all users in the given group.
        /// </summary>
        /// <param name="group">The group to get all users for.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<List<UserInfo>> GetAllUsersInGroup(string group);

        /// <summary>
        /// Invite a user.
        /// </summary>
        /// <param name="displayName">The displayname of the user.</param>
        /// <param name="emailAddress">The emailaddress to send the invitation to.</param>
        /// <param name="redirectUrl">The url the user will be redirected to after accepting the invitation.null</param>
        /// <param name="givenName">The given name (first name) of the user.</param>
        /// <param name="surname">The surname (family name or last name) of the user.</param>
        /// <param name="letAzureSendRedeemMail">Let azure send the redeem email.</param>
        /// <param name="messageBody">The messagebody for the invitation email.</param>
        /// <param name="messageLanguage">The language for the invitation email.</param>
        /// <param name="companyName">The companyname.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation - returning a tuple of the created user and the inviteRedeemUrl.</returns>
        Task<(UserInfo user, string InviteRedeemUrl)> InviteUser(
            string displayName,
            string emailAddress,
            string redirectUrl,
            bool letAzureSendRedeemMail = true,
            string messageBody = null,
            string companyName = null,
            string messageLanguage = "nl-NL",
            string givenName = null,
            string surname = null);

        /// <summary>
        /// Checks if a user with the given emailAddress is already invited.
        /// </summary>
        /// <param name="emailAddress">The emailaddress to send the invitation to.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<bool> IsInvited(string emailAddress);

        /// <summary>
        /// Deletes the given user from the Active Directory.
        /// </summary>
        /// <param name="userId">The userId to delete.</param>
        /// <returns>A <see cref="Task"/> that can be awaited.</returns>
        Task DeleteUser(string userId);

        /// <summary>
        /// Update the details of a user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="displayName">The displayname of the user.</param>
        /// <param name="firstName">The firstname of the user.</param>
        /// <param name="lastName">The lastname of the user.</param>
        /// <param name="companyName">The companyname of the user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UpdateUser(string userId, string displayName, string firstName = null, string lastName = null, string companyName = null);

        /// <summary>
        /// Gets all users.
        /// </summary>
        /// <param name="includeSignInData">Indicates signin data should be retrieve from Azure AD. <remarks>Take care, only logins of the last 7 days are retrieved.</remarks></param>
        /// <returns>A list of <see cref="UserInfo"/> objects, holding the user details.</returns>
        Task<IEnumerable<UserInfo>> GetAllUsers(bool includeSignInData = false);

        /// <summary>
        /// Gets all groups.
        /// </summary>
        /// <returns>A list of <see cref="GroupInfo"/> objects holding group details.</returns>
        Task<List<GroupInfo>> GetAllGroups();

        /// <summary>
        /// Get the groups a user is assigned to.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<List<GroupInfo>> GetGroupsForUser(string userId);

        /// <summary>
        /// Add a new group.
        /// </summary>
        /// <param name="name">The name of the group.</param>
        /// <param name="parentGroupId">The parent group.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<GroupInfo> AddGroup(string name, string parentGroupId = null);

        /// <summary>
        /// Add an user to the group.
        /// </summary>
        /// <param name="groupId">The id of the group.</param>
        /// <param name="userId">The id of the user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddUserToGroup(string groupId, string userId);

        /// <summary>
        /// Remove an user from the group.
        /// </summary>
        /// <param name="groupId">The id of the group.</param>
        /// <param name="userId">The id of the user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RemoveUserFromGroup(string groupId, string userId);
    }
}

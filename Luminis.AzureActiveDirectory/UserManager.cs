﻿// <copyright file="UserManager.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Luminis.AzureActiveDirectory.AuthenticationProviders;
    using Luminis.AzureActiveDirectory.Exceptions;
    using Luminis.AzureActiveDirectory.Models;
    using Microsoft.Graph;

    /// <summary>
    /// A user manager for Azure Active Directory.
    /// </summary>
    public class UserManager : IUserManager
    {
        // See also: https://docs.microsoft.com/en-us/azure/active-directory-b2c/manage-user-accounts-graph-api?tabs=applications
        // Do not forget to enter 'Grant admin consent for Standaardmap: https://docs.microsoft.com/en-us/graph/auth-v2-service
        private const string AppScopes = "https://graph.microsoft.com/.default";
        private readonly IGraphServiceClient graphClient;
        private readonly IAuthenticationProvider authenticationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserManager"/> class.
        /// The arguments can be found in your application registration in Azure Active Directory.
        /// </summary>
        /// <param name="clientId">The clientId.</param>
        /// <param name="clientSecret">The clientSecret.</param>
        /// <param name="tenantId">The tenantId.</param>
        public UserManager(string clientId, string clientSecret, string tenantId)
        {
            this.authenticationProvider = new ConfidentialClientAuthenticationProvider(clientId, clientSecret, AppScopes.Split(';'), tenantId);
            this.graphClient = new GraphServiceClient(this.authenticationProvider);
        }

        /// <inheritdoc/>
        public async Task<UserInfo> GetUserInfo(string userId, bool includeSignInData = false, bool includeGroups = false)
        {
            try
            {
                var user = await this.graphClient.Users[userId].Request().GetAsync().ConfigureAwait(false);
                var userInfo = (UserInfo)user;
                if (includeSignInData)
                {
                    userInfo.LastSignedIn = await this.GetLastSignInAsync(userId).ConfigureAwait(false);
                }

                if (includeGroups)
                {
                    userInfo.Groups = await this.GetGroupsForUser(userId).ConfigureAwait(false);
                }

                return userInfo;
            }
            catch (ServiceException)
            {
                throw new UnknownUserException($"The requested user {userId} is not known in the AD");
            }
        }

        /// <inheritdoc/>
        public async Task DeleteUser(string userId)
        {
            try
            {
                await this.graphClient.Users[userId].Request().DeleteAsync().ConfigureAwait(false);
            }
            catch (ServiceException)
            {
                throw new UnknownUserException($"The requested user {userId} is not known in the AD");
            }
        }

        /// <inheritdoc/>
        public async Task<(UserInfo user, string InviteRedeemUrl)> InviteUser(
           string displayName,
           string emailAddress,
           string redirectUrl,
           bool letAzureSendRedeemMail = true,
           string messageBody = null,
           string companyName = null,
           string messageLanguage = "nl-NL",
           string firstName = null,
           string lastName = null)
        {
            var message = letAzureSendRedeemMail
                ? new InvitedUserMessageInfo
                {
                    CustomizedMessageBody = messageBody,
                    MessageLanguage = messageLanguage,
                }
                : null;

            var invitation = new Invitation
            {
                InvitedUserDisplayName = displayName,
                InvitedUserEmailAddress = emailAddress,
                InvitedUserMessageInfo = message,
                InviteRedirectUrl = redirectUrl,
                SendInvitationMessage = letAzureSendRedeemMail,
            };
            var sentInvitation = await this.graphClient.Invitations.Request().AddAsync(invitation).ConfigureAwait(false);
            var user = sentInvitation.InvitedUser;

            await this.UpdateUser(user.Id, displayName, firstName, lastName, companyName).ConfigureAwait(false);

            user.UserPrincipalName = emailAddress;
            user.DisplayName = displayName;
            return ((UserInfo)user, sentInvitation.InviteRedeemUrl);
        }

        /// <inheritdoc/>
        public async Task UpdateUser(string userId, string displayName, string firstName = null, string lastName = null, string companyName = null)
        {
            var updateUser = new User();
            updateUser.DisplayName = displayName;
            if (!string.IsNullOrEmpty(companyName))
            {
                updateUser.CompanyName = companyName;
            }

            if (!string.IsNullOrEmpty(firstName))
            {
                updateUser.GivenName = firstName;
            }

            if (!string.IsNullOrEmpty(lastName))
            {
                updateUser.Surname = lastName;
            }

            await this.graphClient.Users[userId].Request().UpdateAsync(updateUser).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<(bool, string)> IsInvited(string emailAddress)
        {
            var url = this.graphClient.Users.AppendSegmentToRequestUrl($"?$filter=Mail eq '{emailAddress}'");
            var client = new GraphServiceUsersCollectionRequestBuilder(url, this.graphClient);
            var users = await client.Request().GetAsync().ConfigureAwait(false);
            if (users.Count > 0)
            {
                return (true, users.First().Id);
            }
            return (false, null);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<UserInfo>> GetAllUsers(bool includeSignInData = false)
        {
            var users = new List<UserInfo>();
            var userList = await this.graphClient.Users.Request().GetAsync().ConfigureAwait(false);
            while (userList != null)
            {
                var page = userList.CurrentPage.ToList();
                foreach (var user in page)
                {
                    var userInfo = (UserInfo)user;
                    if (includeSignInData)
                    {
                        userInfo.LastSignedIn = await this.GetLastSignInAsync(user.Id).ConfigureAwait(false);
                    }

                    users.Add(userInfo);
                }

                userList = userList.NextPageRequest != null ? await userList.NextPageRequest.GetAsync().ConfigureAwait(false) : null;
            }

            return users;
        }

        /// <inheritdoc/>
        public async Task<List<GroupInfo>> GetAllGroups()
        {
            var groups = new List<GroupInfo>();
            var groupList = await this.graphClient.Groups.Request().GetAsync().ConfigureAwait(false);
            while (groupList != null)
            {
                var page = groupList.CurrentPage.ToList();
                page.ForEach(group => groups.Add((GroupInfo)group));
                groupList = groupList.NextPageRequest != null ? await groupList.NextPageRequest.GetAsync().ConfigureAwait(false) : null;
            }

            return groups;
        }

        /// <inheritdoc/>
        public async Task<GroupInfo> AddGroup(string name, string parentGroupId = null)
        {
            var group = new Group
            {
                DisplayName = name,
                MailEnabled = false,
                MailNickname = name,
                SecurityEnabled = true,
            };
            var createdGroup = (GroupInfo)await this.graphClient.Groups.Request().AddAsync(group).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(parentGroupId))
            {
                await this.graphClient.Groups[createdGroup.Id].MemberOf.References.Request().AddAsync(new DirectoryObject { Id = parentGroupId }).ConfigureAwait(false);
            }

            return createdGroup;
        }

        /// <inheritdoc/>
        public async Task AddUserToGroup(string groupId, string userId)
        {
            await this.graphClient.Groups[groupId].Members.References.Request().AddAsync(new User { Id = userId }).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<List<GroupInfo>> GetGroupsForUser(string userId)
        {
            var groupIds = new List<string>();
            var groupList = await this.graphClient.Users[userId].GetMemberGroups(true).Request().PostAsync().ConfigureAwait(false);
            while (groupList != null)
            {
                var page = groupList.CurrentPage.ToList();
                page.ForEach(groupIds.Add);
                groupList = groupList.NextPageRequest != null ? await groupList.NextPageRequest.PostAsync().ConfigureAwait(false) : null;
            }

            var groups = await this.GetAllGroups().ConfigureAwait(false);
            return groups.Where(g => groupIds.Contains(g.Id)).ToList();
        }

        /// <inheritdoc/>
        public async Task RemoveUserFromGroup(string groupId, string userId)
        {
            await this.graphClient.Groups[groupId].Members[userId].Reference.Request().DeleteAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<List<UserInfo>> GetAllUsersInGroup(string groupName)
        {
            var result = new List<UserInfo>();
            var groups = await this.graphClient.Groups.Request().Filter($"DisplayName eq '{groupName}'").GetAsync();
            var members = await this.graphClient.Groups[groups.First().Id].Members.Request().GetAsync();

            while (members != null)
            {
                var page = members.CurrentPage.ToList();

                foreach (var member in page)
                {
                    if (member is User)
                    {
                        var user = (UserInfo)member;
                        user.Username = ((User)member).Mail;
                        var x = ((User)member).CompanyName;
                        user.Groups = await this.GetGroupsForUser(user.Id).ConfigureAwait(false);
                        result.Add(user);
                    }
                }

                members = members.NextPageRequest != null ? await members.NextPageRequest.GetAsync().ConfigureAwait(false) : null;
            }

            return result;
        }

        private async Task<DateTimeOffset?> GetLastSignInAsync(string userId)
        {
            var url = this.graphClient.AuditLogs.SignIns.AppendSegmentToRequestUrl($"?$filter=userId eq '{userId}'&$top=1");
            var client = new AuditLogRootSignInsCollectionRequestBuilder(url, this.graphClient);
            var signins = await client.Request().GetAsync().ConfigureAwait(false);
            var lastSignin = signins.FirstOrDefault()?.CreatedDateTime;
            return lastSignin;
        }
    }
}

// <copyright file="UserManagerMock.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Luminis.AzureActiveDirectory.Exceptions;
    using Luminis.AzureActiveDirectory.Models;
    using Microsoft.Graph;

    /// <summary>
    /// A user manager mock for testing.
    /// </summary>
    public class UserManagerMock : IUserManager
    {
        private readonly string subDomain;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserManagerMock"/> class.
        /// Gets a user manager mock.
        /// </summary>
        public UserManagerMock(string subDomain = null)
        {
            this.Groups = new List<GroupInfo>();
            this.Users = new List<UserInfo>();
            this.UsersInGroup = new Dictionary<string, List<string>>();
            this.InvitedUsers = new List<Tuple<UserInfo, string>>();
            this.subDomain = subDomain;
        }

        /// <summary>
        /// Gets groups.
        /// </summary>
        public List<GroupInfo> Groups { get; }

        /// <summary>
        /// Gets users.
        /// </summary>
        public List<UserInfo> Users { get; }

        /// <summary>
        /// Gets users in groups.
        /// </summary>
        public Dictionary<string, List<string>> UsersInGroup { get; }

        /// <summary>
        /// Gets or sets invite useres.
        /// </summary>
        public List<Tuple<UserInfo, string>> InvitedUsers { get; set; }

        /// <summary>
        /// Preconfigures a new user.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="displayName">Display name of the user.</param>
        public void PreConfigureUser(string userName, string displayName)
        {
            this.Users.Add(new UserInfo { DisplayName = displayName, UserPrincipalName = userName, Id = TestFactory.StringToGUID(userName) });
        }

        /// <summary>
        /// Preconfigures a new group.
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        public void PreConfigureGroup(string groupName)
        {
            this.Groups.Add(new GroupInfo { Name = groupName, Id = TestFactory.StringToGUID(groupName) });
        }

        /// <summary>
        /// Preconfigures a group of users.
        /// </summary>
        /// <param name="userName">Name of the user.</param>
        /// <param name="groupName">Name of the group.</param>
        public void PreConfigureGroupOfUser(string userName, string groupName)
        {
            this.AddUserToGroup(TestFactory.StringToGUID(groupName), TestFactory.StringToGUID(userName));
        }

        /// <inheritdoc />
        public Task<GroupInfo> AddGroup(string name, string parentGroupId = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task AddUserToGroup(string groupId, string userId)
        {
            if (this.Users.Count == 0)
            {
                throw new ArgumentException($"no users were preconfigured. Call 'PreConfigureUserInMock' to do so.");
            }

            if (!this.Users.Any(u => u.Id.Equals(userId)))
            {
                throw new ArgumentException($"user with id {userId} was not preconfigured. Call 'PreConfigureUserInMock' to do so.");
            }

            var user = this.Users.First(u => u.Id.Equals(userId));

            if (!this.UsersInGroup.ContainsKey(groupId))
            {
                this.UsersInGroup.Add(groupId, new List<string>() { userId });
            }
            else
            {
                var group = this.UsersInGroup.First(g => g.Key.Equals(groupId));

                if (!group.Value.Any(id => id.Equals(userId)))
                {
                    group.Value.Add(userId);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteUser(string userId)
        {
            if (!this.Users.Any(u => u.Id.Equals(userId, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new UnknownUserException();
            }

            this.Users.RemoveAll(u => u.Id.Equals(userId, StringComparison.InvariantCultureIgnoreCase));
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<List<GroupInfo>> GetAllGroups()
        {
            return Task.FromResult(this.Groups);
        }

        /// <inheritdoc />
        public Task<IEnumerable<UserInfo>> GetAllUsers(bool includeSignInData = false)
        {
            return Task.FromResult(this.Users.AsEnumerable());
        }

        /// <inheritdoc />
        public Task<List<UserInfo>> GetAllUsersInGroup(string group)
        {
            if (this.Groups.Count == 0)
            {
                throw new ArgumentException($"group {group} was not preconfigured. Call 'PreConfigureGroupInMock' to do so.");
            }

            var groupId = this.Groups.First(g => g.Name.Equals(group, StringComparison.InvariantCultureIgnoreCase)).Id;
            if (this.UsersInGroup.Count == 0)
            {
                throw new ArgumentException($"no users were preconfigured to be in any group. Call 'PreConfigureGroupOfUser' to do so.");
            }

            var groupUserId = this.UsersInGroup.First(g => g.Key.Equals(groupId));
            var allUsersInGroup = new List<UserInfo>();
            foreach (var user in groupUserId.Value)
            {
                var storedUser = this.Users.First(u => u.Id.Equals(user));
                var groups = this.GetGroupsForUser(user).ConfigureAwait(false).GetAwaiter().GetResult();
                allUsersInGroup.Add(new UserInfo { Id = storedUser.Id, DisplayName = storedUser.DisplayName, FirstName = storedUser.FirstName, LastSignedIn = storedUser.LastSignedIn, LastName = storedUser.LastName, UserPrincipalName = storedUser.UserPrincipalName, Groups = groups });
            }

            return Task.FromResult(allUsersInGroup);
        }

        /// <inheritdoc />
        public Task<List<GroupInfo>> GetGroupsForUser(string userId)
        {
            var groups = new List<GroupInfo>();
            foreach (var userGroup in this.UsersInGroup)
            {
                if (userGroup.Value.Contains(userId))
                {
                    var groupName = this.Groups.First(g => g.Id.Equals(userGroup.Key)).Name;
                    var groupInfo = new GroupInfo() { Id = userGroup.Key, Name = groupName };
                    groups.Add(groupInfo);
                }
            }

            return Task.FromResult(groups);
        }

        /// <inheritdoc />
        public async Task<UserInfo> GetUserInfo(string userId, bool includeSignInData = false, bool includeGroups = false)
        {
            var user = this.Users.First(u => u.Id.Equals(userId));
            user.Groups = await this.GetGroupsForUser(userId);
            return user;
        }

        /// <inheritdoc />
        public Task<(UserInfo User, string InviteRedeemUrl)> InviteUser(
            string displayName,
            string emailAddress,
            string redirectUrl,
            bool letAzureSendRedeemMail = true,
            string messageBody = null,
            string companyName = null,
            string messageLanguage = "nl-NL",
            string givenName = null,
            string surname = null)
        {
            var user = new UserInfo
            {
                DisplayName = displayName,
                FirstName = givenName,
                LastName = surname,
                UserPrincipalName = emailAddress,
                Id = TestFactory.StringToGUID(emailAddress),
            };
            var t = Tuple.Create(user, redirectUrl);
            this.InvitedUsers.Add(t);
            this.Users.Add(user);
            return Task.FromResult((user, redirectUrl));
        }

        /// <inheritdoc />
        public Task<(bool IsInvited, string UserId)> IsInvited(string emailAddress)
        {
            var invitedUser = this.InvitedUsers.FirstOrDefault(u => u.Item1.UserPrincipalName.Equals(emailAddress, StringComparison.InvariantCultureIgnoreCase)).Item1;
            if (invitedUser != null)
            {
                return Task.FromResult((true, invitedUser.Id));
            }

            return Task.FromResult((true, invitedUser.Id));
        }

        /// <inheritdoc />
        public Task RemoveUserFromGroup(string groupId, string userId)
        {
            var user = this.Users.First(u => u.Id.Equals(userId));

            if (this.UsersInGroup.ContainsKey(groupId))
            {
                if (this.UsersInGroup[groupId].Exists(u => u.Equals(userId)))
                {
                    this.UsersInGroup[groupId].Remove(user.Id);
                    return Task.CompletedTask;
                }

                throw new ArgumentException("user does not exists");
            }

            throw new ArgumentException("group does not exists");
        }

        /// <inheritdoc />
        public Task UpdateUser(string userId, string displayName, string firstName = null, string lastName = null, string companyName = null)
        {
            var user = this.Users.First(u => u.Id.Equals(userId));
            user.DisplayName = displayName;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task SetUserExtensionClaim(string userId, string claimKey, string value)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<string> GetUserExtensionClaim(string userId, string claimKey)
        {
            return Task.FromResult(System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["TestKey"] = "Testvalue",
                ["Testkey2"] = "Testvalue 2",
            }));
        }

        /// <inheritdoc />
        public Task<(bool Exists, string UserId)> DoesIssuedUserExist(string emailAddress, string issuer)
        {
            var userInvitationInfo = this.Users.FirstOrDefault(u => u.B2cSignInEmail.Equals(emailAddress, StringComparison.InvariantCultureIgnoreCase));
            if (userInvitationInfo != null)
            {
                return Task.FromResult((true, userInvitationInfo.Id));
            }

            return Task.FromResult((false, (string)null));
        }

        /// <inheritdoc />
        public Task SetUserClaim(string userId, User updatedUser)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAvailableExtensionClaims(string b2cExtensionsAppObjectId)
        {
            return await Task.FromResult(new[]
            {
                $"extension_{b2cExtensionsAppObjectId}_Project_{this.subDomain}_1",
                $"extension_{b2cExtensionsAppObjectId}_Project_{this.subDomain}_2",
                $"extension_{b2cExtensionsAppObjectId}_Project_{this.subDomain}_3",
            });
        }

        /// <inheritdoc />
        public Task<(string Name, string Domain)> GetTenantInformation()
        {
            return Task.FromResult((string.Empty, string.Empty));
        }

        /// <inheritdoc />
        public Task<(bool Exists, string UserId)> DoesInvitedUserExistWithInvitationState(string emailAddress, string issuer, string invitationState)
        {
            var userInvitationInfo = this.Users.FirstOrDefault(u => u.B2cSignInEmail.Equals(emailAddress, StringComparison.InvariantCultureIgnoreCase) && u.ExternalUserState == invitationState);
            if (userInvitationInfo != null)
            {
                return Task.FromResult((true, userInvitationInfo.Id));
            }

            return Task.FromResult((false, (string)null));
        }
    }
}

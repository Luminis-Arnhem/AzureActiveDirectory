// <copyright file="UserManager.cs" company="Luminis BV">
// Copyright (c) Luminis BV. All rights reserved.
// </copyright>

namespace Luminis.AzureActiveDirectory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Luminis.AzureActiveDirectory.AuthenticationProviders;
    using Luminis.AzureActiveDirectory.Exceptions;
    using Luminis.AzureActiveDirectory.Models;
    using Microsoft.Graph;
    using Microsoft.Graph.Models;
    using Microsoft.Graph.Users.Item.GetMemberGroups;

    /// <summary>
    /// A user manager for Azure Active Directory.
    /// </summary>
    public class UserManager : IUserManager
    {
        // See also: https://docs.microsoft.com/en-us/azure/active-directory-b2c/manage-user-accounts-graph-api?tabs=applications
        // Do not forget to enter 'Grant admin consent for Standaardmap: https://docs.microsoft.com/en-us/graph/auth-v2-service
        private const string AppScopes = "https://graph.microsoft.com/.default";
        private readonly GraphServiceClient graphClient;
        private readonly string tenantId;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserManager"/> class.
        /// The arguments can be found in your application registration in Azure Active Directory.
        /// </summary>
        /// <param name="clientId">The clientId.</param>
        /// <param name="clientSecret">The clientSecret.</param>
        /// <param name="tenantId">The tenantId.</param>
        public UserManager(string clientId, string clientSecret, string tenantId)
        {
            var authenticationProvider = new ConfidentialClientAuthenticationProvider(clientId, clientSecret, AppScopes.Split(';'), tenantId);
            this.graphClient = new GraphServiceClient(authenticationProvider);
            this.tenantId = tenantId;
        }

        /// <inheritdoc/>
        public async Task<UserInfo> GetUserInfo(string userId, bool includeSignInData = false, bool includeGroups = false)
        {
            try
            {
                var user = await this.graphClient.Users[userId]
                    .GetAsync(config =>
                    {
                        config.QueryParameters.Select = new[]
                        {
                           "businessPhones", "displayName", "givenName", "id", "jobTitle", "mail", "otherMails",
                           "mobilePhone", "officeLocation", "preferredLanguage", "surname", "userPrincipalName", "identities",
                        };
                    }).ConfigureAwait(false);

                var userInfo = (UserInfo)user;
                if (includeSignInData)
                {
                    userInfo.LastSignedIn = await this.GetLastSignIn(userId).ConfigureAwait(false);
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
                await this.graphClient.Users[userId].DeleteAsync().ConfigureAwait(false);
            }
            catch (ServiceException)
            {
                throw new UnknownUserException($"The requested user {userId} is not known in the AD");
            }
        }

        /// <inheritdoc/>
        public async Task<(UserInfo User, string InviteRedeemUrl)> InviteUser(
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
            var sentInvitation = await this.graphClient.Invitations.PostAsync(invitation).ConfigureAwait(false);
            var user = sentInvitation.InvitedUser;

            await this.UpdateUser(user.Id, displayName, givenName, surname, companyName).ConfigureAwait(false);

            user.UserPrincipalName = emailAddress;
            user.DisplayName = displayName;
            return ((UserInfo)user, sentInvitation.InviteRedeemUrl);
        }

        /// <inheritdoc/>
        public async Task UpdateUser(string userId, string displayName, string firstName = null, string lastName = null, string companyName = null)
        {
            var updateUser = new User
            {
                DisplayName = displayName,
            };

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

            await this.graphClient.Users[userId].PatchAsync(updateUser).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<(bool, string)> IsInvited(string emailAddress)
        {
            var users = await this.graphClient.Users.GetAsync(config =>
            {
                config.QueryParameters.Filter = $"mail eq '{emailAddress}'";
            });

            if (users.Value.Count > 0)
            {
                return (true, users.Value[0].Id);
            }

            return (false, null);
        }

        /// <inheritdoc/>
        public async Task<(bool Exists, string UserId)> DoesIssuedUserExist(string emailAddress, string issuer)
        {
            var filter = $"mail eq '{emailAddress}' or otherMails/any(id:id eq '{emailAddress}')";
            return await this.DoesUserExist(filter, issuer);
        }

        /// <inheritdoc/>
        public async Task<(bool Exists, string UserId)> DoesInvitedUserExistWithInvitationState(string emailAddress, string issuer, string invitationState)
        {
            var filter = $"(mail eq '{emailAddress}' or otherMails/any(id:id eq '{emailAddress}')) and userType eq 'Guest' and externalUserState eq '{invitationState}'";
            return await this.DoesUserExist(filter, issuer);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<UserInfo>> GetAllUsers(bool includeSignInData = false)
        {
            var users = new List<UserInfo>();
            var userList = await this.graphClient.Users.GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "businessPhones", "displayName", "givenName", "id", "jobTitle", "mail", "otherMails",
                    "mobilePhone", "officeLocation", "preferredLanguage", "surname", "userPrincipalName", "identities",
                };
            }).ConfigureAwait(false);

            foreach (var user in userList.Value)
            {
                var userInfo = (UserInfo)user;
                if (includeSignInData)
                {
                    userInfo.LastSignedIn = await this.GetLastSignIn(user.Id).ConfigureAwait(false);
                }

                users.Add(userInfo);
            }

            return users;
        }

        /// <inheritdoc/>
        public async Task<List<GroupInfo>> GetAllGroups()
        {
            var groups = new List<GroupInfo>();
            var groupList = await this.graphClient.Groups.GetAsync().ConfigureAwait(false);

            groupList.Value?.ForEach(group => groups.Add((GroupInfo)group));

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
            var createdGroup = (GroupInfo)await this.graphClient.Groups.PostAsync(group).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(parentGroupId))
            {
                await this.graphClient.Groups[parentGroupId].Members.Ref.PostAsync(new ReferenceCreate { OdataId = $"https://graph.microsoft.com/v1.0/groups/{createdGroup.Id}" }).ConfigureAwait(false);
            }

            return createdGroup;
        }

        /// <inheritdoc/>
        public async Task AddUserToGroup(string groupId, string userId)
        {
            await this.graphClient.Groups[groupId].Members.Ref.PostAsync(new ReferenceCreate { OdataId = $"https://graph.microsoft.com/v1.0/users/{userId}" }).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<List<GroupInfo>> GetGroupsForUser(string userId)
        {
            var groupIds = new List<string>();
            var requestBody = new GetMemberGroupsPostRequestBody
            {
                SecurityEnabledOnly = false, // Set to true if you only want security-enabled groups
            };

            var groupList = await this.graphClient.Users[userId].GetMemberGroups.PostAsGetMemberGroupsPostResponseAsync(requestBody).ConfigureAwait(false);
            groupList.Value?.ForEach(groupIds.Add);

            var groups = await this.GetAllGroups().ConfigureAwait(false);
            return groups.Where(g => groupIds.Contains(g.Id)).ToList();
        }

        /// <inheritdoc/>
        public async Task RemoveUserFromGroup(string groupId, string userId)
        {
            await this.graphClient.Groups[groupId].Members[userId].Ref.DeleteAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves all users in a specified group.
        /// </summary>
        /// <param name="group">The name of the group.</param>
        /// <returns>A list of <see cref="UserInfo"/> objects representing the users in the group.</returns>
        public async Task<List<UserInfo>> GetAllUsersInGroup(string group)
        {
            var result = new List<UserInfo>();

            var groups = await this.graphClient.Groups.GetAsync(config =>
            {
                config.QueryParameters.Filter = $"displayName eq '{group}'";
            }).ConfigureAwait(false);

            var members = await this.graphClient.Groups[groups.Value[0].Id].Members
                .GetAsync(config =>
                {
                    config.QueryParameters.Select = new[]
                    {
                        "businessPhones", "displayName", "givenName", "id", "jobTitle", "mail", "otherMails",
                        "mobilePhone", "officeLocation", "preferredLanguage", "surname", "userPrincipalName", "identities",
                    };
                }).ConfigureAwait(false);

            foreach (var member in members.Value.Where(member => member is User))
            {
                var user = (UserInfo)member;
                user.UserPrincipalName = ((User)member).Identities.Any(x => x.SignInType == "emailAddress") ? ((User)member).Mail : ((User)member).OtherMails[0];
                user.Groups = await this.GetGroupsForUser(user.Id).ConfigureAwait(false);
                result.Add(user);
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task SetUserClaim(string userId, User updatedUser)
        {
            await this.graphClient.Users[userId].PatchAsync(updatedUser);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetAvailableExtensionClaims(string b2cExtensionsAppObjectId)
        {
            var availableExtensionProperties = await this.graphClient.Applications[b2cExtensionsAppObjectId].ExtensionProperties.GetAsync();

            return availableExtensionProperties == null || !availableExtensionProperties.Value.Any() ?
                Enumerable.Empty<string>() :
                availableExtensionProperties.Value.Where(x => x.Name.StartsWith("extension_", StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Name);
        }

        /// <inheritdoc/>
        public async Task SetUserExtensionClaim(string userId, string claimKey, string value)
        {
            await this.graphClient.Users[userId].PatchAsync(new User
            {
                AdditionalData = new Dictionary<string, object>
                {
                    [claimKey] = value,
                },
            });
        }

        /// <inheritdoc/>
        public async Task<string> GetUserExtensionClaim(string userId, string claimKey)
        {
            try
            {
                var user = await this.graphClient.Users[userId]
                    .GetAsync(config =>
                    {
                        config.QueryParameters.Select = new[] { claimKey };
                    }).ConfigureAwait(false);

                if (user.AdditionalData.TryGetValue(claimKey, out var value))
                {
                    return value switch
                    {
                        JsonElement { ValueKind: JsonValueKind.String } => value.ToString(),
                        string _ => value.ToString(),
                        _ => null
                    };
                }

                return null;
            }
            catch (ServiceException)
            {
                throw new UnknownUserException($"The requested user {userId} is not known in the AD");
            }
        }

        /// <inheritdoc/>
        public async Task<(string Name, string Domain)> GetTenantInformation()
        {
            var tenants = await this.graphClient.Organization.GetAsync().ConfigureAwait(false);
            var information = tenants.Value.FirstOrDefault(t => t.Id == this.tenantId);
            return (information.DisplayName, information.VerifiedDomains.FirstOrDefault()?.Name);
        }

        private async Task<DateTimeOffset?> GetLastSignIn(string userId)
        {
            var signins = await this.graphClient.AuditLogs.SignIns.GetAsync(config =>
            {
                config.QueryParameters.Filter = $"?$filter=userId eq '{userId}'&$top=1";
            }).ConfigureAwait(false);
            var lastSignin = signins.Value.FirstOrDefault()?.CreatedDateTime;
            return lastSignin;
        }

        private async Task<(bool Exists, string UserId)> DoesUserExist(string filter, string issuer)
        {
            var userList = await this.graphClient.Users
                .GetAsync(config =>
                {
                    config.QueryParameters.Filter = filter;
                    config.QueryParameters.Select = new[] { "id", "identities" };
                }).ConfigureAwait(false);

            var userId = userList.Value.FirstOrDefault(x => x.Identities.Any(y => y.Issuer == issuer))?.Id;

            return (userId != null, userId);
        }
    }
}

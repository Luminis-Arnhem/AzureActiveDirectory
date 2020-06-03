using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Luminis.AzureActiveDirectory;
using Luminis.AzureActiveDirectory.Exceptions;
using Luminis.AzureActiveDirectory.Models;

namespace Luminis.AzureActiveDirectory.Test
{
    public class UserManagerMock : IUserManager
    {

        public UserManagerMock()
        {
            Groups = new List<GroupInfo>();
            Users = new List<UserInfo>();
            UsersInGroup = new Dictionary<string, List<string>>();
            InvitedUsers = new List<Tuple<UserInfo, string>>();
        }

        public List<GroupInfo> Groups { get; }
        public List<UserInfo> Users { get; }
        public Dictionary<string, List<string>> UsersInGroup { get; }
        public List<Tuple<UserInfo, string>> InvitedUsers { get; set; }

        public void PreConfigureUser(string userName, string displayName)
        {
            Users.Add(new UserInfo { DisplayName = displayName, Username = userName, Id = TestFactory.StringToGUID(userName) });
        }

        public void PreConfigureGroup(string groupName)
        {
            Groups.Add(new GroupInfo { Name = groupName, Id = TestFactory.StringToGUID(groupName) });
        }

        public void PreConfigureGroupOfUser(string userName, string groupName)
        {
            this.AddUserToGroup(TestFactory.StringToGUID(groupName), TestFactory.StringToGUID(userName));
        }

        public Task<GroupInfo> AddGroup(string name, string parentGroupId = null)
        {
            throw new NotImplementedException();
        }

        public Task AddUserToGroup(string groupId, string userId)
        {
            if (Users.Count == 0)
            {
                throw new ArgumentException($"no users were preconfigured. Call 'PreConfigureUserInMock' to do so.");
            }

            if (!Users.Any(u => u.Id.Equals(userId)))
            {
                throw new ArgumentException($"user with id {userId} was not preconfigured. Call 'PreConfigureUserInMock' to do so.");
            }
            var user = Users.First(u => u.Id.Equals(userId));

            if (!UsersInGroup.ContainsKey(groupId))
            {
                UsersInGroup.Add(groupId, new List<string>() { userId });
            }
            else
            {
                var group = UsersInGroup.First(g => g.Key.Equals(groupId));

                if (!group.Value.Any(id => id.Equals(userId))) ;
                {
                    group.Value.Add(userId);
                }

            }
            return Task.CompletedTask;
        }

        public Task DeleteUser(string userId)
        {
            if (!Users.Any(u => u.Id.Equals(userId, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new UnknownUserException();
            }

            Users.RemoveAll(u => u.Id.Equals(userId, StringComparison.InvariantCultureIgnoreCase));
            return Task.CompletedTask;
        }

        public Task<List<GroupInfo>> GetAllGroups()
        {
            return Task.FromResult(Groups);
        }

        public Task<IEnumerable<UserInfo>> GetAllUsers(bool includeSignInData = false)
        {
            return Task.FromResult(Users.AsEnumerable());
        }

        public Task<List<UserInfo>> GetAllUsersInGroup(string group)
        {
            if (Groups.Count == 0)
            {
                throw new ArgumentException($"group {group} was not preconfigured. Call 'PreConfigureGroupInMock' to do so.");
            }
            var groupId = Groups.First(g => g.Name.Equals(group, StringComparison.InvariantCultureIgnoreCase)).Id;
            if (UsersInGroup.Count == 0)
            {
                throw new ArgumentException($"no users were preconfigured to be in any group. Call 'PreConfigureGroupOfUser' to do so.");
            }
            var groupUserId = UsersInGroup.First(g => g.Key.Equals(groupId));
            var allUsersInGroup = new List<UserInfo>();
            foreach (var user in groupUserId.Value)
            {
                var storedUser = Users.First(u => u.Id.Equals(user));
                var groups = GetGroupsForUser(user).ConfigureAwait(false).GetAwaiter().GetResult();
                allUsersInGroup.Add(new UserInfo { Id = storedUser.Id, DisplayName = storedUser.DisplayName, FirsName = storedUser.FirsName, LastSignedIn = storedUser.LastSignedIn, LastName = storedUser.LastName, Username = storedUser.Username, Groups = groups });
            }
            return Task.FromResult(allUsersInGroup);
        }


        public Task<List<GroupInfo>> GetGroupsForUser(string userId)
        {
            var groups = new List<GroupInfo>();
            foreach (var userGroup in UsersInGroup)
            {
                if (userGroup.Value.Contains(userId))
                {
                    var groupName = Groups.First(g => g.Id.Equals(userGroup.Key)).Name;
                    var groupInfo = new GroupInfo() { Id = userGroup.Key, Name = groupName };
                    groups.Add(groupInfo);
                }

            }
            return Task.FromResult(groups);
        }

        public async Task<UserInfo> GetUserInfo(string userId, bool includeSignInData = false, bool includeGroups = false)
        {
            var user = Users.First(u => u.Id.Equals(userId));
            user.Groups = await GetGroupsForUser(userId);
            return user;
        }


        public Task<(UserInfo user, string InviteRedeemUrl)> InviteUser(
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
            var user = new UserInfo();
            user.DisplayName = displayName;
            user.FirsName = givenName;
            user.LastName = surname;
            user.Username = emailAddress;
            user.Id = TestFactory.StringToGUID(emailAddress);
            var t = Tuple.Create(user, redirectUrl);
            InvitedUsers.Add(t);
            Users.Add(user);
            return Task.FromResult((user, redirectUrl));
        }

        public Task<(bool, string)> IsInvited(string emailAddress)
        {
            var invitedUser = InvitedUsers.FirstOrDefault(u => u.Item1.Username.Equals(emailAddress, StringComparison.InvariantCultureIgnoreCase)).Item1;
            if (invitedUser != null)
            {
                return Task.FromResult((true, invitedUser.Id));
            }
            return Task.FromResult((true, invitedUser.Id));

        }

        public Task RemoveUserFromGroup(string groupId, string userId)
        {
            var user = Users.First(u => u.Id.Equals(userId));

            if (UsersInGroup.ContainsKey(groupId))
            {
                if (UsersInGroup[groupId].Exists(u => u.Equals(userId)))
                {
                    UsersInGroup[groupId].Remove(user.Id);
                    return Task.CompletedTask;
                }
                throw new ArgumentException("user does not exists");
            }
            throw new ArgumentException("group does not exists");
        }

        public Task UpdateUser(string userId, string displayName, string firstName = null, string lastName = null, string companyName = null)
        {
            var user = Users.First(u => u.Id.Equals(userId));
            user.DisplayName = displayName;
            return Task.CompletedTask;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using SharepointMessenger.Models;
using SharepointMessenger.Repos;
using Microsoft.SharePoint.Utilities;

namespace SharepointMessenger.Repositories
{
    public interface IGroupRepository
    {
        Group GetGroup(string name);
        SPRoleAssignment CreateRoleAssignment(string group);
    }

    public class GroupRepository :
        IGroupRepository
    {
        public Group GetGroup(string name)
        {
            var group = SPContext.Current.Web.SiteGroups[name];
            return new Group()
            {
                ID = group.ID,
                Name = group.Name,
                Username = group.LoginName
            };
        }
        public SPRoleAssignment CreateRoleAssignment(string groupName)
        {
            var group = SPContext.Current.Web.SiteGroups[groupName];
            SPRoleAssignment ass = new SPRoleAssignment(group);
            SPRoleDefinition def = SPContext.Current.Web.RoleDefinitions[Language.SMUPermissionName];
            ass.RoleDefinitionBindings.Add(def);
            return ass;
        }
    }

    public interface IContactRepository
    {
        Contact GetByID(Group group, int id);
        Contact[] GetAllFromGroup(Group group);
        SPRoleAssignment CreateRoleAssignment(string groupName, int userID);
    }

    public class ContactRepository :
        IContactRepository
    {
        public SPRoleAssignment CreateRoleAssignment(string groupName, int userID)
        {
            SPRoleAssignment ass = null;
            var spGroup = SPContext.Current.Web.SiteGroups[groupName];
            var spUser = spGroup.Users.Cast<SPUser>().FirstOrDefault(u => u.ID == userID);
            if (spUser != null)
            {
                ass = new SPRoleAssignment(spUser);
                SPRoleDefinition def = SPContext.Current.Web.RoleDefinitions[Language.SMUPermissionName];
                ass.RoleDefinitionBindings.Add(def);
            }
            return ass;
        }

        public Contact GetByID(Group group, int id)
        {
            var spGroup = SPContext.Current.Web.SiteGroups[group.Name];
            Contact result = null;
            var spUser = spGroup.Users.Cast<SPUser>().FirstOrDefault(u => u.ID == id);
            if (spUser != null)
                result = new Contact() { ID = spUser.ID, Name = spUser.Name, Username = spUser.LoginName };
            return result;
        }

        public Contact[] GetAllFromGroup(Group group)
        {
            var spGroup = SPContext.Current.Web.SiteGroups[group.Name];
            var currentId = SPContext.Current.Web.CurrentUser.ID;
            var list = spGroup.Users.Cast<SPUser>().Where(u => u.ID != currentId).Select(u =>
                new Contact() { ID = u.ID, Name = u.Name, Username = u.LoginName }).ToArray();

            foreach (Contact user in list)
            {
                var item = SPContext.Current.Web.SiteUserInfoList.Items.GetItemById(user.ID);
                if (item["Picture"] == null)
                    user.ImageUrl = "/_layouts/images/person.gif";
                else
                    user.ImageUrl = item["Picture"].ToString().Replace(",", "");
            }
            return list;
        }
    }

    public interface IChatMessageRepository
    {
        ChatMessage GetByID(int id);
        void Create(ChatMessage message);
        void DeleteByID(int id);
        void Delete(ChatMessage message);
        ChatMessage[] GetUnReadByUserIDAndSenderID(int userID, int senderID);
        void SetChatMessagesRead(int[] ids);
        ChatMessage[] GetPendingMessageByUser(int id);
    }

    public class ChatMessageRepository :
        IChatMessageRepository
    {
        private string ListName { get { return Language.SMUListName; } }

        public ChatMessage GetByID(int id)
        {
            SPList list = SPContext.Current.Web.Lists[ListName];
            SPItem item = list.Items.GetItemById(id);
            return new ChatMessage()
            {
                ID = item.ID,
                Title = item[ChatMessageFields.Title].ToString(),
                Message = item[ChatMessageFields.Message].ToString(),
            };
        }

        public void Create(ChatMessage message)
        {
            IContactRepository repo = new ContactRepository();
            var id = SPContext.Current.Web.CurrentUser.ID;
            SPList list = SPContext.Current.Web.Lists[ListName];
            List<int> ids = message.Receivers.Select(r => r.ID).ToList();
            ids.Add(id);
            SPListItem conversation = Config.GetConversationFolder(list, ids.ToArray());
            if (conversation == null)
                conversation = Config.CreateConversationFolder(SPContext.Current.Web, Guid.NewGuid().ToString().Replace("-", ""), ids.Select(i => repo.CreateRoleAssignment(Language.SMUGroupName, i)).ToArray());
            SPItem item = list.Items.Add(conversation.Folder.ServerRelativeUrl, SPFileSystemObjectType.File, message.Title);
            item[ChatMessageFields.Title] = message.Title;
            item[ChatMessageFields.Message] = message.Message;
            SPFieldLookupValueCollection receivers = new SPFieldLookupValueCollection();
            foreach (Contact c in message.Receivers)
                receivers.Add(new SPFieldLookupValue(c.ID, null));
            item[ChatMessageFields.Receivers] = receivers;
            item.Update();
        }

        public void DeleteByID(int id)
        {
            SPList list = SPContext.Current.Web.Lists[ListName];
            list.Items.DeleteItemById(id);
        }

        public void Delete(ChatMessage message)
        {
            DeleteByID(message.ID);
        }

        public void SetChatMessagesRead(int[] ids)
        {
            SPList list = SPContext.Current.Web.Lists[ListName];
            StringBuilder methodBuilder = new StringBuilder();
            string batch = string.Empty;
            string batchFormat = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
              "<ows:Batch OnError=\"Return\">{0}</ows:Batch>";
            string methodFormat = "<Method ID=\"Item{0}\">" +
             "<SetList>{1}</SetList>" +
             "<SetVar Name=\"Cmd\">Save</SetVar>" +
             "<SetVar Name=\"ID\">{2}</SetVar>" +
             "<SetVar Name=\"urn:schemas-microsoft-com:office:office#smIsRead\">{3}</SetVar>" +
             "</Method>";
            foreach (int id in ids)
                methodBuilder.AppendFormat(methodFormat, id, list.ID, id, "TRUE");
            batch = string.Format(batchFormat, methodBuilder.ToString());

            // We have to elevate because we don't want users able to edit other 
            // users messages at all. Each of the users don't have the edit permission
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                using (SPSite site = new SPSite(SPContext.Current.Web.Site.ID))
                {
                    using (SPWeb web = site.OpenWeb(SPContext.Current.Web.ID))
                    {
                        web.ProcessBatchData(batch);
                    }
                }
            });
        }

        public ChatMessage[] GetUnReadByUserIDAndSenderID(int userID, int senderID)
        {
            SPList list = SPContext.Current.Web.Lists[ListName];
            SPQuery query = new SPQuery();
            StringBuilder builder = new StringBuilder();
            builder.Append("<Where><And>");
            builder
                .Append("<And>")
                .AppendFormat("<Contains><FieldRef Name='{0}' LookupId='TRUE' /><Value Type='Integer'>{1}</Value></Contains>", ChatMessageFields.Receivers, userID)
                .AppendFormat("<Eq><FieldRef Name='{0}' LookupId='TRUE' /><Value Type='Integer'>{1}</Value></Eq>", ChatMessageFields.CreatedBy, senderID)
                .Append("</And>");
            builder
                .Append("<Or>")
                .AppendFormat("<Eq><FieldRef Name='{0}' /><Value Type='Boolean'>{1}</Value></Eq>", ChatMessageFields.IsRead, "FALSE")
                .AppendFormat("<IsNull><FieldRef Name='{0}' /></IsNull>", ChatMessageFields.IsRead)
                .Append("</Or>");
            builder.Append("</And></Where>");
            query.Query = builder.ToString();
            query.ViewFields = string.Format(
                "<FieldRef Name='{0}' /><FieldRef Name='{1}' /><FieldRef Name='{2}' /><FieldRef Name='{3}' /><FieldRef Name='{4}' />",
                "ID", ChatMessageFields.Message, ChatMessageFields.Receivers, ChatMessageFields.Created, ChatMessageFields.CreatedBy);
            query.ViewFieldsOnly = true;
            query.DatesInUtc = true;
            query.ViewAttributes = "Scope=\"RecursiveAll\"";
            var items = list.GetItems(query);
            List<ChatMessage> result = new List<ChatMessage>();

            foreach (SPItem item in items)
            {
                var cm = new ChatMessage();
                cm.Created = (DateTime)item[ChatMessageFields.Created];
                var createdBy = item[ChatMessageFields.CreatedBy].ToString().Replace("#", "");
                cm.CreatedBy = new Contact() { ID = Int32.Parse(createdBy.Split(';')[0]), Name = createdBy.Split(';')[1], Username = "" };
                cm.ID = item.ID;
                cm.Message = (item[ChatMessageFields.Message] != null) ? item[ChatMessageFields.Message].ToString() : "";
                SPFieldUserValueCollection receivers = item[ChatMessageFields.Receivers] as SPFieldUserValueCollection;
                cm.Receivers = receivers.Cast<SPFieldUserValue>().Select(l => new Contact() { ID = l.LookupId, Name = l.User.Name, Username = l.User.LoginName }).ToArray();
                result.Add(cm);
            }
            return result.OrderBy(i => i.Created).ToArray();
        }

        public ChatMessage[] GetPendingMessageByUser(int id)
        {
            SPList list = SPContext.Current.Web.Lists[ListName];
            SPQuery query = new SPQuery();
            StringBuilder builder = new StringBuilder();
            builder.Append("<Where><And>");
            builder
                .AppendFormat("<Contains><FieldRef Name='{0}' LookupId='TRUE' /><Value Type='Integer'>{1}</Value></Contains>", ChatMessageFields.Receivers, id);
            builder
                .Append("<Or>")
                .AppendFormat("<Eq><FieldRef Name='{0}' /><Value Type='Boolean'>{1}</Value></Eq>", ChatMessageFields.IsRead, "FALSE")
                .AppendFormat("<IsNull><FieldRef Name='{0}' /></IsNull>", ChatMessageFields.IsRead)
                .Append("</Or>");
            builder.Append("</And></Where>");
            query.Query = builder.ToString();
            query.ViewFields = string.Format(
                "<FieldRef Name='{0}' /><FieldRef Name='{1}' /><FieldRef Name='{2}' /><FieldRef Name='{3}' /><FieldRef Name='{4}' />",
                "ID", ChatMessageFields.Message, ChatMessageFields.Receivers, ChatMessageFields.Created, ChatMessageFields.CreatedBy);
            query.ViewFieldsOnly = true;
            query.ViewAttributes = "Scope=\"RecursiveAll\"";
            query.DatesInUtc = true;
            var items = list.GetItems(query);
            List<ChatMessage> result = new List<ChatMessage>();
            foreach (SPItem item in items)
            {
                var cm = new ChatMessage();
                cm.Created = (DateTime)item[ChatMessageFields.Created];
                var createdBy = item[ChatMessageFields.CreatedBy].ToString().Replace("#", "");
                cm.CreatedBy = new Contact() { ID = Int32.Parse(createdBy.Split(';')[0]), Name = createdBy.Split(';')[1], Username = "" };
                cm.ID = item.ID;
                cm.Message = (item[ChatMessageFields.Message] != null) ? item[ChatMessageFields.Message].ToString() : "";
                SPFieldUserValueCollection receivers = item[ChatMessageFields.Receivers] as SPFieldUserValueCollection;
                cm.Receivers = receivers.Cast<SPFieldUserValue>().Select(l => new Contact() { ID = l.LookupId, Name = l.User.Name, Username = l.User.LoginName }).ToArray();
                result.Add(cm);
            }
            return result.OrderBy(i => i.Created).ToArray();
        }
    }
}

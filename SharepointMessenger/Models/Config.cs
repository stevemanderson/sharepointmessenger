using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace SharepointMessenger.Models
{
    public class Config
    {
        public static SPListItem CreateConversationFolder(SPWeb web, string name, SPRoleAssignment[] assignments)
        {
            SPListItem newFolder = null;
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                using (SPSite site = new SPSite(web.Site.ID))
                {
                    using (SPWeb webelevated = site.OpenWeb(web.ID))
                    {
                        SPList list = GetList(webelevated);
                        if (list.Folders.OfType<SPListItem>().Any(p => p.Name == name)) throw new Exception(String.Format(Language.ConversationAlreadyExists, name));
                        newFolder = list.Folders.Add(list.RootFolder.ServerRelativeUrl, SPFileSystemObjectType.Folder, name);
                        if (null != newFolder)
                        {
                            newFolder["Title"] = name;
                            newFolder.Update();
                            newFolder.BreakRoleInheritance(false);
                            List<int> ids = new List<int>();
                            foreach (SPRoleAssignment ass in assignments)
                            {
                                newFolder.RoleAssignments.Add(ass);
                                ids.Add(ass.Member.ID);
                            }
                            var stringIDs = ids.OrderBy(p => p).Select<int, string>(p => p.ToString()).ToArray();
                            string value = String.Join(",", stringIDs);
                            newFolder.RoleAssignments.Remove(web.AssociatedMemberGroup);
                            newFolder.Properties.Add(Language.ConversationKey, value);
                            newFolder.Update();
                        }
                    }
                }
            });
            return newFolder;
        }

        public static SPListItem GetConversationFolder(SPWeb web, int[] involvedUserIDs)
        {
            SPList list = GetList(web);
            return GetConversationFolder(list, involvedUserIDs);
        }

        public static SPListItem GetConversationFolder(SPList list, int[] involvedUserIDs)
        {
            if(involvedUserIDs.Count() == 0) return null;
            return list.Folders.OfType<SPListItem>().Select(i =>
            {
                if (!i.Properties.ContainsKey(Language.ConversationKey)) return null;
                var stringIDs = involvedUserIDs.OrderBy(p=>p).Select<int, string>(p => p.ToString()).ToArray();
                string value = String.Join(",", stringIDs);
                if (i.Properties[Language.ConversationKey].ToString() == value)
                    return i;
                return null;
            }).FirstOrDefault(i => i != null);
        }

        public static void CreateList(SPWeb web)
        {
            DeleteList(web);

            // New List

            SPListTemplateType template = SPListTemplateType.GenericList;
            Guid listId = web.Lists.Add(Language.SMUListName, Language.SMUListDescription, template);
            SPList list = web.Lists[listId];

            SPContentType ct = web.AvailableContentTypes[Language.SMUChatMessage];
            list.Hidden = true;

#if DEBUG
            list.Hidden = false;
#endif

            list.ContentTypesEnabled = true;

            list.EnableVersioning = false;
            list.ContentTypes.Add(ct);

            // get rid of the item content type
            list.ContentTypes["Item"].Delete();

            CreateDefaultListView(list);

            ApplyGroupRoleAssignments(web, list);
        }

        public static void ApplyGroupRoleAssignments(SPWeb web, SPList list)
        {
            list.BreakRoleInheritance(true);
            SPGroup grp = web.SiteGroups[Language.SMUGroupName];
            SPRoleAssignment ass = new SPRoleAssignment(grp);
            SPRoleDefinition def = web.RoleDefinitions[Language.SMUPermissionName];
            ass.RoleDefinitionBindings.Add(def);
            list.RoleAssignments.Add(ass);
            list.RoleAssignments.Remove(web.AssociatedMemberGroup);
            list.Update();
        }

        public static void DeleteList(SPWeb web)
        {
            SPList list = GetList(web);
            if (list != null)
                list.Delete();
        }

        public static void CreatePersmission(SPWeb web)
        {
            DeletePermission(web);
            SPRoleDefinition def = new SPRoleDefinition()
            {
                Name = Language.SMUPermissionName,
                Description = Language.SMUPermissionDescription,
                BasePermissions =
                    SPBasePermissions.AddListItems |
                    SPBasePermissions.ViewListItems |
                    SPBasePermissions.ViewFormPages |
                    SPBasePermissions.Open
            };
            web.RoleDefinitions.Add(def);
            web.Update();
        }

        public static void DeletePermission(SPWeb web)
        {
            try
            {
                if (web.RoleDefinitions.Cast<SPRoleDefinition>().Any(p => p.Name == Language.SMUPermissionName))
                    web.RoleDefinitions.Delete(Language.SMUPermissionName);
            }
            catch (Exception ex)
            {
                WriteException(ex);
                throw ex;
            }
        }

        public static void CreateGroup(SPWeb web)
        {
            DeleteGroup(web);
            try
            {
                web.SiteGroups.Add(Language.SMUGroupName, web.AssociatedOwnerGroup, null, Language.SMUGroupDescription);
                SPGroup grp = web.SiteGroups.Cast<SPGroup>().FirstOrDefault(p => p.Name == Language.SMUGroupName);
                if (grp == null)
                    throw new Exception(Language.GroupCreateError);
                SPRoleAssignment ass = new SPRoleAssignment(grp);
                SPRoleDefinition def = web.RoleDefinitions[Language.SMUPermissionName];
                ass.RoleDefinitionBindings.Add(def);
                web.RoleAssignments.Add(ass);
                web.Update();
            }
            catch (Exception ex)
            {
                DeleteGroup(web);
                WriteException(ex);
                throw ex;
            }
        }

        public static SPList GetList(SPWeb web)
        {
            SPList result = web.Lists.TryGetList(Language.SMUListName);
            if (result == null)
                throw new Exception(String.Format(Language.ListNotFound, web.Name));
            return result;
        }

        public static void DeleteGroup(SPWeb web)
        {
            try
            {
                SPGroup grp = web.SiteGroups.Cast<SPGroup>().FirstOrDefault(p => p.Name == Language.SMUGroupName);
                if (grp != null)
                    web.SiteGroups.RemoveByID(grp.ID);
            }
            catch (Exception ex)
            {
                WriteException(ex);
                throw ex;
            }
        }

        public static void CreateDefaultListView(SPList list)
        {
            SPViewCollection views = list.Views;
            string viewName = Language.SMUListViewName;
            System.Collections.Specialized.StringCollection viewFields = new System.Collections.Specialized.StringCollection();
            viewFields.Add("ID");
            viewFields.Add("Type");
            viewFields.Add("smReceivers");
            viewFields.Add("Author");
            viewFields.Add("Created");


            StringBuilder query = new StringBuilder();
            query.Append("<Where></Where><OrderBy><FieldRef Name='Created' Ascending='FALSE' /></OrderBy>");
            views.Add(viewName, viewFields, query.ToString(), 100, true, true);
            SPView allItems = views.Cast<SPView>().FirstOrDefault(p => p.Title == "All Items");
            if (allItems != null)
                views.Delete(allItems.ID);
        }

        public static void WriteException(Exception ex)
        {
            SPDiagnosticsService.Local.WriteTrace(0,
                new SPDiagnosticsCategory(Language.LoggingCategory, TraceSeverity.Unexpected, EventSeverity.Error),
                TraceSeverity.Unexpected,
                ex.Message,
                ex.StackTrace);
        }
    }
}

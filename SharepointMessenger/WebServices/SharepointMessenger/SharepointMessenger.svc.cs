using Microsoft.SharePoint.Client.Services;
using System.ServiceModel.Activation;
using Microsoft.SharePoint;
using SharepointMessenger.Repositories;
using SharepointMessenger.Models;
using Microsoft.SharePoint.Utilities;
using System.Linq;
using System;
using System.ServiceModel.Web;
using System.Threading;
using System.Text;
using System.IO;

namespace SharepointMessenger.WebServices
{
    [BasicHttpBindingServiceMetadataExchangeEndpoint]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class SharepointMessenger : ISharepointMessenger
    {
        public System.IO.Stream ExportHistory(string SenderID)
        {
            StringBuilder result = new StringBuilder();
            try
            {
                IChatMessageRepository repo = new ChatMessageRepository();
                var messages = repo.GetConversationHistory(SPContext.Current.Web.CurrentUser.ID, Int32.Parse(SenderID));
                result.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
                result.Append("<ChatMessages>");
                foreach (ChatMessage msg in messages)
                    result.Append(msg.GetXml());
                result.Append("</ChatMessages>");
            }
            catch (Exception ex)
            {
                Config.WriteException(ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.CreateChatMessageError;
            }
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
            return new MemoryStream(Encoding.UTF8.GetBytes(result.ToString()));;
        }

        public ChatContactServiceView[] ListContacts(int messageTimeOut)
        {
            ChatContactServiceView[] result = null;
            try
            {
                IGroupRepository repo = new GroupRepository();
                IContactRepository contactRepo = new ContactRepository();
                var group = repo.GetGroup(Language.SMUGroupName);
                result = contactRepo.GetAllFromGroup(group, messageTimeOut).Select(c => new ChatContactServiceView() { ImageUrl = c.ImageUrl, ID = c.ID, Username = c.Username.Split('\\').Last(), Name = c.Name }).OrderBy(u => u.Name).ToArray();
            }
            catch (Exception ex)
            {
                Config.WriteException(ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.GetContactsError;
            }
            return result;
        }

        public ContactMessageInfoView GetContactInfoByID(int id)
        {
            ContactMessageInfoView result = null;
            try
            {
                IGroupRepository repo = new GroupRepository();
                IContactRepository contactRepo = new ContactRepository();
                var group = repo.GetGroup(Language.SMUGroupName);
                var contact = contactRepo.GetByID(group, id);
                // TODO: update the exception here with a custom one that goes back to the user if the contact is null
                // currently it will throw an exception but will just give generic error.
                result = new ContactMessageInfoView()
                {
                    ID = id,
                    ImageUrl = contact.ImageUrl,
                    EmailAddress = contact.EmailAddress,
                    Name = contact.Name
                };
            }
            catch (Exception ex)
            {
                Config.WriteException(ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.GetMessagesError;
            }
            return result;
        }

        public void CreateChatMessage(ChatMessageServiceView message)
        {
            try
            {
                if (SPUtility.ValidateFormDigest())
                {
                    IChatMessageRepository repo = new ChatMessageRepository();
                    repo.Create(
                        new ChatMessage()
                        {
                            Title = "",
                            Message = message.Message,
                            Receivers = (message.Receivers != null) ? message.Receivers.Select(p => new Contact() { ID = p.ID }).ToArray() : null
                        }
                    );
                }
                else
                {
                    Config.WriteException(new Exception(Language.AccessDenied));
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Unauthorized;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.UserNotValidated;
                }
            }
            catch (Exception ex)
            {
                Config.WriteException(ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.CreateChatMessageError;
            }
        }

        public ChatMessageListResult StartConversation(int SenderID)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Cache-Control", "no-cache");
            ChatMessageListResult result = null;
            try
            {
                if (SPUtility.ValidateFormDigest())
                {
                    IChatMessageRepository repo = new ChatMessageRepository();
                    // get the users timezone
                    SPTimeZone zone = SPContext.Current.Web.RegionalSettings.TimeZone;
                    if (SPContext.Current.Web.CurrentUser.RegionalSettings != null)
                    {
                        SPRegionalSettings rs = SPContext.Current.Web.CurrentUser.RegionalSettings;
                        zone = rs.TimeZone;
                    }

                    var newItems = repo.GetUnReadByUserIDAndSenderID(SPContext.Current.Web.CurrentUser.ID, SenderID)
                        .Select(m => new ChatMessageServiceView()
                        {
                            ID = m.ID,
                            Created = (zone.UTCToLocalTime(m.Created)).ToString(),
                            CreatedDateOnly = (zone.UTCToLocalTime(m.Created).Date).ToShortDateString(),
                            CreatedTimeOnly = (zone.UTCToLocalTime(m.Created)).ToString("HH:mm"),
                            CreatedBy = m.CreatedBy.Name,
                            Message = m.Message,
                            IsOld = false,
                            Receivers = m.Receivers.Select(r => new ChatContactServiceView()
                            {
                                ID = r.ID,
                                Name = r.Name,
                                Username = r.Username.Split('\\').Last()
                            }).ToArray()
                        }).ToArray();

                    var someOldItems = repo.GetLastByUserIDAndSenderID(SPContext.Current.Web.CurrentUser.ID, SenderID, 3)
                        .Select(m => new ChatMessageServiceView()
                        {
                            ID = m.ID,
                            Created = (zone.UTCToLocalTime(m.Created)).ToString(),
                            CreatedDateOnly = (zone.UTCToLocalTime(m.Created).Date).ToShortDateString(),
                            CreatedTimeOnly = (zone.UTCToLocalTime(m.Created)).ToString("HH:mm"),
                            CreatedBy = m.CreatedBy.Name,
                            Message = m.Message,
                            IsOld = true,
                            Receivers = m.Receivers.Select(r => new ChatContactServiceView()
                            {
                                ID = r.ID,
                                Name = r.Name,
                                Username = r.Username.Split('\\').Last()
                            }).ToArray()
                        }).ToArray();

                    result = new ChatMessageListResult()
                    {
                        LastRequested = DateTime.Now.ToString(),
                    };
                    if (newItems.Length > 0)
                        repo.SetChatMessagesRead(newItems.Select(i => i.ID).ToArray());
                    result.ChatMessages = someOldItems.Concat(newItems).ToArray();
                }
                else
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Unauthorized;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.UserNotValidated;
                }
            }
            catch (Exception ex)
            {
                Config.WriteException(ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.GetMessagesError;
            }
            return result;
        }

        public ChatMessageListResult ChatMessages(int SenderID)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Cache-Control", "no-cache");
            ChatMessageListResult result = null;
            try
            {
                if (SPUtility.ValidateFormDigest())
                {
                    IChatMessageRepository repo = new ChatMessageRepository();
                    // get the users timezone
                    SPTimeZone zone = SPContext.Current.Web.RegionalSettings.TimeZone;
                    if (SPContext.Current.Web.CurrentUser.RegionalSettings != null)
                    {
                        SPRegionalSettings rs = SPContext.Current.Web.CurrentUser.RegionalSettings;
                        zone = rs.TimeZone;
                    }
                    result = new ChatMessageListResult()
                    {
                        LastRequested = DateTime.Now.ToString(),
                        ChatMessages = repo.GetUnReadByUserIDAndSenderID(SPContext.Current.Web.CurrentUser.ID, SenderID)
                        .Select(m => new ChatMessageServiceView()
                        {
                            ID = m.ID,
                            Created = (zone.UTCToLocalTime(m.Created)).ToString(),
                            CreatedDateOnly = (zone.UTCToLocalTime(m.Created).Date).ToShortDateString(),
                            CreatedTimeOnly = (zone.UTCToLocalTime(m.Created)).ToString("HH:mm"),
                            CreatedBy = m.CreatedBy.Name,
                            Message = m.Message,
                            Receivers = m.Receivers.Select(r => new ChatContactServiceView()
                            {
                                ID = r.ID,
                                Name = r.Name,
                                Username = r.Username.Split('\\').Last()
                            }).ToArray()
                        }).ToArray()
                    };
                    if (result.ChatMessages.Length > 0)
                        repo.SetChatMessagesRead(result.ChatMessages.Select(i => i.ID).ToArray());
                }
                else
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Unauthorized;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.UserNotValidated;
                }
            }
            catch (Exception ex)
            {
                Config.WriteException(ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.GetMessagesError;
            }
            return result;
        }

        public PendingMessageView[] PendingMessageCounts(int messageTimeOut)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Cache-Control", "no-cache");
            PendingMessageView[] result = new PendingMessageView[0];
            try
            {
                if (SPUtility.ValidateFormDigest())
                {
                    // set the current user to online
                    IContactRepository crepo = new ContactRepository();
                    crepo.SetContactOnline(Language.SMUGroupName, SPContext.Current.Web.CurrentUser.ID);
                    
                    // get the user statuses
                    IGroupRepository grepo = new GroupRepository();
                    var group = grepo.GetGroup(Language.SMUGroupName);
                    var contactList = crepo.GetAllFromGroup(group, messageTimeOut);

                    // get the pending messages
                    IChatMessageRepository repo = new ChatMessageRepository();
                    var messages = repo.GetPendingMessageByUser(SPContext.Current.Web.CurrentUser.ID);

                    result = (from m in messages
                              group m by m.CreatedBy.ID into g
                              select new PendingMessageView()
                              {
                                  ID = g.Key,
                                  IsOnline = false,
                                  Count = g.Count()
                              }).ToArray();

                    // get the pending information
                    result = (from i in contactList
                              where i.ID != SPContext.Current.Web.CurrentUser.ID
                              select new PendingMessageView()
                              {
                                  ID = i.ID,
                                  IsOnline = i.IsOnline,
                                  Count = 
                                    (result.Any(p => p.ID == i.ID) ?
                                        result.First(p => p.ID == i.ID).Count : 0)
                              }).ToArray();
                }
                else
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.Unauthorized;
                    WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.UserNotValidated;
                }
            }
            catch (Exception ex)
            {
                Config.WriteException(ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.GetMessagesError;
            }
            return result;
        }
    }
}


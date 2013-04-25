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

namespace SharepointMessenger.WebServices
{
    [BasicHttpBindingServiceMetadataExchangeEndpoint]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Required)]
    public class SharepointMessenger : ISharepointMessenger
    {
        public ChatContactServiceView[] ListContacts()
        {
            ChatContactServiceView[] result = null;
            try
            {
                IGroupRepository repo = new GroupRepository();
                IContactRepository contactRepo = new ContactRepository();
                var group = repo.GetGroup(Language.SMUGroupName);
                result = contactRepo.GetAllFromGroup(group).Select(c => new ChatContactServiceView() { ImageUrl = c.ImageUrl, ID = c.ID, Username = c.Username.Split('\\').Last(), Name = c.Name }).OrderBy(u => u.Name).ToArray();
            }
            catch (Exception ex)
            {
                Config.WriteException(ex);
                WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
                WebOperationContext.Current.OutgoingResponse.StatusDescription = Language.GetContactsError;
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

        // remember that the time on the server is not the same as the time on the 
        // client, might want to send back the last time for the server
        public ChatMessageListResult ChatMessages(int SenderID)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Cache-Control", "no-cache");
            ChatMessageListResult result = null;
            try
            {
                if (SPUtility.ValidateFormDigest())
                {
                    IChatMessageRepository repo = new ChatMessageRepository();
                    result = new ChatMessageListResult()
                    {
                        LastRequested = DateTime.Now.ToString(),
                        ChatMessages = repo.GetUnReadByUserIDAndSenderID(SPContext.Current.Web.CurrentUser.ID, SenderID)
                        .Select(m => new ChatMessageServiceView()
                        {
                            ID = m.ID,
                            Created = m.Created.ToString(),
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

        public PendingMessageView[] PendingMessageCounts()
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Cache-Control", "no-cache");
            PendingMessageView[] result = new PendingMessageView[0];
            try
            {
                IChatMessageRepository repo = new ChatMessageRepository();
                var messages = repo.GetPendingMessageByUser(SPContext.Current.Web.CurrentUser.ID);
                if (messages.Count() == 0)
                    return result;
                result = (from m in messages
                          group m by m.CreatedBy.ID into g
                          select new PendingMessageView()
                             {
                                 ID = g.Key,
                                 Count = g.Count()
                             }).ToArray();
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

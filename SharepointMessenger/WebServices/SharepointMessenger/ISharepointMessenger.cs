using System.ServiceModel;
using System.ServiceModel.Web;
using System.Runtime.Serialization;

namespace SharepointMessenger.WebServices
{
    [ServiceContract]
    public interface ISharepointMessenger
    {
        [OperationContract]
        [WebInvoke(UriTemplate = "Contacts", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ChatContactServiceView[] ListContacts();

        [OperationContract]
        [WebInvoke(UriTemplate = "Contacts/ContactInfoByID", Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ContactMessageInfoView GetContactInfoByID(int id);

        [OperationContract]
        [WebInvoke(UriTemplate = "ChatMessages/Create", Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        void CreateChatMessage(ChatMessageServiceView message);

        [OperationContract]
        [WebInvoke(UriTemplate = "ChatMessages/StartConversation", Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ChatMessageListResult StartConversation(int SenderID);

        [OperationContract]
        [WebInvoke(UriTemplate = "ChatMessages", Method = "POST", BodyStyle = WebMessageBodyStyle.WrappedRequest, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        ChatMessageListResult ChatMessages(int SenderID);

        [OperationContract]
        [WebInvoke(UriTemplate = "ChatMessages/PendingMessageCounts", Method = "GET", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        PendingMessageView[] PendingMessageCounts();

        [OperationContract]
        [WebGet(UriTemplate = "ChatMessages/ExportHistory/{SenderID}", BodyStyle = WebMessageBodyStyle.Bare)]
        System.IO.Stream ExportHistory(string SenderID);
    }

    [DataContract]
    public class PendingMessageView
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int Count { get; set; }
    }

    [DataContract]
    public class ContactMessageInfoView
    {
        [DataMember]
        public int ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string ImageUrl { get; set; }

        [DataMember]
        public string EmailAddress { get; set; }
    }

    [DataContract]
    public class ChatContactServiceView
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Username { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string ImageUrl { get; set; }
    }

    [DataContract]
    public class ChatMessageServiceView
    {
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public ChatContactServiceView[] Receivers { get; set; }
        [DataMember]
        public string CreatedBy { get; set; }
        [DataMember]
        public string Created { get; set; }
        [DataMember]
        public string CreatedDateOnly { get; set; }
        [DataMember]
        public string CreatedTimeOnly { get; set; }
        [DataMember]
        public bool IsOld { get; set; }
    }

    [DataContract]
    public class ChatMessageListResult
    {
        [DataMember]
        public string LastRequested { get; set; }
        [DataMember]
        public ChatMessageServiceView[] ChatMessages { get; set; }
    }
}


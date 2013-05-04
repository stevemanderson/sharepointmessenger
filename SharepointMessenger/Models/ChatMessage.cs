using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharepointMessenger.Extensions;
using Microsoft.SharePoint.Utilities;

namespace SharepointMessenger.Models
{
    public class ChatMessage
    {
        private int _id;
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }
        private string _title;
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }
        private string _message;
        public string Message
        {
            get { return SPHttpUtility.HtmlDecode(_message); }
            set { _message = SPHttpUtility.HtmlEncode(value); }
        }
        private Contact[] _receivers;
        public Contact[] Receivers
        {
            get { return _receivers; }
            set { _receivers = value; }
        }
        private DateTime _created;
        public DateTime Created
        {
            get { return _created; }
            set { _created = value; }
        }
        private Contact _createdBy;
        public Contact CreatedBy
        {
            get { return _createdBy; }
            set { _createdBy = value; }
        }
        private bool _isRead = false;
        public bool IsRead
        {
            get { return _isRead; }
            set { _isRead = value; }
        }

        public string GetXml()
        {
            StringBuilder result = new StringBuilder();
            result.AppendFormat("<ChatMessage ID='{0}'>", this.ID);
            result.AppendFormat("<{0}>{1}</{0}>", "Title", SPHttpUtility.HtmlEncode(this.Title));
            result.AppendFormat("<{0}>{1}</{0}>", "Message", SPHttpUtility.HtmlEncode(this.Message));

            result.Append("<Receivers>");
            foreach(Contact receiver in this.Receivers)
                result.AppendFormat("<{0} ID='{2}'>{1}</{0}>", "Receiver", SPHttpUtility.HtmlEncode(receiver.Name), receiver.ID);
            result.Append("</Receivers>");

            result.AppendFormat("<{0}>{1}</{0}>", "Created", this.Created);
            result.AppendFormat("<{0} ID='{2}'>{1}</{0}>", "CreatedBy", SPHttpUtility.HtmlEncode(this.CreatedBy.Name), this.CreatedBy.ID);
            result.AppendFormat("<{0}>{1}</{0}>", "IsRead", this.IsRead);
            result.Append("</ChatMessage>");
            return result.ToString();
        }
    }
}

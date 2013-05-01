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
    }
}

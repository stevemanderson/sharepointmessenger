using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System.Xml.Serialization;

namespace SharepointMessenger.WebParts.ContactList
{
    [ToolboxItemAttribute(false)]
    public class ContactList : Microsoft.SharePoint.WebPartPages.WebPart
    {
        protected int _messageTimeOut = 5000;
        protected bool _showContactImages = false;
        protected string _defaultSite = "";

        [Category("Sharepoint Messenger")]
        [WebPartStorage(Storage.Shared)]
        [FriendlyNameAttribute("Default Site")]
        [Description("The name of the default site. The site that the Chat Messages List feature is activated.")]
        [Browsable(true)]
        [DefaultValue("")]
        public string DefaultSite
        {
            get
            {
                return _defaultSite;
            }
            set
            {
                _defaultSite = value;
            }
        }

        [Category("Sharepoint Messenger")]
        [WebPartStorage(Storage.Shared)]
        [FriendlyNameAttribute("Message Load Timeout")]
        [Description("The timeout for the call to the web service.")]
        [Browsable(true)]
        [DefaultValue(5000)]
        public int MessageTimeOut
        {
            get
            {
                return _messageTimeOut;
            }
            set
            {
                _messageTimeOut = value;
            }
        }

        [Category("Sharepoint Messenger")]
        [WebPartStorage(Storage.Personal)]
        [FriendlyNameAttribute("Show Contact Images")]
        [Description("Show contact images in the contact list.")]
        [Browsable(true)]
        [DefaultValue(5000)]
        public bool ShowContactImages
        {
            get
            {
                return _showContactImages;
            }
            set
            {
                _showContactImages = value;
            }
        }
        
        // Visual Studio might automatically update this path when you change the Visual Web Part project item.
        private const string _ascxPath = @"~/_CONTROLTEMPLATES/SharepointMessenger.WebParts/ContactList/ContactListUserControl.ascx";

        protected override void CreateChildControls()
        {
            Control control = Page.LoadControl(_ascxPath);
            (control as ContactListUserControl).MessageTimeOut = MessageTimeOut;
            (control as ContactListUserControl).ShowContactImages = ShowContactImages;
            (control as ContactListUserControl).DefaultSite = DefaultSite;
            Controls.Add(control);
        }
    }
}

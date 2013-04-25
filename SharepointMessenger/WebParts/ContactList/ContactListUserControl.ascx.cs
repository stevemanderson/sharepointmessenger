using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;

namespace SharepointMessenger.WebParts.ContactList
{
    public partial class ContactListUserControl : UserControl
    {
        protected string CurrentMessageUser
        {
            get { return SPContext.Current.Web.CurrentUser.Name; }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}

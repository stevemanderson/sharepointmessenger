using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;

namespace SharepointMessenger.WebParts.ContactList
{
    public partial class ContactListUserControl : UserControl
    {
        protected string ThemeName
        {
            get 
            {
                string themeUrl = ThmxTheme.GetThemeUrlForWeb(SPContext.Current.Web);
                if (!String.IsNullOrEmpty(themeUrl))
                {
                    ThmxTheme theme = ThmxTheme.Open(SPContext.Current.Site, themeUrl);
                    return theme.Name.ToLower().Replace(" ", "_"); 
                }
                return "classic";
            }
        }
        protected string CurrentMessageUser
        {
            get { return SPContext.Current.Web.CurrentUser.Name; }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}

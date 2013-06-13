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
        public int MessageTimeOut { set; get; }
        public bool ShowContactImages { set; get; }
        public string DefaultSite { set; get; }

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
        protected string UserTimeZone
        {
            get
            {
                SPTimeZone zone = SPContext.Current.Web.RegionalSettings.TimeZone;
                if (SPContext.Current.Web.CurrentUser.RegionalSettings != null)
                {
                    SPRegionalSettings rs = SPContext.Current.Web.CurrentUser.RegionalSettings;
                    zone = rs.TimeZone;
                }
                var time = (zone.Information.Bias / -60);
                time += (zone.Information.DaylightBias / -60);
                return time.ToString();
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
        }
    }
}

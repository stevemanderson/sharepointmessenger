<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ContactListUserControl.ascx.cs" Inherits="SharepointMessenger.WebParts.ContactList.ContactListUserControl" %>
<SharePoint:FormDigest runat="server"/>

<div id='sharepoint-messenger'></div>

<link rel="Stylesheet" type="text/css" href="/_layouts/SharepointMessenger/css/<%= ThemeName %>/jquery-ui-1.10.2.custom.min.css" />
<link rel="Stylesheet" type="text/css" href="/_layouts/SharepointMessenger/css/style.css" />

<script type="text/javascript" src="/_layouts/SharepointMessenger/js/json2.js"></script>
<script type="text/javascript" src="/_layouts/SharepointMessenger/js/jquery-1.9.1.min.js"></script>
<script type="text/javascript" src="/_layouts/SharepointMessenger/js/jquery-ui-1.10.2.custom.min.js"></script>
<script type="text/javascript" src="/_layouts/SharepointMessenger/js/jquery-sharepointmessenger-1.0.0.js"></script>

<script type="text/javascript">
    $('#sharepoint-messenger').sharepointmessenger({CurrentUsername:'<%= CurrentMessageUser %>', TimeZone:<%= UserTimeZone %> });
</script>
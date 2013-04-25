SharePoint Messenger
===================

Description
-
I started this project to give SharePoint the chat/message functionality and to expand my SharePoint knowledge. I know that there are features out there that allow chat functionality however, I wanted to take it a bit further and allow users to specify the recipients, allow administration of the list items and groups and to give a richer experience to the end user.

Features
-
The features are a work in progress. Anything that has a strike through is not completed.

- <del>Multiple receiver messages</del>
- Custom view so users will only see messages they created or they are a receiver of
- A chat group so you know who has the permission to use the message list
- A specific role definition for the chat associated to the group to keep your custom roles/permissions separate
- <del>Archiving messages after a specific amount of time</del>
- <del>Allow webpart in subsites to access a specific site's Chat Messages. If you have the Chat Messages added to the root site and you create child sites, there should be a setting to use the parent site.</del>
- <del>FBA Compatibility</del>
- <del>I am not sure if there is already functionality for Sharepoint to see who is online, but view online users</del>
- <del>refresh time</del>
- <del>show previous messages/number of previous messages</del>
- <del>Smiley support</dev>
- Show list of users in the messenger group
- Current chat windows
- <del>Previous messages list</del>
- <del>Send file (Attach to the list item)</del>
- Style sheets for customization
- <del>Sound on receiving new message</del>
- <del>Flashing on receiving new message</del>
- <del>Group Chats I.E. chats with multiple people</del>
- <del>Off the record (Use the list as just a queue and when the item(s) are read then delete them)</del>
- <del>Show when message has been read to the sender</del>

Installation
-
I want the installation to be as simple as possible. Currently there is a site and web feature. This is a farm solution. The site feature deploys the content type and fields. The web feature creates the permissions, the group and the list.

1. Deploy the solution to the farm
2. Activate the site feature
3. Activate the web feature
4. Add any users that you would like to have permission to the Sharepoint Messenger Group
5. Add webparts to the pages that you would like the chat to show up






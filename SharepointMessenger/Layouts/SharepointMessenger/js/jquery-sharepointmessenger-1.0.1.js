(function ($) {

    function cleanHTML(value) {
        var temp = document.createElement("div");
        temp.innerHTML = value;
        var sanitized = temp.textContent || temp.innerText;
        return sanitized;
    }

    function html5Supported() {
        var elem = document.createElement('canvas');
        return !!(elem.getContext && elem.getContext('2d'));
    }

    $.fn.sharepointmessenger = function (options) {
        var chats = [];
        var self = this;
        var settings = $.extend({
            ShowContactImages: false,
            TimeZone: 0,
            CurrentUsername: "You",
            MessageTimeOut: 5000,
            Service: "/_vti_bin/SharepointMessenger.WebServices/SharepointMessenger.svc",
            FormDigestID: "__REQUESTDIGEST"
        }, options);
        var digestId = $('#' + settings.FormDigestID).val();
        settings = $.extend({
            FormDigest: digestId
        }, settings);

        var COOKIE = {
            Loaded: false,
            ShowUserInformation: 1,
            HTML5: 0,
            Name: "SharepointMessenger",
            Value: function () {
                var arr = [];
                arr.push(escape(this.ShowUserInformation));
                arr.push(escape(this.HTML5));
                return arr.join('|');
            },
            parseString: function (str) {
                var arr = str.split('|');
                this.ShowUserInformation = arr[0];
                this.HTML5 = arr[1];
            },
            loadCookie: function () {
                var cookies = document.cookie.replace(' ', '').split(';');
                var cookie = { "Name": "", "Value": "" };
                for (var i = 0; i < cookies.length; ++i) {
                    var s = cookies[i].split('=');
                    if (s.length > 1) {
                        if (s[0] == this.Name) {
                            this.parseString(s[1]);
                            this.Loaded = true;
                            break;
                        }
                    }
                }
            },
            exists: function () {
                return this.Loaded;
            },
            set: function (days) {
                var date = new Date();
                date.setDate(date.getDate() + days);
                var value = this.Value() + ((days == null) ? "" : "; expires=" + date.toUTCString());
                console.log(value);
                document.cookie = this.Name + "=" + value;
            },
            remove: function () {
                document.cookie = this.Name + '=; expires=Thu, 01 Jan 1970 00:00:01 GMT;';
            }
        };

        COOKIE.loadCookie();

        if (!COOKIE.exists()) {
            COOKIE.set(1);
        }

        if (html5Supported()) {
            COOKIE.HTML5 = 1;
            COOKIE.set(1);
        }

        function TopError(xmlhttp) {
            if (xmlhttp.status == 0) return;
            if ($('#sharepoint-messenger .err').length == 0) {
                var err = $("<div class='err'></div>");
                err.append($("<span>" + xmlhttp.statusText + "</span>"));
                $('#sharepoint-messenger').prepend(err);
            }
        }

        function getAdjustedDate() {
            var d = new Date();
            var localTime = d.getTime();
            var localOffset = d.getTimezoneOffset() * 60000;
            var utc = localTime + localOffset;
            var offset = settings.TimeZone;
            var d2 = utc + (3600000 * offset);
            return new Date(d2);
        }

        function getDate() {
            var temp = getAdjustedDate();
            var result = (temp.getMonth() + 1).toString() + '/' + temp.getDate().toString() + '/' + temp.getFullYear().toString();
            return result;
        }

        function getTime() {
            var temp = getAdjustedDate();
            var hours = (temp.getHours() < 10) ? ('0' + temp.getHours().toString()) : temp.getHours().toString();
            var minutes = (temp.getMinutes() < 10) ? ('0' + temp.getMinutes().toString()) : temp.getMinutes().toString();
            var result = hours + ':' + minutes;
            return result;
        }

        var service = {
            Send: function (method, uri, data, onComplete, params, onFail) {
                var xmlhttp = new XMLHttpRequest();
                xmlhttp.open(method.toString().toUpperCase(), settings.Service + '/' + uri, true);
                xmlhttp.setRequestHeader('X-RequestDigest', settings.FormDigest);
                xmlhttp.setRequestHeader('Content-Type', 'application/json');
                xmlhttp.setRequestHeader('Pragma', 'no-cache');
                xmlhttp.setRequestHeader('Cache-Control', 'no-cache');
                xmlhttp.onreadystatechange = function () {
                    if (xmlhttp.readyState == 4) {
                        if (xmlhttp.status == 200) {
                            if (onComplete != null)
                                onComplete(xmlhttp, params);
                        }
                        else {
                            if (onFail != null)
                                onFail(xmlhttp, params);
                        }
                    }
                }
                if (data != null)
                    xmlhttp.send(JSON.stringify(data));
                else
                    xmlhttp.send();
            }
        };

        var Repository = {
            Contacts: {
                DataSource: service,
                All: function (callback, params, onfail) {
                    this.DataSource.Send('get', 'Contacts', {}, callback, params, onfail);
                },
                GetContactInfoByID: function (id, callback, params, onfail) {
                    this.DataSource.Send('post', 'Contacts/ContactInfoByID', { "id": id }, callback, params, onfail);
                }
            },
            ChatMessages: {
                DataSource: service,
                Create: function (message, callback, params, onfail) {
                    this.DataSource.Send('post', 'ChatMessages/Create', { "message": params }, callback, params, onfail);
                },
                StartConversation: function (id, callback, params, onfail) {
                    this.DataSource.Send('post', 'ChatMessages/StartConversation', { "SenderID": id }, callback, params, onfail);
                },
                GetNewMessages: function (id, callback, params, onfail) {
                    this.DataSource.Send('post', 'ChatMessages', { "SenderID": id }, callback, params, onfail);
                },
                GetPendingMessageCounts: function (callback, params, onfail) {
                    this.DataSource.Send('get', 'ChatMessages/PendingMessageCounts', {}, callback, params, onfail);
                },
                ExportHistory: function (id, callback, params, onfail) {
                    this.DataSource.Send('post', 'ChatMessages/ExportHistory', { "SenderID": id }, callback, params, onfail);
                }
            }
        };

        function CloseDialog(event, ui) {
            var temp = [];
            for (var i = 0; i < chats.length; ++i) {
                var chat = chats[i];
                if (chat.Dialog != ('#' + $(this).attr('id'))) {
                    temp.push(chat);
                }
            }
            chats = temp;
            $(this).dialog('destroy').remove();
        }

        function ResizeDialog(event, ui) {
            Resize($(this));
        }

        function Resize(obj) {
            var messages = obj.find('.messages').parent();
            var hDelta = 90;
            var info = obj.find('.user-information');
            if (info.is(':visible')) {
                hDelta = hDelta + 80;
            }
            messages.css('height', messages.parent().height() - hDelta);
            var text = obj.find('textarea');
            var totalWidthAvailable = text.parent().width();
            text.css('width', (totalWidthAvailable - 60) + 'px');
        }

        function GetChatMessages(id, first) {
            $('#sharepoint-messenger .err').remove();
            var found = false;
            for (var i = 0; i < chats.length; ++i) {
                var chat = chats[i];
                if (id == chat.ID) {
                    found = true;
                }
            }
            if (!found) return;
            if (first) {
                Repository.ChatMessages.StartConversation(id, LoadMessages, { "ID": id }, function (xmlhttp, params) {
                    TopError(xmlhttp);
                });
            }
            else {
                Repository.ChatMessages.GetNewMessages(id, LoadMessages, { "ID": id }, function (xmlhttp, params) {
                    TopError(xmlhttp);
                });
            }
            setTimeout(function () { GetChatMessages(id, false); }, settings.MessageTimeOut);
        }

        function SubmitMessage(list, message, id) {
            message = cleanHTML(message);
            if (message.length == 0) return;
            var li = AddMessage(list, { "CreatedBy": settings.CurrentUsername, "Message": message, "CreatedTimeOnly": getTime() }, false);
            Repository.ChatMessages.Create(
                message,
                function () { li.removeClass(); },
                { "Message": message, "Receivers": [{ "ID": id}] },
                function (xmlhttp, params) { li.append(" <b>" + xmlhttp.statusText + "</b>"); li.removeClass(); li.addClass('message-failed'); });
        }

        function KeyPress(event) {
            if (event.which == 13) {
                event.preventDefault();
                $(this).next().click();
            }
        }

        function ButtonClick(event) {
            var list = $(this).parent().prev().find('ul');
            var text = $(this).prev();
            var receiverId = text.attr('data-id');
            SubmitMessage(list, text.val(), receiverId);
            $(this).focus();
            text[0].value = '';
            var container = list.parent();
            setTimeout(
                function () {
                    text.focus();
                    container.animate({ scrollTop: container[0].scrollHeight }, 100);
                }, 20);

            return false;
        }

        function AddMessage(list, obj, sent) {
            var e = obj;
            var li = $('<li></li>');
            if (!sent) {
                li.addClass('message-sending');
            }
            if (e.IsOld) {
                li.addClass('is-old');
                var html = "";
                if (getDate() != e.CreatedDateOnly) html += e.CreatedDateOnly + " ";
                html += e.CreatedTimeOnly + " <b>" + e.CreatedBy + "</b>" + ' says: ' + e.Message;
                li.html(html);
            }
            else { li.html(e.CreatedTimeOnly + " <b>" + e.CreatedBy + "</b>" + ' says: ' + e.Message); }
            list.append(li);
            return li;
        }

        function LoadMessages(xhr, params) {
            var result = JSON.parse(xhr.responseText);
            var messages = result.ChatMessages;
            var o = null;
            for (var i = 0; i < chats.length; ++i) {
                var chat = chats[i];
                if (chat.ID == params.ID)
                    o = chat.Dialog;
            }
            if (o == null) {
                alert('Could not load message id dialog. Sharepoint Messenger might not be installed properly. Also, please see minimum browser requirements.');
                return;
            }
            var list = $(o).find('.messages');
            for (var i = 0; i < messages.length; ++i) {
                AddMessage(list, messages[i], true);
            }
            var container = list.parent();
            container.animate({ scrollTop: container[0].scrollHeight }, 1000);
        }

        function UpdateMessageCounts(xhr) {
            var result = JSON.parse(xhr.responseText);
            $.each(result, function () {
                var o = this;
                var found = false;
                for (var i = 0; i < chats.length; ++i) {
                    if (chats[i].ID == o.ID) {
                        found = true;
                    }
                }
                if (found) return;
                var li = $('#users li[data-id=' + o.ID + ']');
                li.addClass('ui-widget-header');
                var count = li.find('span');
                count.html(o.Count);
            });
            setTimeout(function () { GetUserMessageCounts(); }, settings.MessageTimeOut);
        }

        function GetUserMessageCounts() {
            $('#sharepoint-messenger .err').remove();
            Repository.ChatMessages.GetPendingMessageCounts(
                UpdateMessageCounts, {},
                function (xmlhttp, params) {
                    TopError(xmlhttp);
                });
        }

        function GetUserInformation(id) {
            var info = $('<div class="information"></div>');
            var closeButton = $('<button class="visible-toggle">Toggle User Information</button>');
            var exportButton = $('<button class="export">Export Message History</button>');
            var userInfo = $('<div class="user-information ui-widget-header"></div>');
            var img = $('<img src="/_layouts/SharepointMessenger/images/loader-50x50.gif" alt="User Image" />');
            var name = $('<span class="name"></span>');
            var emailaddress = $('<span class="emailaddress"></span>');
            userInfo.append(img);
            var internalinfo = $('<div class="info"></div>');
            internalinfo.append(name);
            internalinfo.append('<br/>');
            internalinfo.append(emailaddress);
            userInfo.append(internalinfo);
            var dialog = $(this).closest('.chat-dialog');
            var icon = "ui-icon-arrow-1-nw";
            if (COOKIE.ShowUserInformation == 1) {
                icon = "ui-icon-arrow-1-nw";
                userInfo.show();
            }
            else {
                icon = "ui-icon-arrow-1-se";
                userInfo.hide();
            }
            closeButton.button({
                icons: {
                    primary: icon
                },
                text: false
            });
            exportButton.button({
                icons: {
                    primary: "ui-icon-copy"
                },
                text: false
            });
            exportButton.click(function () {
                var win = window.open(settings.Service + '/ChatMessages/ExportHistory/' + id, '_blank');
                win.focus();
            });

            closeButton.click(function () {
                var dialog = $(this).closest('.chat-dialog');
                $(this).siblings(".user-information").slideToggle('fast', function () {
                    if ($(this).is(':visible')) {
                        COOKIE.ShowUserInformation = 1;
                        $(this).siblings('.visible-toggle').button({ icons: { primary: "ui-icon-arrow-1-nw"} });
                    }
                    else {
                        COOKIE.ShowUserInformation = 0;
                        $(this).siblings('.visible-toggle').button({ icons: { primary: "ui-icon-arrow-1-se"} });
                    }
                    COOKIE.set(1);
                    Resize(dialog);
                });
            });
            info.append(closeButton);
            info.append(exportButton);
            info.append('<div style="clear:both"></div>');
            info.append(userInfo);

            // get the user data
            Repository.Contacts.GetContactInfoByID(
                id,
                function (xhr) {
                    var obj = jQuery.parseJSON(xhr.responseText);
                    img.attr('src', obj.ImageUrl);
                    name.html(obj.Name);
                    emailaddress.html(obj.EmailAddress);
                },
                { "ContactID": id },
                function (xmlhttp, params) {
                    alert(xmlhttp.statusText);
                });
            return info;
        }

        function SelectUser() {
            var id = $(this).attr('data-id');
            $(this).removeClass('ui-widget-header');
            $(this).find('span').html('0');
            var found = false;
            for (var i = 0; i < chats.length; ++i) {
                if (chats[i].ID == id) { found = true; break; }
            }
            if (found) return;
            var o = { "ID": id, "Dialog": '#chat-dialog-' + id };
            chats.push(o);
            var chatDialog = $('<div class="chat-dialog" id="chat-dialog-' + id + '" style="overflow:hidden"></div>');
            var msgContainer = $('<div class="message-container"><ul class="messages"></ul></div><div class="control-container"><textarea data-id="' + id + '"></textarea><button class="send">Send</button></div>');
            chatDialog.append(GetUserInformation(id));
            chatDialog.append(msgContainer);
            self.append(chatDialog);
            self.find('.send').click(ButtonClick);
            self.find('textarea').keypress(KeyPress);
            $(o.Dialog).dialog({
                title: 'Chatting with ' + $(this).find('a').html(),
                focus: ResizeDialog,
                resize: ResizeDialog,
                close: CloseDialog,
                width: 400,
                height: 400
            });
            GetChatMessages(o.ID, true);
        }

        function LoadUsers(xhr) {
            var users = jQuery.parseJSON(xhr.responseText);
            var list = $('#users');
            $.each(users, function () {
                var span = $('<a href="#">' + this.Name + '</a>');
                var msgs = $('<span class="message-count">0</span>');
                var li = $('<li></li>');
                li.attr('data-id', this.ID);

                if (settings.ShowContactImages) {
                    var img = $('<img src="' + this.ImageUrl + '" alt="User Image" />');
                    li.append(img);
                }

                li.append(span);
                li.append(msgs);
                li.append("<div class='clear'></div>");
                list.append(li);
                li.click(SelectUser);
            });
            GetUserMessageCounts();
        }

        this.append('<ul id="users"></ul>');
        Repository.Contacts.All(LoadUsers, {},
        function (xmlhttp, params) {
            TopError(xmlhttp);
        });
    };
})(jQuery);
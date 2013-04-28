(function ($) {


    function cleanHTML(value) {
        var temp = document.createElement("div");
        temp.innerHTML = value;
        var sanitized = temp.textContent || temp.innerText;
        return sanitized;
    }

    $.fn.sharepointmessenger = function (options) {
        var chats = [];
        var self = this;
        var settings = $.extend({
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
                }
            },
            ChatMessages: {
                DataSource: service,
                Create: function (message, callback, params, onfail) {
                    this.DataSource.Send('post', 'ChatMessages/Create', { "message": params }, callback, params, onfail);
                },
                GetNewMessages: function (id, callback, params, onfail) {
                    this.DataSource.Send('post', 'ChatMessages', { "SenderID": id }, callback, params, onfail);
                },
                GetPendingMessageCounts: function (callback, params, onfail) {
                    this.DataSource.Send('get', 'ChatMessages/PendingMessageCounts', {}, callback, params, onfail);
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
            var messages = $(this).find('.messages').parent();
            messages.css('height', messages.parent().height() - 70);
            var text = $(this).find('textarea');
            var totalWidthAvailable = text.parent().width();
            text.css('width', (totalWidthAvailable - 60) + 'px');
        }

        function GetChatMessages(id) {
            $('#sharepoint-messenger .err').remove();
            var found = false;
            for (var i = 0; i < chats.length; ++i) {
                var chat = chats[i];
                if (id == chat.ID) {
                    found = true;
                }
            }
            if (!found) return;
            Repository.ChatMessages.GetNewMessages(id, LoadMessages, { "ID": id },
            function (xmlhttp, params) {
                TopError(xmlhttp);
            });
            setTimeout(function () { GetChatMessages(id); }, settings.MessageTimeOut);
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
            li.html(e.CreatedTimeOnly + " <b>" + e.CreatedBy + "</b>" + ' says: ' + e.Message);
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
                var count = $('#users li[data-id=' + o.ID + '] span');
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

        function SelectUser() {
            var id = $(this).attr('data-id');
            $(this).find('span').html('0');
            var found = false;
            for (var i = 0; i < chats.length; ++i) {
                if (chats[i].ID == id) { found = true; break; }
            }
            if (found) return;
            var o = { "ID": id, "Dialog": '#chat-dialog-' + id };
            chats.push(o);
            self.append($('<div class="chat-dialog" id="chat-dialog-' + id + '" style="overflow:hidden"><div class="message-container"><ul class="messages"></ul></div><div class="control-container"><textarea data-id="' + id + '"></textarea><button class="send">Send</button></div></div>'));
            self.find('button').click(ButtonClick);
            self.find('textarea').keypress(KeyPress);
            $(o.Dialog).dialog({
                title: 'Chatting with ' + $(this).find('a').html(),
                focus: ResizeDialog,
                resize: ResizeDialog,
                close: CloseDialog,
                width: 400,
                height: 400
            });
            GetChatMessages(o.ID);
        }

        function LoadUsers(xhr) {
            var users = jQuery.parseJSON(xhr.responseText);
            var list = $('#users');
            $.each(users, function () {
                var img = $('<img src="' + this.ImageUrl + '" alt="User Image" />');
                var span = $('<a href="#">' + this.Name + '</a>');
                var msgs = $('<span class="message-count">0</span>');
                var li = $('<li></li>');
                li.attr('data-id', this.ID);
                li.append(img);
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
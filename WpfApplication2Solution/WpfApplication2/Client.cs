using GalaSoft.MvvmLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication2
{
    public class Client
    {
        

        public void OnNewConnectedCustomer(string name)
        {
            UserEventArgs UserEA = new UserEventArgs(name);
            NewConnectedCustomer?.Invoke(this, UserEA);
        }

        public event EventHandler<UserEventArgs> NewConnectedCustomer;

        public sealed class UserEventArgs : EventArgs
        {
            public string UserName { get; private set; }

            public UserEventArgs (string username)
            {
                UserName = username;
            }
        }
        

        public void OnReceivedMessage ()
        {
            MessageEventArgs MessageEA = new MessageEventArgs("Bonjour je suis un message test");
            ReceivedMessage?.Invoke(this, MessageEA);
        }

        public event EventHandler<MessageEventArgs> ReceivedMessage;

        public sealed class MessageEventArgs : EventArgs
        {
            public string MessageContent { get; private set; }

            public MessageEventArgs(string messageContent)
            {
                MessageContent = messageContent;
            }
        }

    }
}

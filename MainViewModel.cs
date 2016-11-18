using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WpfApplication2;

namespace WpfApplication2.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public string Name { get; private set; }
        public string WelcomeMsg { get; private set; }
        private readonly object verrou = new object();
        private readonly object verrou2 = new object();

        public Client client { get; set; } = new Client();

        /*public sealed class ObservableClient : ObservableObject
        {
            private string name;
            public string Name { get { return name; } set { Set(() => Name, ref name, value); } }

            private bool hasSentANewMessage = false;
            public bool HasSentANewMessage { get { return hasSentANewMessage; } set { Set(() => HasSentANewMessage, ref hasSentANewMessage, value); } }
        }*/

        public sealed class ObservableClient : ObservableObject
        {
            private string name;
            public string Name { get { return name; } set { name = value; } }

            private bool hasSentANewMessage = false;
            public bool HasSentANewMessage { get { return hasSentANewMessage; } set { Set(() => HasSentANewMessage, ref hasSentANewMessage, value); }  }
        }

        //private List<ObservableClient> connectedUsers = new List<ObservableClient>();
        //public List<ObservableClient> ConnectedUsers { get { return connectedUsers; } set { Console.WriteLine("setting ConnectedUsers"); connectedUsers = value; RaisePropertyChanged();  } }
        private ObservableCollection<ObservableClient> connectedUsers = new ObservableCollection<ObservableClient>();
        public ObservableCollection<ObservableClient> ConnectedUsers { get { return connectedUsers; } set { Console.WriteLine("setting ConnectedUsers"); connectedUsers = value; /*RaisePropertyChanged();*/ } }

        private ObservableClient currentInterlocutor = new ObservableClient();
        public ObservableClient CurrentInterlocutor
        {
            get
            {
                return currentInterlocutor;
            }
            set
            {
                if (value != null)
                {
                    currentInterlocutor = value;
                    ConnectedUsers.Single(i => i.Name == currentInterlocutor.Name).HasSentANewMessage = false;
                    CurrentMsgList.Clear();
                    var list = new List<Message>();
                    if (client.ExchangedMessages.TryGetValue(currentInterlocutor.Name, out list))
                    {
                        //list.ForEach(x => { CurrentMsgList.Add(new ObservableMessage { Body = x.Body, Target = x.Target }); });
                        //list.ForEach(x => { CurrentMsgList.Add(new Message { Body = x.Body, Target = x.Target }); });
                        list.ForEach(x => { CurrentMsgList.Add(x); });
                        Console.WriteLine(CurrentMsgList.Count);
                    }
                    Console.WriteLine(currentInterlocutor.Name);
                }
                
            }
        }

        /*public sealed class ObservableMessage : ObservableObject
        {
            private string body;
            public string Body { get { return body; } set { Set(() => Body, ref body, value); } }

            private string target;
            public string Target { get { return target; } set { Set(() => Target, ref target, value); } }
        }*/

        /*private List<ObservableMessage> currentMsgList = new List<ObservableMessage>();
        public List<ObservableMessage> CurrentMsgList { get { return currentMsgList; } set { currentMsgList = value; RaisePropertyChanged(); } } */

        //public ObservableCollection<ObservableMessage> CurrentMsgList { get; set; } = new ObservableCollection<ObservableMessage>();
        public ObservableCollection<Message> CurrentMsgList { get; set; } = new ObservableCollection<Message>();

        private Message currentMsg { get; set; } = new Message();
        public Message CurrentMsg
        {
            get { return currentMsg; }
            set
            {
                if (value.Body != "")
                {
                    currentMsg = value;
                    currentMsg.Sender = Name;
                    currentMsg.Target = CurrentInterlocutor.Name;
                    //currentMsg.Target = "kasra";
                    currentMsg.SendTime = DateTime.Now.ToShortDateString();
                    Console.WriteLine("changed current Msg ? : " + value.Body + " And Target is : " + currentMsg.Target);
                    var list = new List<Message>();
                    if(client.ExchangedMessages.TryGetValue(CurrentInterlocutor.Name, out list))
                    {
                        list.Add(currentMsg);
                    }
                    CurrentMsgList.Add(new Message { Body = value.Body, Target = value.Target });
                    client.MessagesToSend.Enqueue(currentMsg);
                    
                }


            }
        }

       
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

            BindingOperations.EnableCollectionSynchronization(ConnectedUsers, verrou);
            BindingOperations.EnableCollectionSynchronization(CurrentMsgList, verrou2);

            Name = "Igor";
            WelcomeMsg = $"Bienvenue sur PtiChat, {Name}";


            client.NewConnectedCustomer += Client_NewConnectedCustomer;
            /*client.OnNewConnectedCustomer("Jack");
            client.OnNewConnectedCustomer("Averell");
            client.OnNewConnectedCustomer("Joe");*/

            client.ReceivedMessage += Client_ReceivedMessage;
            /*client.OnReceivedMessage("Message pour Averell", "Averell");
            client.OnReceivedMessage("Message pour Joe", "Joe");
            client.OnReceivedMessage("Message pour Averell 2", "Averell");*/

            client.StartChat();

        }

        private void Client_ReceivedMessage(object sender, Client.MessageEventArgs e)
        {
            
            if (CurrentInterlocutor.Name == e.Target)
            {
                CurrentMsgList.Add(new Message { Body = e.MessageContent, Target = e.Target });
            } else
            {
               ConnectedUsers.Single(i => i.Name == e.Target).HasSentANewMessage = true;
            }

        }

        private void Client_NewConnectedCustomer(object sender, Client.UserEventArgs e)
        {
            ConnectedUsers.Clear();
            client.ConnectedUsers.ForEach(x => { ConnectedUsers.Add(new ObservableClient { Name = x, HasSentANewMessage = true }); });
            Console.WriteLine(ConnectedUsers.Count);
        }

        }
}
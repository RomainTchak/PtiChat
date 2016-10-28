using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

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
        public Dictionary<string, List<Message>> ConversationMsg { get; set; } = new Dictionary<string, List<Message>>();
        public ConcurrentQueue<Message> ToSend { get; set; } = new ConcurrentQueue<Message>();

        public sealed class ObservableClient : ObservableObject
        {
            private string name;
            public string Name { get { return name; } set { Set(() => Name, ref name, value); } }

            private bool hasSentANewMessage = true;
            public bool HasSentANewMessage { get { return hasSentANewMessage; } set { Set(() => HasSentANewMessage, ref hasSentANewMessage, value); } }
        }

        public string Name { get; private set; }
        public string WelcomeMsg { get; private set; }

        public List<ObservableClient> ConnectedUsers { get; set; } = new List<ObservableClient>();
        private ObservableClient currentInterlocutor = new ObservableClient();
        public ObservableClient CurrentInterlocutor
        {
            get
            {
                return currentInterlocutor;
            }
            set
            {
                currentInterlocutor = value;
                ConnectedUsers.Find(i => i.Name == currentInterlocutor.Name).HasSentANewMessage = false;
            }
        }

        public sealed class ObservableMessage : ObservableObject
        {
            private string body;
            public string Body { get { return body; } set { Set(() => Body, ref body, value); } }

            private string sender;
            public string Sender { get { return sender; } set { Set(() => Sender, ref sender, value); } }
        }

        public List<ObservableMessage> CurrentMsgList { get; set; } = new List<ObservableMessage>();
        private Message currentMsg { get; set; } = new Message();
        public Message CurrentMsg
        {
            get { return currentMsg; }
            set
            {
                currentMsg = value;
                //RaisePropertyChanged();
                //Console.WriteLine(CurrentMsg.Body);
                Console.WriteLine("changed current Msg ?");
            }
        }

        //private string currentMsgBody;
        //public string CurrentMsgBody { get { return currentMsgBody; } set { currentMsgBody = value; Console.WriteLine("changed current message body"); CurrentMsg.Body = value; Console.WriteLine(CurrentMsg.Body); } }

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

            

            Name = "John";
            WelcomeMsg = $"Bienvenue sur PtiChat, {Name}";
            Client client = new Client();

            client.ReceivedMessage += Client_ReceivedMessage;
            client.OnReceivedMessage();
            client.OnReceivedMessage();

            client.NewConnectedCustomer += Client_NewConnectedCustomer;
            client.OnNewConnectedCustomer("Jack");
            client.OnNewConnectedCustomer("Averell");
            client.OnNewConnectedCustomer("Joe");

            //Console.WriteLine(CurrentMsgList[0]);
            //Console.WriteLine(CurrentMsgList.CurrentMsg.Body);

            //CurrentMsg.PrpertyChanged += MainViewModel_PrpertyChanged;

        }

        /*private void MainViewModel_PrpertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ExecuteSendMessageCommand();
        }*/


        private void Client_ReceivedMessage(object sender, Client.MessageEventArgs e)
        {
            CurrentMsgList.Add(new ObservableMessage { Body = e.MessageContent });
        }

        private void Client_NewConnectedCustomer(object sender, Client.UserEventArgs e)
        {
            ConnectedUsers.Add(new ObservableClient { Name = e.UserName });
        }

        /*public void ExecuteSendMessageCommand () {
            Console.WriteLine($"Le message courant est : {CurrentMsg.Body}");
            if (CurrentMsg.Body != "")
            {
                CurrentMsgList.Add(CurrentMsg.Body);
            }
            CurrentMsg.Body = "";
        }*/



    }
}
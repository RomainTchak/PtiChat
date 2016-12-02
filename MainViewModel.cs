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
        //public bool IsFromMe { get; set; }

        public sealed class ObservableClient : ObservableObject
        {
            private string name;
            public string Name { get { return name; } set { name = value; } }

            private bool hasSentANewMessage = false;
            public bool HasSentANewMessage { get { return hasSentANewMessage; } set { Set(() => HasSentANewMessage, ref hasSentANewMessage, value); } }

        }

        public sealed class ObservableMessage : ObservableObject
        {
            private bool isFromMe;
            public bool IsFromMe { get { return isFromMe; } set { Set(() => IsFromMe, ref isFromMe, value); } }

            private string body;
            public string Body { get { return body; } set { Set(() => Body, ref body, value); } }

            private string target;
            public string Target { get { return target; } set { Set(() => Target, ref target, value); } }

        }

        private ObservableCollection<ObservableClient> connectedUsers = new ObservableCollection<ObservableClient>();
        public ObservableCollection<ObservableClient> ConnectedUsers { get { return connectedUsers; } set { Console.WriteLine("setting ConnectedUsers"); connectedUsers = value; } }

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
                    //****** LE CODE DE TCHAK **************************************************************
                    // currentInterlocutor = value;
                    // ConnectedUsers.Single(i => i.Name == currentInterlocutor.Name).HasSentANewMessage = false;
                    // CurrentMsgList.Clear();
                    // var list = new List<Message>();
                    // /*if (client.ExchangedMessages.TryGetValue(currentInterlocutor.Name, out list))
                    // {
                    //     list.ForEach(x => { CurrentMsgList.Add(x); });
                    //     Console.WriteLine(CurrentMsgList.Count);
                    // }*/
                    // Console.WriteLine(currentInterlocutor.Name);
                    //*****************************************************************************

                    // ******* MON CODE **************************************
                    currentInterlocutor = value;
                    ConnectedUsers.Single(i => i.Name == currentInterlocutor.Name).HasSentANewMessage = false;
                    CurrentMsgList.Clear();
                    var list = new List<Message>();
                    if (client.ExchangedMessages.TryGetValue(currentInterlocutor.Name, out list))
                    {
                        //list.ForEach(x => { CurrentMsgList.Add(new ObservableMessage { Body = x.Body, Target = x.Target }); });
                        //list.ForEach(x => { CurrentMsgList.Add(new Message { Body = x.Body, Target = x.Target }); });

                        //On affiche chaque message de la conversation précédé par le nom de l'expéditeur
                        list.ForEach(x => { CurrentMsgList.Add(new ObservableMessage { Body = x.Body, Target = x.Target, IsFromMe = (x.Sender == client.Username) }); } );
                        Console.WriteLine(CurrentMsgList.Count);
                    }
                    Console.WriteLine(currentInterlocutor.Name);
                    // *************************************************************

                }

            }
        }

        public ObservableCollection<ObservableMessage> CurrentMsgList { get; set; } = new ObservableCollection<ObservableMessage>();

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

                    // ************* TCHAK ************************************************
                    // /*if(client.ExchangedMessages.TryGetValue(CurrentInterlocutor.Name, out list)) {
                    //     list.Add(currentMsg);
                    // }*/
                    // ==================================================================

                    // ********** IGOR *******************************************************
                    if (client.ExchangedMessages.TryGetValue(CurrentInterlocutor.Name, out list))
                    {
                        list.Add(currentMsg);
                    }
                    // ==========================================================================

                    CurrentMsgList.Add(new ObservableMessage { Body = value.Body, Target = value.Target, IsFromMe = true });
                    //CurrentMsgList.Add(currentMsg);
                    client.MessagesToSend.Enqueue(currentMsg);

                    //******* IGOR ********************************************
                    client.StoreMessage(currentMsg.Target, currentMsg);
                    // ===============================================================

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

            /*MessageBoxResult result = MessageBox.Show("Do you want to close this window?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                //Application.Current.Shutdown();
            }*/

            StartAuthentification();

            BindingOperations.EnableCollectionSynchronization(ConnectedUsers, verrou);
            BindingOperations.EnableCollectionSynchronization(CurrentMsgList, verrou2);

            //Name = "Choroncos";
            //WelcomeMsg = $"Bienvenue sur PtiChat, {Name}";


            client.NewConnectedCustomer += Client_NewConnectedCustomer;
            //var list = new List<string>(); list.Add("Joe"); list.Add("Jack"); list.Add("Averell");
            /*client.ConnectedUsers.Add("Joe");
            client.ConnectedUsers.Add("Jack");
            client.ConnectedUsers.Add("Averell");*/

            //client.ConnectedUsers = list;

            client.ReceivedMessage += Client_ReceivedMessage;
            /*client.OnReceivedMessage("Message pour Averell", "Averell");
            client.OnReceivedMessage("Message pour Joe", "Joe");
            client.OnReceivedMessage("Message pour Averell 2", "Averell");*/

            client.StartChat();

        }

        private void StartAuthentification()
        {
            var dialog = new MyDialog();
            if (dialog.ShowDialog() == true)
            {
                if (dialog.SignIn)
                {
                    if (client.TryAuthentification(dialog.SignIn, dialog.SignInId_Result, dialog.SignInPw_Result))
                    {
                        Name = dialog.SignInId_Result;
                        WelcomeMsg = $"Bienvenue sur PtiChat, {Name}";
                    }
                    else
                    {
                        MessageBox.Show("Nous ne reconnaissons pas ces identifiants, désolé.");
                        StartAuthentification();
                    }
                }
                else
                {
                    if (client.TryAuthentification(dialog.SignIn, dialog.SignInId_Result, dialog.SignInPw_Result))
                    {
                        Name = dialog.SignInId_Result;
                        WelcomeMsg = $"Bienvenue sur PtiChat, {Name}";
                    }
                    else
                    {
                        MessageBox.Show("L'identifiant est déjà pris par un autre utilisateur.");
                        StartAuthentification();
                    }
                }

            }
        }

        private void Client_ReceivedMessage(object sender, Client.MessageEventArgs e)
        {

            //********** TCHAK ****************************************************
            // if (CurrentInterlocutor.Name == e.Target)
            // {
            //     CurrentMsgList.Add(new ObservableMessage { Body = e.MessageContent, Target = e.Target, IsFromMe = false });
            // }
            // else
            // {
            //    ConnectedUsers.Single(i => i.Name == e.Target).HasSentANewMessage = true;
            // }
            // =============================================================================

            // ******** IGOR ***********************************************************
            if (CurrentInterlocutor.Name == e.Interlocutor)
            {
                //On a reçu un message, donc on fait précéder le corps du message du nom de l'expéditeur
                string senderToDisplay = (e.Sender != client.Username) ? e.Interlocutor : "Vous";
                CurrentMsgList.Add(new ObservableMessage { Body = e.MessageContent, Target = e.Interlocutor, IsFromMe = (client.Username == e.Sender) });
            }
            

            else
            {
                ConnectedUsers.Single(i => i.Name == e.Interlocutor).HasSentANewMessage = true;
            }
            // ==============================================================================
        }

        private void Client_NewConnectedCustomer(object sender, Client.UserEventArgs e)
        {
            ConnectedUsers.Clear();
            client.ConnectedUsers.ForEach(x => { ConnectedUsers.Add(new ObservableClient { Name = x, HasSentANewMessage = true }); });
            Console.WriteLine(ConnectedUsers.Count);
            /*client.ConnectedUsers.ForEach(
                x => {
                   if (!ConnectedUsers.Any(i => { return (i.Name == x); } ))
                    {
                        ConnectedUsers.Add(new ObservableClient { Name = x, HasSentANewMessage = true });
                    }
                }
            );     

            foreach (ObservableClient i in ConnectedUsers)
            {
                if (!client.ConnectedUsers.Contains(i.Name))
                {
                    ConnectedUsers.Remove(i);
                }
            }
        
            Console.WriteLine(ConnectedUsers.Count);*/
        }



    }
}
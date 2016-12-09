using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

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
        public string Name { get; private set; } // utilisé dans WelcomeMsg
        public string WelcomeMsg { get; private set; } // bindé avec le label de message de bienvenue
        private readonly object verrou = new object(); // verrou pour actualiser la liste des utilisateurs connectés
        private readonly object verrou2 = new object(); // verrou pour actualiser la liste des messages
        public Client client { get; set; } = new Client(); //instanciation de la classe client qui contient toutes les méthodes pour communiquer avec le serveur.

        /// <summary>
        /// Equivalent de la classe Client mais pour l'afichage
        /// </summary>
        public sealed class ObservableClient : ObservableObject
        {
            private string name;
            public string Name { get { return name; } set { name = value; } }

            // Gère l'affichage des notifications.
            private bool hasSentANewMessage = false;
            public bool HasSentANewMessage { get { return hasSentANewMessage; } set { Set(() => HasSentANewMessage, ref hasSentANewMessage, value); } }
        }

        /// <summary>
        /// Equivalent de la classe Message mais pour l'afichage
        /// </summary>
        public sealed class ObservableMessage : ObservableObject
        {
            // Booléen de provenance du message pour l'affichage à gauche ou à droite.
            private bool isFromMe;
            public bool IsFromMe { get { return isFromMe; } set { Set(() => IsFromMe, ref isFromMe, value); } }

            private string body;
            public string Body { get { return body; } set { Set(() => Body, ref body, value); } }

            private string target;
            public string Target { get { return target; } set { Set(() => Target, ref target, value); } }

        }

        //Affichage des utilisateurs connectés, objet bindé avec la listView des utilisateurs.
        private ObservableCollection<ObservableClient> connectedUsers = new ObservableCollection<ObservableClient>();
        public ObservableCollection<ObservableClient> ConnectedUsers { get { return connectedUsers; } set { connectedUsers = value; } }

        //Gestion de l'interlocuteur courant, objet bindé avec l'item sélectionné de la listView des utilisateurs.
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
                    ConnectedUsers.Single(i => i.Name == currentInterlocutor.Name).HasSentANewMessage = false; // La notification de nouveau message est retirée.
                    CurrentMsgList.Clear();
                    var list = new List<Message>();
                    if (client.ExchangedMessages.TryGetValue(currentInterlocutor.Name, out list))
                    {
                        //On affiche chaque message de la conversation précédé par le nom de l'expéditeur
                        list.ForEach(x => { CurrentMsgList.Add(new ObservableMessage { Body = x.Body, Target = x.Target, IsFromMe = (x.Sender == client.Username) }); });
                    }
                }

            }
        }

        //Affichage de la liste des messages avec l'interlocuteur courant, objet bindé avec la listView des messages.
        public ObservableCollection<ObservableMessage> CurrentMsgList { get; set; } = new ObservableCollection<ObservableMessage>();

        //Gestion du message courant, objet bindé avec le texte de la barre de message moyennant la conversion avec StringToMessageConverter
        private Message currentMsg { get; set; } = new Message();
        public Message CurrentMsg
        {
            get { return currentMsg; }
            set
            {
                if (value.Body != "")
                {
                    if (String.IsNullOrEmpty(CurrentInterlocutor.Name))
                    {
                        MessageBox.Show("Veuillez sélectionner un interlocuteur. S'il n'y en a pas, c'est que vous êtes tous seul.");
                    }
                    else
                    {
                        currentMsg = value;
                        currentMsg.Sender = Name;
                        currentMsg.Target = CurrentInterlocutor.Name;
                        currentMsg.SendTime = DateTime.Now.ToShortDateString();
                        var list = new List<Message>();

                        if (client.ExchangedMessages.TryGetValue(CurrentInterlocutor.Name, out list))
                        {
                            list.Add(currentMsg); //On ajoute le message à la liste dans le dictionnaire correspondant à la conversation avec l'interlocuteur.
                        }

                        CurrentMsgList.Add(new ObservableMessage { Body = value.Body, Target = value.Target, IsFromMe = true }); // Ajout à la liste d'affichage
                        client.MessagesToSend.Enqueue(currentMsg); // Ajout à la liste de message à traiter par le serveur
                        client.StoreMessage(currentMsg.Target, currentMsg); // AJout à la base de donnée
                    }
                }
            }
        }



        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            StartAuthentification(); //Lance la fenêtre d'authentification

            BindingOperations.EnableCollectionSynchronization(ConnectedUsers, verrou);
            BindingOperations.EnableCollectionSynchronization(CurrentMsgList, verrou2);

            //Connection des événements client aux méthodes à exécuter
            client.NewConnectedCustomer += Client_NewConnectedCustomer;
            client.ReceivedMessage += Client_ReceivedMessage;

            client.StartChat(); //Connecte le client avec le serveur.
        }

        /// <summary>
        /// Ouvre une fenêtre qui gère toutes les conditions liées à l'authentification 
        /// </summary>
        private void StartAuthentification()
        {
            var dialog = new MyDialog();
            if (dialog.ShowDialog() == true)
            {
                if (dialog.Exit)
                {
                    MessageBox.Show("A la prochaine !");
                    Application.Current.Shutdown();
                }
                else if (dialog.SignIn)
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
                    if (client.TryAuthentification(dialog.SignIn, dialog.SignUpId_Result, dialog.SignUpPw_Result))
                    {
                        Name = dialog.SignUpId_Result;
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

        /// <summary>
        /// Méthode appelée quand l'utilisateur reçoit un message d'un interlocuteur
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_ReceivedMessage(object sender, Client.MessageEventArgs e)
        {
            if (CurrentInterlocutor.Name == e.Interlocutor) // On ajoute le message à la liste de message si l'interlocuteur est l'interlocuteur courant.
            {
                CurrentMsgList.Add(new ObservableMessage { Body = e.MessageContent, Target = e.Interlocutor, IsFromMe = client.Username == e.Sender });
            }
            else // Sinon on affiche une notification de nouveau message dans la liste des interlocuteurs.
            {
                ConnectedUsers.Single(i => i.Name == e.Interlocutor).HasSentANewMessage = true;
            }
        }

        /// <summary>
        /// Méthode appelée quand un interlocuteur se connecte ou se déconnecte.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_NewConnectedCustomer(object sender, Client.UserEventArgs e)
        {
            if (ConnectedUsers.Count == 0) //lors de la connetion
            {
                client.ConnectedUsers.ForEach(x => { ConnectedUsers.Add(new ObservableClient { Name = x, HasSentANewMessage = true }); });
            }
            else
            {
                bool newConnection = false;
                client.ConnectedUsers.ForEach(
                x =>
                {
                    if (!ConnectedUsers.Any(i => { return (i.Name == x); }))
                    {
                        ConnectedUsers.Add(new ObservableClient { Name = x, HasSentANewMessage = true });
                        newConnection = true;
                    }
                });

                if (!newConnection)
                {
                    foreach (ObservableClient i in ConnectedUsers)
                    {
                        if (!client.ConnectedUsers.Contains(i.Name))
                        {
                            ConnectedUsers.Remove(i);
                        }
                    }
                }
            }
        }
    }
}
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
        public string Name { get; private set; } // utilis� dans WelcomeMsg
        public string WelcomeMsg { get; private set; } // bind� avec le label de message de bienvenue
        private readonly object verrou = new object(); // verrou pour actualiser la liste des utilisateurs connect�s
        private readonly object verrou2 = new object(); // verrou pour actualiser la liste des messages
        public Client client { get; set; } = new Client(); //instanciation de la classe client qui contient toutes les m�thodes pour communiquer avec le serveur.

        /// <summary>
        /// Equivalent de la classe Client mais pour l'afichage
        /// </summary>
        public sealed class ObservableClient : ObservableObject
        {
            private string name;
            public string Name { get { return name; } set { name = value; } }

            // G�re l'affichage des notifications.
            private bool hasSentANewMessage = false;
            public bool HasSentANewMessage { get { return hasSentANewMessage; } set { Set(() => HasSentANewMessage, ref hasSentANewMessage, value); } }
        }

        /// <summary>
        /// Equivalent de la classe Message mais pour l'afichage
        /// </summary>
        public sealed class ObservableMessage : ObservableObject
        {
            // Bool�en de provenance du message pour l'affichage � gauche ou � droite.
            private bool isFromMe;
            public bool IsFromMe { get { return isFromMe; } set { Set(() => IsFromMe, ref isFromMe, value); } }

            private string body;
            public string Body { get { return body; } set { Set(() => Body, ref body, value); } }

            private string target;
            public string Target { get { return target; } set { Set(() => Target, ref target, value); } }

        }

        //Affichage des utilisateurs connect�s, objet bind� avec la listView des utilisateurs.
        private ObservableCollection<ObservableClient> connectedUsers = new ObservableCollection<ObservableClient>();
        public ObservableCollection<ObservableClient> ConnectedUsers { get { return connectedUsers; } set { connectedUsers = value; } }

        //Gestion de l'interlocuteur courant, objet bind� avec l'item s�lectionn� de la listView des utilisateurs.
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
                    ConnectedUsers.Single(i => i.Name == currentInterlocutor.Name).HasSentANewMessage = false; // La notification de nouveau message est retir�e.
                    CurrentMsgList.Clear();
                    var list = new List<Message>();
                    if (client.ExchangedMessages.TryGetValue(currentInterlocutor.Name, out list))
                    {
                        //On affiche chaque message de la conversation pr�c�d� par le nom de l'exp�diteur
                        list.ForEach(x => { CurrentMsgList.Add(new ObservableMessage { Body = x.Body, Target = x.Target, IsFromMe = (x.Sender == client.Username) }); });
                    }
                }

            }
        }

        //Affichage de la liste des messages avec l'interlocuteur courant, objet bind� avec la listView des messages.
        public ObservableCollection<ObservableMessage> CurrentMsgList { get; set; } = new ObservableCollection<ObservableMessage>();

        //Gestion du message courant, objet bind� avec le texte de la barre de message moyennant la conversion avec StringToMessageConverter
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
                        MessageBox.Show("Veuillez s�lectionner un interlocuteur. S'il n'y en a pas, c'est que vous �tes tous seul.");
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
                            list.Add(currentMsg); //On ajoute le message � la liste dans le dictionnaire correspondant � la conversation avec l'interlocuteur.
                        }

                        CurrentMsgList.Add(new ObservableMessage { Body = value.Body, Target = value.Target, IsFromMe = true }); // Ajout � la liste d'affichage
                        client.MessagesToSend.Enqueue(currentMsg); // Ajout � la liste de message � traiter par le serveur
                        client.StoreMessage(currentMsg.Target, currentMsg); // AJout � la base de donn�e
                    }
                }
            }
        }



        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            StartAuthentification(); //Lance la fen�tre d'authentification

            BindingOperations.EnableCollectionSynchronization(ConnectedUsers, verrou);
            BindingOperations.EnableCollectionSynchronization(CurrentMsgList, verrou2);

            //Connection des �v�nements client aux m�thodes � ex�cuter
            client.NewConnectedCustomer += Client_NewConnectedCustomer;
            client.ReceivedMessage += Client_ReceivedMessage;

            client.StartChat(); //Connecte le client avec le serveur.
        }

        /// <summary>
        /// Ouvre une fen�tre qui g�re toutes les conditions li�es � l'authentification 
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
                        MessageBox.Show("Nous ne reconnaissons pas ces identifiants, d�sol�.");
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
                        MessageBox.Show("L'identifiant est d�j� pris par un autre utilisateur.");
                        StartAuthentification();
                    }
                }
            }
        }

        /// <summary>
        /// M�thode appel�e quand l'utilisateur re�oit un message d'un interlocuteur
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_ReceivedMessage(object sender, Client.MessageEventArgs e)
        {
            if (CurrentInterlocutor.Name == e.Interlocutor) // On ajoute le message � la liste de message si l'interlocuteur est l'interlocuteur courant.
            {
                CurrentMsgList.Add(new ObservableMessage { Body = e.MessageContent, Target = e.Interlocutor, IsFromMe = client.Username == e.Sender });
            }
            else // Sinon on affiche une notification de nouveau message dans la liste des interlocuteurs.
            {
                ConnectedUsers.Single(i => i.Name == e.Interlocutor).HasSentANewMessage = true;
            }
        }

        /// <summary>
        /// M�thode appel�e quand un interlocuteur se connecte ou se d�connecte.
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
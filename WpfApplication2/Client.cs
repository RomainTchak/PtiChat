using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Net.NetworkInformation;

namespace WpfApplication2
{

    public class Client
    {
        public ConcurrentDictionary<string, List<Message>> ExchangedMessages { get; set; } = new ConcurrentDictionary<string, List<Message>>();
        public ConcurrentQueue<Message> MessagesToSend { get; set; } = new ConcurrentQueue<Message>();
        private List<string> connectedUsers = new List<string>();
        public List<string> ConnectedUsers { get { return connectedUsers; } set { connectedUsers = value; OnNewConnectedCustomer(connectedUsers); } }
        public string Username { get; set; }
        public string Password { get; set; }
        public TcpClient SocClient;
        public NetworkStream NS;
        public BinaryReader BR;
        public BinaryWriter BW;
        private object myLock = new object();
        private object connectionLock = new object();
        public bool isConnected = false;
        public string HostAddress = "ec2-35-162-78-174.us-west-2.compute.amazonaws.com";
        //public string HostAddress = "localhost";
        Exception WriteException = new Exception("Write Exception");
        Exception ConnectionException = new Exception("Connection Exception");
        private string PathToAppDirectory = "C:\\PtiChat";

        public event EventHandler<MessageEventArgs> ReceivedMessage;
        public event EventHandler<UserEventArgs> NewConnectedCustomer;

        /// <summary>
        /// Cette classe permet de définir les paramètres de l'événement "la liste de client a été mise à jour"
        /// </summary>
        public sealed class UserEventArgs : EventArgs
        {
            public List<string> UserNames { get; private set; } //la liste des noms des clients connectés

            public UserEventArgs(List<string> usernames)
            {
                UserNames = usernames;
            }
        }

        /// <summary>
        /// Cette classe permet de définir les paramètres de l'événement "il y a un nouveau message à afficher"
        /// </summary>
        public sealed class MessageEventArgs : EventArgs
        {
            public string MessageContent { get; private set; } //le corps du message à afficher
            public string Interlocutor { get; private set; } // le nom de la personne qui nous envoie le message ou à qui on l'envoie
            public string Sender { get; private set; } // le nom de la personne qui envoie le message (qui est toujours égal à l'interlocuteur
                                                       // sauf s'il s'agit d'un message qu'on recharge dans un historique de messages, auquel cas on peut être l'expéditeur

            public MessageEventArgs(string messageContent, string interlocutor, string sender)
            {
                MessageContent = messageContent;
                Interlocutor = interlocutor;
                Sender = sender;
            }
        }

        /// <summary>
        /// Déclenche l'événement "la liste des utilisateurs connectés a changé" pour l'interface
        /// </summary>
        /// <param name="list"> la nouvelle liste des noms d'utilisateurs connectés </param>
        public void OnNewConnectedCustomer(List<string> list)
        {
            UserEventArgs UserEA = new UserEventArgs(list);
            NewConnectedCustomer?.Invoke(this, UserEA);
        }

        /// <summary>
        /// Déclenche l'événement "il y a un nouveau message à afficher" pour l'interface
        /// </summary>
        /// <param name="msg"> le corps du message </param>
        /// <param name="interlocutor"> le nom de l'utilisateur (autre que nous-mêmes) concerné par ce message </param>
        /// <param name="sender"> l'expéditeur du message </param>
        public void OnReceivedMessage(string msg, string interlocutor, string sender)
        {

            var list = new List<Message>();
            if (ExchangedMessages.TryGetValue(interlocutor, out list))
            {
                //Si l'interlocuteur est le sender, ca veut dire que le message a été envoyé par un autre client
                list.Add(new Message { Body = msg, Sender = (interlocutor == sender) ? interlocutor : this.Username, Target = (interlocutor == sender) ? this.Username : interlocutor });
            }
            MessageEventArgs MessageEA = new MessageEventArgs(msg, interlocutor, sender);
            ReceivedMessage?.Invoke(this, MessageEA);

        }

        public Client()
        {

            SocClient = new TcpClient();
            isConnected = false;

        }


        /// <summary>
        /// Envoie un objet Message sur le Stream à destination du serveur
        /// </summary>
        /// <param name="msg"> l'objet message à envoyer </param>
        void Envoyer(Message msg)
        {
            lock (myLock)
            {
                //On envoie successivement les différents éléments du Message sur le stream
                BW.Write((string)msg.Sender);
                BW.Write((string)msg.Target);
                BW.Write((string)msg.Body);
                BW.Write((int)msg.FileSize);
                if (msg.FileSize != 0)
                {
                    //Si il y a un fichier à envoyer
                    BW.Write(msg.Attachment, 0, msg.Attachment.Length);
                }
                BW.Write((string)msg.FileName);
                BW.Write((string)msg.SendTime);
                BW.Write((string)msg.ReceiveTime);

            }
        }

        /// <summary>
        /// Lit le stream qui relie le client au serveur, et, lorsqu'un message est reçu, récupère les différents attributs
        /// de ce message
        /// </summary>
        /// <returns> l'objet Message qui vient d'être lu sur le stream </returns>
        Message Recevoir()
        {
            Message ReceivedMessage = new Message();

            //La première chaîne de caractères à lire correspond à l'expéditeur
            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream


            }
            ReceivedMessage.Sender = BR.ReadString();

            //On lit ensuite le nom du destinataire
            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.Target = BR.ReadString();

            // On lit le corps du message
            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.Body = BR.ReadString();

            // On lit la taille de la pièce jointe
            while (!(NS.DataAvailable))
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.FileSize = BR.ReadInt32();

            // On ne traite la pièce jointe que si elle a une taille non nulle
            if (ReceivedMessage.FileSize != 0)
            {
                //On récupère le tableau d'octets représentant le fichier reçu. On le transformera en fichier plus bas
                while (!NS.DataAvailable)
                {
                    //On attend qu'il y ait quelque chose sur le stream
                }
                ReceivedMessage.Attachment = BR.ReadBytes(ReceivedMessage.FileSize);

            }

            //On lit le nom de la pièce jointe
            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.FileName = BR.ReadString();

            //On convertit le tableau d'octets de la pièce jointe en fichier
            if (ReceivedMessage.FileSize != 0)
            {
                try
                {
                    //On transforme le tableau d'octets en fichier sous le nom indiqué par l'attribut filename
                    string pathToFile = PathToAppDirectory + "\\" + ReceivedMessage.FileName;
                    Stream file = File.OpenWrite(pathToFile);
                    file.Write(ReceivedMessage.Attachment, 0, ReceivedMessage.FileSize);

                    file.Close();
                }
                catch (System.IO.IOException)
                {
                    try
                    {
                        // Il y a eu une erreur, le On essaie de renommer le fichier
                        string extension = ReceivedMessage.FileName.Substring(ReceivedMessage.FileName.LastIndexOf('.'));
                        string RealName = ReceivedMessage.FileName.Substring(0, ReceivedMessage.FileName.LastIndexOf('.'));
                        ReceivedMessage.FileName = RealName + " copy" + extension;
                        string pathToFile = "C:\\PtiChat\\" + ReceivedMessage.FileName;
                        Stream file = File.OpenWrite(pathToFile);
                        file.Write(ReceivedMessage.Attachment, 0, ReceivedMessage.FileSize);
                    }
                    catch (Exception)
                    {
                        //Debug.WriteLine(FileEx.Message);
                    }
                }

                ReceivedMessage.Body = $"{ReceivedMessage.Body} Pièce jointe : {ReceivedMessage.FileName}";
            }
            ReceivedMessage.Attachment = new byte[0];
            //On vide le tableau Attachment afin de libérer de l'espace sur le mémoire 

            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.SendTime = BR.ReadString();

            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.ReceiveTime = BR.ReadString();

            //On récupère la date de réception du message
            ReceivedMessage.ReceiveTime = DateTime.Now.ToString();

            return ReceivedMessage;
        }


        /// <summary>
        /// Gère l'envoi de messages vers le serveur
        /// </summary>
        void EnvoiMessage()
        {
            bool success;

            Message msgToSend = new Message();

            while (true)
            {
                while (MessagesToSend.IsEmpty)
                {
                    //attendre

                }

                //On récupère le premier message dans la file sans le supprimer
                MessagesToSend.TryPeek(out msgToSend);

                //On regarde si le message contient une PJ
                if (msgToSend.Body.Contains("@File"))
                {
                    string filePath = msgToSend.Body.Substring(msgToSend.Body.IndexOf("@File") + 5); //Le chemin du fichier

                    //On enlève du corps du message tout ce qui arrive après @File
                    msgToSend.Body = msgToSend.Body.Remove(msgToSend.Body.IndexOf("@File"));

                    //On va lire ce fichier et on le convertit en tableau de bytes pour remplir l'attribut Attachment de l'objet Message
                    Stream file = File.OpenRead(filePath);
                    msgToSend.Attachment = new byte[file.Length];
                    file.Read(msgToSend.Attachment, 0, (int)file.Length);
                    msgToSend.FileSize = msgToSend.Attachment.Length;
                    msgToSend.FileName = filePath.Substring(filePath.LastIndexOf("\\") + 1); //Le nom du fichier
                }
                else
                {
                    //On envoie pas de fichier donc on envoie un tableau vide
                    msgToSend.FileSize = 0;
                    msgToSend.Attachment = new byte[0];
                    msgToSend.FileName = "";

                }
                msgToSend.Sender = Username;
                msgToSend.SendTime = DateTime.Now.ToString();


                //On essaie d'envoyer le message
                success = true;
                try
                {

                    try
                    {
                        Envoyer(msgToSend);
                    }
                    catch (Exception)
                    {
                        throw WriteException;
                    }

                    if (!this.isConnected)
                    {
                        throw ConnectionException;
                    }


                }
                catch (Exception)
                {
                    //L'envoi a échoué, la connexion a été perdue   
                    success = false;

                    while (!this.isConnected)
                    {
                        //ne rien faire
                        //On attend que la connexion soit rétablie
                    }

                }

                //on ne dequeue le message à envoyer que si l'envoi a réussi
                if (success)
                {
                    MessagesToSend.TryDequeue(out msgToSend);
                }

            }
        }



        /// <summary>
        /// Gère la réception des messages provenant du serveur
        /// </summary>
        void ReceptionMessage()
        {
            List<Message> MsgList = new List<Message>();
            string ClientToUpdate;


            while (true)
            {

                //On essaie de recevoir les messages
                try
                {

                    while (!NS.DataAvailable)
                    {
                        //On attend qu'il y ait quelque chose sur le Stream

                        //On vérifie qu'on est toujours connecté à Internet
                        if (!this.isConnected)
                        {
                            throw ConnectionException;
                        }

                    }


                    Message messageRecu = this.Recevoir();

                    if (messageRecu.Sender == "Server")
                    {
                        if (messageRecu.Body.Contains("!Cd"))
                        {
                            //Le message du serveur commence par !Cd, cela veut dire qu'un nouvel utilisateur s'est connecté

                            //Le nom du client qui vient de se connecter est situé en position 4
                            ClientToUpdate = messageRecu.Body.Substring(4);

                            //On crée l'événement annoncant la connexion d'un nouveau client
                            //OnNewConnectedClient(ClientToUpdate);

                            //On initialise la conversation pour ce nouvel utilisateur
                            ExchangedMessages.TryAdd(ClientToUpdate, new List<Message>());

                        }

                        else if (messageRecu.Body.Contains("!Dd"))
                        {
                            //Le message du serveur commence par !Dd, cela veut dire qu'un utilisateur s'est déconnecté

                            //Le nom du client qui vient de se déconnecter est situé en position 4
                            ClientToUpdate = messageRecu.Body.Substring(4);

                            //On crée l'événement annoncant la déconnexion d'un client
                            //OnNewDisconnectedClient(ClientToUpdate);

                            //On supprime la conversation pour cet utilisateur
                            ExchangedMessages.TryRemove(ClientToUpdate, out MsgList);
                        }

                        else
                        {
                            List<string> connectedPeople = messageRecu.Body.Split(',').ToList();


                            //On met à jour la liste des utilisateurs connectés avec cette nouvelle liste reçue du serveur
                            ConnectedUsers = string.IsNullOrEmpty(connectedPeople[0]) ? new List<string>() : connectedPeople;

                            //On supprime les conversations correspondant aux clients qui ne sont plus connectés
                            foreach (string user in ExchangedMessages.Keys)
                            {

                                if (!ConnectedUsers.Contains(user))
                                {
                                    ExchangedMessages.TryRemove(user, out MsgList);
                                }
                            }

                            foreach (string user in connectedPeople)
                            {
                                //on rajoute la conversation correspondant à chaque nouvel utilisateur dans le dictionnaire
                                //ExchangedMessages et on recharge l'historique de la conversation avec cet utilisateur
                                if (!(ExchangedMessages.ContainsKey(user)))
                                {
                                    ExchangedMessages.TryAdd(user, new List<Message>());
                                    RetrieveMessages(user);
                                }
                            }

                        }

                    }

                    else
                    {
                        //Le message vient d'un autre client, on l'ajoute dans le dictionnaire de notre conversation avec l'autre client
                        OnReceivedMessage(messageRecu.Body, messageRecu.Sender, messageRecu.Sender);

                        //On stocke ce message dans la base de données de messages
                        StoreMessage(messageRecu.Sender, messageRecu);

                    }
                }
                catch (Exception ex)
                {
                    if (ex.Message == "Connection Exception")
                    {
                        //Si l'erreur de réception est due à une déconnexion, on attend jusqu'à ce que 
                        //la connexion soit rétablie

                        while (!this.isConnected) { }
                    }
                }
            }
        }

        /// <summary>
        /// Enregistre un message dans l'historique d'une conversation (dans un fichier stocké sur l'ordinateur)
        /// </summary>
        /// <param name="interlocutor"> le nom de l'interlocuteur dans la conversation dont fait partie le message </param>
        /// <param name="MessageToStore"> le message à enregistrer dans l'historique </param>
        public void StoreMessage(string interlocutor, Message MessageToStore)
        {
            //On stocke les messages dans un fichier, sous le format expéditeur*destinataire*corps du message
            var Line = new List<string>();
            Line.Add($"{MessageToStore.Sender}*{MessageToStore.Target}*{MessageToStore.Body}");
            File.AppendAllLines($"{PathToAppDirectory}\\Conversation between {this.Username}&{interlocutor}.txt", Line);

        }

        /// <summary>
        /// Récupère l'historique des messages (stocké dans un fichier sur l'ordinateur) échangés avec un utilisateur spécifié
        /// </summary>
        /// <param name="user"> le nom de l'interlocuteur dont on veut rechrger l'historique des messages </param>
        public void RetrieveMessages(string user)
        {
            try
            {
                Message storedMessage = new Message();
                //Chaque ligne du fichier correpond à un message
                List<string> messageHistory = File.ReadLines($"{PathToAppDirectory}\\Conversation between {this.Username}&{user}.txt").ToList();
                foreach (string message in messageHistory)
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        //Les messages sont stocké sous le format expéditeur*destinataire*corps du message
                        List<string> Attributes = message.Split('*').ToList();
                        storedMessage.Sender = Attributes[0];
                        storedMessage.Target = Attributes[1];
                        storedMessage.Body = Attributes[2];
                        string interlocutor;

                        //l'interlocuteur est la personne avec qui on parle dans la conversation
                        if (storedMessage.Sender == this.Username)
                        {
                            //Si l'expéditeur c'est nous, alors l'interlocuteur est le destinataire du message
                            interlocutor = storedMessage.Target;
                        }
                        else
                        {
                            //Si on n'est pas l'expéditeur du message, l'interlocuteur est forcémenent l'expéditeur
                            interlocutor = storedMessage.Sender;
                        }

                        //On informe l'interface qu'elle doit afficher ce message à l'écran
                        OnReceivedMessage(storedMessage.Body, interlocutor, storedMessage.Sender);
                    }

                }



            }
            catch (System.InvalidOperationException) { }

            catch (System.IO.FileNotFoundException) { }
        }

        /// <summary>
        /// Point d'entrée du chat
        /// </summary>
        public void StartChat()
        {
            //S'il n'existe pas encore, on crée le répertoire de l'application
            DirectoryInfo create = Directory.CreateDirectory(PathToAppDirectory);

            this.isConnected = true;
            //On lance les différents threads d'envoi et de réception
            Thread sendingThread = new Thread(() => EnvoiMessage());
            Thread receivingThread = new Thread(() => ReceptionMessage());
            Thread reconnectionThread = new Thread(() => Reconnect());

            sendingThread.Start();
            receivingThread.Start();
            reconnectionThread.Start();


        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="AuthMode"></param>
        /// <param name="pseudo"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool TryAuthentification(bool AuthMode, string pseudo, string password)
        {
            IPAddress ipServer;
            bool success = true;
            Message ConnectMessage = new Message();
            Message ServerResponse = new Message();

            //On essaie de se connecter au serveur puis d'envoyer le message d'identification
            try
            {

                //On se connecte au serveur

                ipServer = Dns.GetHostAddresses(this.HostAddress)[0];

                SocClient.Connect(ipServer, 80);

                //On essaie ensuite d'envoyer le message d'identification au serveur

                this.NS = SocClient.GetStream();
                this.BR = new BinaryReader(NS);
                this.BW = new BinaryWriter(NS);

                this.Username = pseudo;
                this.Password = password;


                ConnectMessage.Sender = this.Username;
                ConnectMessage.Target = "Server";
                ConnectMessage.Body = $"{AuthMode.ToString()}*{this.Username}*{this.Password}";
                ConnectMessage.SendTime = DateTime.Now.ToString();

                //On envoie notre message d'authentification au serveur
                Envoyer(ConnectMessage);

                //On reçoit le message de la part du serveur indiquant si la connexion est réussie ou pas
                ServerResponse = Recevoir();
                if (ServerResponse.Body != "OK")
                {
                    success = false;
                    SocClient.Close();
                    SocClient = new TcpClient();
                }


            }
            catch (Exception)
            {
                //Il y a eu une erreur lors de la tentative de connexion
                success = false;
                SocClient.Close();
                SocClient = new TcpClient();
            }


            return success;
        }

        /// <summary>
        /// Vérifie constamment la connexion à Internet et, le cas échéant, tente de se reconnecter au serveur
        /// </summary>
        public void Reconnect()
        {
            Ping myPing;
            String host;
            byte[] buffer;
            int timeout;
            PingOptions pingOptions;
            PingReply reply;
            int counter = 0; //counter va stocker le nombre de pings consécutifs sans réponse

            while (true)
            {
                //On ping google pour tester la connexion à Internet

                try
                {
                    myPing = new Ping();
                    host = "8.8.8.8";
                    buffer = new byte[32];
                    timeout = 1000;
                    pingOptions = new PingOptions(10000, false);
                    reply = myPing.Send(host, timeout, buffer, pingOptions);
                    counter = 0;
                }
                catch (Exception)
                {
                    //A chaque ping sans réponse, on incrémente counter
                    counter++;

                    //Dès qu'on a plus de 3 pings sans réponse, on considère que la connexion a été perdue
                    if (counter >= 3)
                    {
                        this.isConnected = false;
                        counter = 0;
                    }
                }

                lock (connectionLock)
                {
                    if (!this.isConnected)
                    {

                        //On teste si on a une connexion Internet
                        bool PingSuccessful = false;
                        while (!PingSuccessful)
                        {
                            try
                            {
                                myPing = new Ping();
                                host = "8.8.8.8";
                                buffer = new byte[32];
                                timeout = 1000;
                                pingOptions = new PingOptions(10000, false);
                                reply = myPing.Send(host, timeout, buffer, pingOptions);
                                PingSuccessful = true;

                            }
                            catch (Exception)
                            {
                                PingSuccessful = false;

                            }
                        }

                        //Dès qu'on a récupéré la connexion Internet, on se reconnecte au serveur en mode SignIn

                        SocClient.Close();
                        SocClient = new TcpClient();

                        Thread.Sleep(500);
                        while (!TryAuthentification(true, this.Username, this.Password))
                        {

                        }

                        this.isConnected = true;
                    }
                }
            }
        }

    }
}

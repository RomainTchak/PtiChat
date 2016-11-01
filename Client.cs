using GalaSoft.MvvmLight;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfApplication2.ViewModel;

namespace WpfApplication2
{
    
    public class Client
    {
        public ConcurrentDictionary<string, List<Message>> ExchangedMessages { get; set; } = new ConcurrentDictionary<string, List<Message>>();
        public ConcurrentQueue<Message> MessagesToSend { get; set; } = new ConcurrentQueue<Message>();
        private List<string> connectedUsers = new List<string>();
        public List<string> ConnectedUsers { get { return connectedUsers; } set { connectedUsers = value; OnNewConnectedCustomer(connectedUsers); } }

        public void OnNewConnectedCustomer(List<string> list)
        {
            UserEventArgs UserEA = new UserEventArgs(list);
            NewConnectedCustomer?.Invoke(this, UserEA);
        }

        public event EventHandler<UserEventArgs> NewConnectedCustomer;

        public sealed class UserEventArgs : EventArgs
        {
            public List<string> UserNames { get; private set; }

            public UserEventArgs (List<string> usernames)
            {
                UserNames = usernames;
            }
        }
        

        public void OnReceivedMessage (string msg, string target)
        {
            
            var list = new List<Message>();
            if (ExchangedMessages.TryGetValue(target, out list))
            {
                list.Add(new Message { Body = msg, Target = target});
            }
            MessageEventArgs MessageEA = new MessageEventArgs(msg, target);
            ReceivedMessage?.Invoke(this, MessageEA);
        }

        public event EventHandler<MessageEventArgs> ReceivedMessage;

        public sealed class MessageEventArgs : EventArgs
        {
            public string MessageContent { get; private set; }
            public string Target { get; private set; }

            public MessageEventArgs(string messageContent, string target)
            {
                MessageContent = messageContent;
                Target = target;
            }
        }


        //Import Client
        public Client()
        {
            ExchangedMessages.TryAdd("Server", new List<Message>());
            SocClient = new TcpClient();
            //StartChat();
            //NS = SocClient.GetStream();
            //BR = new BinaryReader(NS);
            //BW = new BinaryWriter(NS);
        }

        public string Username { get; set; }
        TcpClient SocClient;
        NetworkStream NS;
        BinaryReader BR;
        BinaryWriter BW;
        private Object myLock = new object();
        

        void Envoyer(Message msg)
        {
            lock (myLock)
            {
                
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
                    //On constitue l'objet message à envoyer avec les élements fournis par l'utilisateur 

                    //ExchangedMessages[msg.Target].Add(msg);
                
            }
        }

        Message Recevoir()
        {
            Message ReceivedMessage = new Message();
            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.Sender = BR.ReadString();

            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.Target = BR.ReadString();

            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.Body = BR.ReadString();

            while (!(NS.DataAvailable))
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.FileSize = BR.ReadInt32();
            if (ReceivedMessage.FileSize != 0)
            {
                while (!NS.DataAvailable)
                {
                    //On attend qu'il y ait quelque chose sur le stream
                }
                ReceivedMessage.Attachment = BR.ReadBytes(ReceivedMessage.FileSize);
                //On récupère le tableau d'octets représentant le fichier reçu. On le transformera en fichier plus bas
            }

            while (!NS.DataAvailable)
            {
                //On attend qu'il y ait quelque chose sur le stream
            }
            ReceivedMessage.FileName = BR.ReadString();
            if (ReceivedMessage.FileSize != 0)
            {
                //On transforme le tableau d'octets en fichier
                string pathToFile = "C:\\PtiChat\\" + ReceivedMessage.FileName;
                Stream file = File.OpenWrite(pathToFile);
                file.Write(ReceivedMessage.Attachment, 0, ReceivedMessage.FileSize);
                
                file.Close();

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
            ReceivedMessage.ReceiveTime = DateTime.Now.ToString();
            //On récupère la date de réception du message

            return ReceivedMessage;
        }
        void EnvoiMessage()
        {
            bool success;
            bool ConnectionSuccessful;
            Message msgToSend;
            string HostAddress = "ec2-35-162-78-174.us-west-2.compute.amazonaws.com";
            //string HostAddress = "igorpc.northeurope.cloudapp.azure.com";
            //string HostAddress = "localhost";

            while (true)
            {
                // ******** REACTIVER CETTE SECTION POUR FAIRE MARCHER LE PROGRAMME AVEC WPF **************
                while (MessagesToSend.IsEmpty)
                {
                    //attendre
                }
                
                //On récupère le premier message dans la file sans le supprimer
                MessagesToSend.TryPeek(out msgToSend);

                // ****************************************************************************************

                if (msgToSend.Body.Contains("@File"))
                {
                    //On regarde si le message contient une PJ
                    string filePath = msgToSend.Body.Substring(msgToSend.Body.IndexOf("@File") + 5); //Le chemin du fichier
                    msgToSend.Body = msgToSend.Body.Remove(msgToSend.Body.IndexOf("@File"));
                    //On enlève tout ce qui arrive après @File
                    Stream file = File.OpenRead(filePath);
                    msgToSend.Attachment = new byte[file.Length];
                    file.Read(msgToSend.Attachment, 0, (int)file.Length);
                    msgToSend.FileSize = msgToSend.Attachment.Length;
                    msgToSend.FileName = filePath.Substring(filePath.LastIndexOf("\\") + 1); //Le nom du fichier
                }
                else
                {
                    msgToSend.FileSize = 0;
                    msgToSend.Attachment = new byte[0];
                    msgToSend.FileName = "";
                    //On envoie pas de fichier donc on envoie un tableau vide
                }
                msgToSend.Sender = Username;
                msgToSend.SendTime = DateTime.Now.ToString();

                //On essaie d'envoyer le message
                success = true;
                try
                {
                    this.Envoyer(msgToSend);
                }
                catch
                {
                    success = false;
                    //Si l'envoi a échoué, c'est probablement du à une perte de connexion.
                    //On se déconnecte puis on se reconnecte
                    SocClient.Close();
                    ConnectionSuccessful = false;
                    while (!ConnectionSuccessful)
                    {
                        ConnectionSuccessful = AttemptConnection(HostAddress, this.Username);
                    }        
                }

                //on ne dequeue le message à envoyer que si l'envoi a réussi
                if (success)
                {
                    MessagesToSend.TryDequeue(out msgToSend);
                }


                
                

            }
        }
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
                                //ExchangedMessages
                                if (!(ExchangedMessages.ContainsKey(user)))
                                {
                                    ExchangedMessages.TryAdd(user, new List<Message>());
                                }
                            }

                        }

                    }

                    else
                    {
                        //Le message vient d'un autre client, on l'ajoute dans le dictionnaire de notre conversation avec l'autre client
                        //ExchangedMessages[messageRecu.Sender].Add(messageRecu);
                        OnReceivedMessage(messageRecu.Body, messageRecu.Sender);
                    }
                }
                catch
                {
                    //Il y a eu une erreur, ce qui normalement veut dire qu'on a perdu la connexion
                    //avec le serveur.
                    
                }

                
            }
        }

        
        public void StartChat()
        {
            string HostAddress = "ec2-35-162-78-174.us-west-2.compute.amazonaws.com";
            //string HostAddress = "igorpc.northeurope.cloudapp.azure.com";
            //string HostAddress = "localhost";


            MessagesToSend.Enqueue(new Message { Body = $"Igor{DateTime.Now.Millisecond}" });

            // ******* REACTIVER CETTE SECTION POUR FAIRE MARCHER LE PROGRAMME AVEC WPF ****************
            while (MessagesToSend.IsEmpty)
            {
                //attendre
            }
            Message startingMessage;
            MessagesToSend.TryDequeue(out startingMessage);

            // *****************************************************************************************

            //On essaie de se connecter au serveur
            bool ConnectionSuccessful = false;
            while (!ConnectionSuccessful)
            {
                ConnectionSuccessful = AttemptConnection(HostAddress, startingMessage.Body);
            }


            //On lance les différents threads d'envoi et de réception
            Thread sendingThread = new Thread(() => EnvoiMessage());
            Thread receivingThread = new Thread(() => ReceptionMessage());
            
            sendingThread.Start();
            receivingThread.Start();
            
            

        }

        /// <summary>
        /// Essaie d'établir la connexion en suivant le protocole d'identification
        /// </summary>
        /// <param name="ServerHostDns"> l'adresse où contacter le serveur </param>
        /// <param name="pseudo"> le nom avec lequel on va être identifié sur le serveur</param>
        /// <returns> un booléen indiquant si la connexion est réussie </returns>
        public bool AttemptConnection(string ServerHostDns,string pseudo)
        {
            IPAddress ipServer;
            bool success = true;
            Message listOfClients;
            Message ConnectMessage = new Message();
            List<string> connectedPeople;

            //On essaie de se connecter au serveur puis d'envoyer le message d'identification
            try
            {

                //On se connecte au serveur

                ipServer = Dns.GetHostAddresses(ServerHostDns)[0];


                SocClient.Connect(ipServer, 80);
            

                //On essaie ensuite d'envoyer le message d'identification au serveur
            
                this.NS = SocClient.GetStream();
                this.BR = new BinaryReader(NS);
                this.BW = new BinaryWriter(NS);

                this.Username = pseudo;

                
                //ClientName = ConnectMessage.Body;
                ConnectMessage.Sender = this.Username;
                ConnectMessage.Target = "Server";
                ConnectMessage.Body = this.Username;
                ConnectMessage.SendTime = DateTime.Now.ToString();
                //On envoie notre nom d'utilisateur au serveur
                this.Envoyer(ConnectMessage);
  
                //On reçoit la liste des clients connecté de la part du serveur et on met à jour ConnectedUsers
                listOfClients = this.Recevoir();
                connectedPeople = listOfClients.Body.Split(',').ToList();
                ConnectedUsers = string.IsNullOrEmpty(connectedPeople[0]) ? new List<string>() : connectedPeople;

                //On initialise les conversations dans le dictionnaire ExchangedMessages
                foreach (string user in connectedPeople)
                {
                    ExchangedMessages.TryAdd(user, new List<Message>());
                }
            }
            catch
            {
                //Il y a eu une erreur lors de la tentative de connexion
                success = false;
            }

            return success;
        }

    }
}

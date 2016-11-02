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
                string pathToFile = "C:\\PtiChat\\" + ReceivedMessage.FileName;
                Stream file = File.OpenWrite(pathToFile);
                file.Write(ReceivedMessage.Attachment, 0, ReceivedMessage.FileSize);
                //On transforme le tableau d'octets en fichier
                file.Close();
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
            while (true)
            {

                while (MessagesToSend.IsEmpty)
                {

                }
                Message msgToSend;
                MessagesToSend.TryDequeue(out msgToSend);



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
                msgToSend.SendTime = DateTime.Now.ToString();
                this.Envoyer(msgToSend);

            }
        }
        void ReceptionMessage()
        {
            List<Message> RemoveList = new List<Message>();
            while (true)
            {
                while (!NS.DataAvailable)
                {
                    //On attend qu'il y ait quelque chose sur le Stream
                }
                Message messageRecu = this.Recevoir();
                if (messageRecu.Sender == "Server")
                {
                    List<string> connectedPeople = messageRecu.Body.Split(',').ToList();

                    ConnectedUsers = string.IsNullOrEmpty(connectedPeople[0]) ? new List<string>() : connectedPeople;

                    foreach(string user in ExchangedMessages.Keys)
                    {
                        if (!ConnectedUsers.Contains(user))
                        {
                            List<string> ConversationList = new List<string>();
                            foreach(Message messageToStore in ExchangedMessages[user])
                            {
                                ConversationList.Add($"<{messageToStore.SendTime}/><{messageToStore.Sender}/><{messageToStore.Body}/>");
                            }
                            File.AppendAllLines($"C:\\Users\\Kasi\\Desktop\\Conversation between {this.Username}&{user}.txt", ConversationList);

                            ExchangedMessages.TryRemove(user, out RemoveList);

                        }
                    }
                    //si un client se déconnecte (il n'est plus dans ConnectedUsers) on l'enlève du dictionnaire ExchangedMessages
                    // et on stocke la discussion qu'on a eu avec lui dans un fichier txt


                    foreach (string user in connectedPeople)
                    {
                        //on rajoute la conversation correspondant à chaque nouvel utilisateur dans le dictionnaire
                        //ExchangedMessages
                        if (!(ExchangedMessages.ContainsKey(user)))
                        {
                            ExchangedMessages.TryAdd(user, new List<Message>());
                        }
                    }
                    //Le message vient du serveur, ayant donc comme but de nous informer de la liste des clients connectés
                    //On met à jour la list des utilisateurs connectés dans l'objet ViewModel

                }
                else
                {
                    //ExchangedMessages[messageRecu.Sender].Add(messageRecu);
                    OnReceivedMessage(messageRecu.Body, messageRecu.Sender);
                    //Le message vient d'un autre client, on l'ajoute dans le dictionnaire de notre conversation avec l'autre client
                }

            }
        }

        public void StartChat()
        {
            MessagesToSend.Enqueue(new Message { Body = "Kasra" });
            IPAddress ipServer;

            ipServer = Dns.GetHostAddresses("igorpc.northeurope.cloudapp.azure.com")[0];

            // A utiliser si on veut se connecter au localhost
            //IPAddress.TryParse("127.0.0.1", out ipServer);


            SocClient.Connect(ipServer, 80);
            this.NS = SocClient.GetStream();
            this.BR = new BinaryReader(NS);
            this.BW = new BinaryWriter(NS);

            while (MessagesToSend.IsEmpty)
            {
                //attendre
            }
            Message startingMessage;
            MessagesToSend.TryDequeue(out startingMessage);


            this.Username = startingMessage.Body;
            //ClientName = startingMessage.Body;
            startingMessage.Sender = this.Username;
            startingMessage.Target = "Server";
            startingMessage.Body = this.Username;
            startingMessage.SendTime = DateTime.Now.ToString();
            this.Envoyer(startingMessage);
            //On envoie notre nom d'usilisteur au serveur

            Message listOfClients = this.Recevoir();
            List<string> connectedPeople = listOfClients.Body.Split(',').ToList();
            ConnectedUsers = connectedPeople;

            foreach (string user in connectedPeople)
            {
                ExchangedMessages.TryAdd(user, new List<Message>());
            }
            //On reçoit la liste des clients connecté de la part du serveur et on la mets à jour

            Thread sendingThread = new Thread(() => EnvoiMessage());
            Thread receivingThread = new Thread(() => ReceptionMessage());
            sendingThread.Start();
            receivingThread.Start();
            //On lance les différents threads d'envoi et réception de message au serveur

        }

    }
}

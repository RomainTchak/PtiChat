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
using System.Net.NetworkInformation;
using System.Diagnostics;

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
        //string HostAddress = "igorpc.northeurope.cloudapp.azure.com";
        //string HostAddress = "localhost";
        Exception WriteException = new Exception("Write Exception");
        Exception ConnectionException = new Exception("Connection Exception");


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
        

        public void OnReceivedMessage (string msg, string interlocutor,string sender)
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

        public event EventHandler<MessageEventArgs> ReceivedMessage;

        public sealed class MessageEventArgs : EventArgs
        {
            public string MessageContent { get; private set; }
            public string Interlocutor { get; private set; }
            public string Sender { get; private set; }

            public MessageEventArgs(string messageContent, string interlocutor, string sender)
            {
                MessageContent = messageContent;
                Interlocutor = interlocutor;
                Sender = sender;
            }
        }


        
        public Client()
        {
            ExchangedMessages.TryAdd("Server", new List<Message>());
            SocClient = new TcpClient();
            isConnected = false;
        }

        
        


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
                try
                {
                    //On transforme le tableau d'octets en fichier
                    string pathToFile = "C:\\PtiChat\\" + ReceivedMessage.FileName;
                    Stream file = File.OpenWrite(pathToFile);
                    file.Write(ReceivedMessage.Attachment, 0, ReceivedMessage.FileSize);

                    file.Close();
                }
                catch (System.IO.IOException)
                {
                    try
                    {
                        //On essaie de renommer le fichier
                        string extension = ReceivedMessage.FileName.Substring(ReceivedMessage.FileName.LastIndexOf('.'));
                        string RealName = ReceivedMessage.FileName.Substring(0, ReceivedMessage.FileName.LastIndexOf('.'));
                        ReceivedMessage.FileName = RealName + " copy" + extension;
                        string pathToFile = "C:\\PtiChat\\" + ReceivedMessage.FileName;
                        Stream file = File.OpenWrite(pathToFile);
                        file.Write(ReceivedMessage.Attachment, 0, ReceivedMessage.FileSize);
                    }
                    catch (Exception FileEx)
                    {
                        Debug.WriteLine(FileEx.Message);
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
            ReceivedMessage.ReceiveTime = DateTime.Now.ToString();
            //On récupère la date de réception du message

            return ReceivedMessage;
        }



        void EnvoiMessage()
        {
            bool success;
            
            
            Message msgToSend = new Message();
            //string HostAddress = "ec2-35-162-78-174.us-west-2.compute.amazonaws.com";
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
                    catch(Exception)
                    {
                        throw WriteException;
                    }

                    if(!this.isConnected)
                    {
                        throw ConnectionException;
                    }
                    
                    
                }
                catch(Exception )
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
                            //foreach (string user in ExchangedMessages.Keys)
                            //{

                            //    if (!ConnectedUsers.Contains(user))
                            //    {
                            //        ExchangedMessages.TryRemove(user, out MsgList);
                            //    }
                            //}

                            foreach (string user in connectedPeople)
                            {
                                //on rajoute la conversation correspondant à chaque nouvel utilisateur dans le dictionnaire
                                //ExchangedMessages
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
                        OnReceivedMessage(messageRecu.Body, messageRecu.Sender,messageRecu.Sender);

                        //On stocke ce message dans la base de données de messages
                        StoreMessage(messageRecu.Sender, messageRecu);

                    }
                }
                catch(Exception ex)
                {
                    if(ex.Message == "Connection Exception")
                    {
                        //Si l'erreur de réception est due à une déconnexion, on attend jusqu'à ce que 
                        //la connexion soit rétablie

                        while (!this.isConnected) { }
                    }
                    
                    else
                    {
                        throw;
                    }
                    
                }

                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interlocutor"></param>
        /// <param name="MessageToStore"></param>
        public void StoreMessage(string interlocutor,Message MessageToStore)
        {
            var Line = new List<string>();
            Line.Add($"{MessageToStore.Sender}*{MessageToStore.Target}*{MessageToStore.Body}");       
            File.AppendAllLines($"C:\\PtiChat\\Conversation between {this.Username}&{interlocutor}.txt",Line);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        public void RetrieveMessages(string user)
        {
            try
            {
                Message storedMessage = new Message();
                List<string> messageHistory = File.ReadLines($"C:\\PtiChat\\Conversation between {this.Username}&{user}.txt").ToList();
                foreach (string message in messageHistory)
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        List<string> Attributes = message.Split('*').ToList();
                        storedMessage.Sender = Attributes[0];
                        storedMessage.Target = Attributes[1];
                        storedMessage.Body = Attributes[2];
                        string interlocutor;
                        if (storedMessage.Sender == this.Username)
                        {
                            interlocutor = storedMessage.Target;
                        }
                        else
                        {
                            interlocutor = storedMessage.Sender;
                        }
                        OnReceivedMessage(storedMessage.Body, interlocutor, storedMessage.Sender);
                    }

                }


                
            }
            catch (System.InvalidOperationException) { }
            
            catch (System.IO.FileNotFoundException) { }
        }


        public void StartChat()
        {
            
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
                    pingOptions = new PingOptions(10000,false);
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
                            catch(Exception)
                            {
                                PingSuccessful = false;
                                
                            }
                        }
                        
                        //Dès qu'on a récupéré la connexion Internet, on se reconnecte au serveur
                        
                        SocClient.Close();
                        SocClient = new TcpClient();

                        while (!TryAuthentification(false, this.Username, this.Password))
                        {

                        }
                        
                        this.isConnected = true;
                    }
                }
            }
        }

    }
}

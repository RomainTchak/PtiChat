using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;


namespace PtiChatClient
{
    class Client
    {
        string username;
        TcpClient SocClient;
        NetworkStream NS;
        BinaryReader BR;
        BinaryWriter BW;
        private Object myLock = new object();
        public Client()
        {
            SocClient = new TcpClient();
            //NS = SocClient.GetStream();
            //BR = new BinaryReader(NS);
            //BW = new BinaryWriter(NS);
        }
        public void Envoyer(Message msg,ViewModel vm)
        {
            lock (myLock)
            {
                BW.Write((string)msg.Sender);
                BW.Write((string)msg.Target);
                BW.Write((string)msg.Body);
                BW.Write((int)msg.FileSize);
                if (msg.FileSize!=0)
                {
                    //Si il y a un fichier à envoyer
                    BW.Write(msg.Attachment, 0, msg.Attachment.Length);
                }                           
                BW.Write((string)msg.FileName);
                BW.Write((string)msg.SendTime);
                BW.Write((string)msg.ReceiveTime);
                //On constitue l'objet message à envoyer avec les élements fournis par l'utilisateur 

                vm.ExchangedMessages[msg.Target].Add(msg);

            }
        }
        public Message Recevoir()
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
        public void EnvoiMessage(ViewModel vm)
        {
            while (true)
            {
                // ******** REACTIVER CETTE SECTION POUR FAIRE MARCHER LE PROGRAMME AVEC WPF **************
                //while (vm.MessagesToSend.IsEmpty)
                //{

                //}
                //Message msgToSend;
                //vm.MessagesToSend.TryDequeue(out msgToSend);
                //msgToSend.Sender = this.username;

                // ****************************************************************************************


                // ******** CETTE SECTION SERT JUSTE A TESTER LE PROGRAMME EN MODE CONSOLE ****************
                Message msgToSend = new Message();
                msgToSend.Body = Console.ReadLine();
                msgToSend.Sender = this.username;
                msgToSend.Target = vm.ConnectedUsers[0];
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
                msgToSend.SendTime = DateTime.Now.ToString();
                this.Envoyer(msgToSend,vm);

            }
        }
        public void ReceptionMessage(ViewModel vm)
        {
            while (true)
            {
                while (!NS.DataAvailable)
                {
                    //On attend qu'il y ait quelque chose sur le Stream
                }
                Message messageRecu=this.Recevoir();
                if (messageRecu.Sender == "Server")
                {
                    List<string> connectedPeople = messageRecu.Body.Split(',').ToList();
                    vm.ConnectedUsers = connectedPeople;

                    foreach(string user in connectedPeople)
                    {
                        //on rajoute la conversation correspondant à chaque nouvel utilisateur dans le dictionnaire
                        //ExchangedMessages
                        if (!(vm.ExchangedMessages.ContainsKey(user))) {
                            vm.ExchangedMessages.TryAdd(user, new List<Message>());
                        }
                    }
                    //Le message vient du serveur, ayant donc comme but de nous informer de la liste des clients connectés
                    //On met à jour la list des utilisateurs connectés dans l'objet ViewModel

                    // ************ CETTE SECTION EST A RETIRER LORS DU FONCTIONNEMENT AVEC WPF ***********************
                    Console.WriteLine("Clients connectés : " + messageRecu.Body);
                    // ************************************************************************************************
                }
                else
                {
                    vm.ExchangedMessages[messageRecu.Sender].Add(messageRecu);
                    //Le message vient d'un autre client, on l'ajoute dans le dictionnaire de notre conversation avec l'autre client
                }

                //******** CETTE SECTION SERT JUSTE A TESTER LE PROGRAMME EN MODE CONSOLE **************** //
                if (messageRecu.FileSize > 0)
                {
                    messageRecu.Body = messageRecu.Body + " Attachment : " + messageRecu.FileName;
                }

                if (messageRecu.Sender != "Server")
                {
                    Console.WriteLine(messageRecu.Sender + " : " + messageRecu.Body);
                }

                //*************************************************************************************** //
            }
        }

        public void UpdateConnection(ViewModel vm)
        {
            Message pingMessage = new Message();
            pingMessage.Sender = this.username;
            pingMessage.Target = "Server";
            pingMessage.SendTime = DateTime.Now.ToString();

            while (true)
            {
                Thread.Sleep(10);
                
                
                this.Envoyer(pingMessage,vm);
                //On envoie un ping toutes les 3 sec au serveur pour lui dire qu'on est toujours connectés

            }
        }
        public void StartChat(ViewModel vm)
        {
            IPAddress ipServer;

            //ipServer = Dns.GetHostAddresses("igorpc.northeurope.cloudapp.azure.com")[0];
            ipServer = Dns.GetHostAddresses("ec2-35-162-78-174.us-west-2.compute.amazonaws.com")[0];

            // A utiliser si on veut se connecter au localhost
            //IPAddress.TryParse("127.0.0.1", out ipServer);


            SocClient.Connect(ipServer, 80);
            this.NS = SocClient.GetStream();
            this.BR = new BinaryReader(NS);
            this.BW = new BinaryWriter(NS);

            // ******* REACTIVER CETTE SECTION POUR FAIRE MARCHER LE PROGRAMME AVEC WPF ****************
            //while (vm.MessagesToSend.IsEmpty)
            //{
            //    //attendre
            //}
            //Message startingMessage;
            //vm.MessagesToSend.TryDequeue(out startingMessage);

            // *****************************************************************************************

            // **************** CETTE SECTION SERT JUSTE A TESTER LE PROGRAMME EN MODE CONSOLE *********
            Message startingMessage = new Message();
            Console.WriteLine("Choisissez un nom d'utilisateur.");
            startingMessage.Body = Console.ReadLine();
            // *****************************************************************************************

            this.username = startingMessage.Body;
            vm.ClientName = startingMessage.Body;
            startingMessage.Sender = this.username;
            startingMessage.Target = "Server";
            startingMessage.Body = this.username;
            startingMessage.SendTime = DateTime.Now.ToString();
            this.Envoyer(startingMessage, vm);
            //On envoie notre nom d'usilisteur au serveur

            Message listOfClients = this.Recevoir();
            List<string> connectedPeople = listOfClients.Body.Split(',').ToList();
            vm.ConnectedUsers = connectedPeople;

            // *************** CETTE SECTION SERT A TESTER LE PROGRAMME EN MODE CONSOLE *******************
            Console.WriteLine("Clients connectés : " + listOfClients.Body);
            // ********************************************************************************************

            foreach (string user in connectedPeople)
            {
                vm.ExchangedMessages.TryAdd(user, new List<Message>());
            }
            //On reçoit la liste des clients connecté de la part du serveur et on la mets à jour

            Thread sendingThread = new Thread(() => EnvoiMessage(vm));
            Thread receivingThread = new Thread(() => ReceptionMessage(vm));
            Thread updatingThread = new Thread(() => UpdateConnection(vm));
            sendingThread.Start();
            receivingThread.Start();
            //updatingThread.Start();
            //On lance les différents threads d'envoi, réception et de d'envoi de ping au serveur

        }
    }
}

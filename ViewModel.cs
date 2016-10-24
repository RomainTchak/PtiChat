using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace PtiChatClient
{
    public class ViewModel
    {
        public List<string> ConnectedUsers = new List<string>();
        public ConcurrentDictionary<string, List<Message>> ExchangedMessages;
        //Cela sert de créer une liste de messages par conversation, la conversation étant le string dans le dictionnaire
        public ConcurrentQueue<Message> MessagesToSend;
        //Stocke les messages que l'utilisateur tape dans la textBox
        public string ClientName;
        public ViewModel()
        {
            ConnectedUsers = new List<string>();
            ExchangedMessages = new ConcurrentDictionary<string, List<Message>>();
            MessagesToSend = new ConcurrentQueue<Message>();
            ClientName = "";
            ExchangedMessages.TryAdd("Server", new List<Message>());
        }

    }


}

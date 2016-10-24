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
    public class Message
    {
        public String Sender { get; set; } //expéditeur du message
        public String Target { get; set; } //destinataire du message
        public String Body { get; set; } //corps du message
        public int FileSize { get; set; } //nombre d'octets dans la pièce-jointe
        public Byte[] Attachment { get; set; } //contient les octets de la pièce jointe
        public String FileName { get; set; } //nom du fichier PJ
        public String SendTime { get; set; } //L'heure d'envoi
        public String ReceiveTime { get; set; } //L'heure de réception

        public Message()
        {
            Sender = "";
            Target = "";
            Body = "";
            FileSize = 0;
            Attachment = new byte[0];
            FileName = "";
            SendTime = "";
            ReceiveTime = "";
        }
        
    }
}

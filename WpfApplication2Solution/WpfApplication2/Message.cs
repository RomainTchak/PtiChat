using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApplication2
{
    public class Message
    {
        public string Sender { get; set; } = ""; //expéditeur du message
        public string Target { get; set; } = ""; //destinataire du message
        public string Body { get; set; } = ""; //corps du message
        public int FileSize { get; set; } //nombre d'octets dans la pièce-jointe
        public Byte[] Attachment = new byte[0]; //contient les octets de la pièce jointe
        public string FileName = ""; //nom du fichier PJ
        public string SendTime = ""; //L'heure d'envoi
        public string ReceiveTime = ""; //L'heure de réception

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PtiChatClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Client myClient = new Client();
            ViewModel myViewModel = new ViewModel();
            myClient.StartChat(myViewModel);
        }
    }
}

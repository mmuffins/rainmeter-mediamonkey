using PluginMediaMonkey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var mm = new MediaMonkey();

            mm.TempConnect();

            //mm.Session.OpenSessionAsync();
            //mm.Session.Player.RefreshAsync();
        }
    }
}

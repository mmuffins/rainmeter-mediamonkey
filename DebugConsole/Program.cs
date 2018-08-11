using PluginMediaMonkey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {

            //var mm = new MediaMonkey();

            //mm.TempConnect();

            var measure = new Measure();
            measure.Reload(Measure.MeasureType.Title);

            //mm.Session.OpenSessionAsync();
            //mm.Session.Player.RefreshAsync();
            while (true)
            {
                Console.WriteLine("Title:" + measure.GetString());
                Thread.Sleep(1000);
            }
        }
    }
}

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

            var title = new Measure();
            title.Reload(Measure.MeasureType.Title);

            var progress = new Measure();
            progress.Reload(Measure.MeasureType.Progress);

            var duration = new Measure();
            duration.Reload(Measure.MeasureType.Duration);

            var position = new Measure();
            position.Reload(Measure.MeasureType.Position);

            var state = new Measure();
            state.Reload(Measure.MeasureType.State);

            //mm.Session.OpenSessionAsync();
            //mm.Session.Player.RefreshAsync();
            int count = 0;
            while (true)
            {
                Console.WriteLine(count++);
                Console.WriteLine("Title:" + title.GetString());
                Console.WriteLine("state:" + state.GetString());
                Console.WriteLine("progress:" + progress.Update());
                Console.WriteLine("duration:" + duration.Update());
                Console.WriteLine("position:" + position.Update());
                Thread.Sleep(1000);
            }
        }
    }
}

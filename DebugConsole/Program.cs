using PluginMediaMonkey;
using Rainmeter;
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

            var api = new API(new IntPtr());

            var title = new Measure();
            title.Reload(Measure.MeasureType.Title, api);

            var progress = new Measure();
            progress.Reload(Measure.MeasureType.Progress, api);

            var duration = new Measure();
            duration.Reload(Measure.MeasureType.Duration, api);

            var position = new Measure();
            position.Reload(Measure.MeasureType.Position, api);

            var state = new Measure();
            state.Reload(Measure.MeasureType.State, api);

            var cover = new Measure();
            cover.Reload(Measure.MeasureType.Cover, api);

            var dateAdded = new Measure();
            dateAdded.Reload(Measure.MeasureType.DateAdded, api);

            var lastplayed = new Measure();
            lastplayed.Reload(Measure.MeasureType.LastPlayedDate, api);

            int count = 0;
            while (true)
            {
                Console.WriteLine(count++);
                Console.WriteLine("Title:" + title.GetString());
                Console.WriteLine("state:" + state.GetString());
                Console.WriteLine("progress:" + progress.Update());
                Console.WriteLine("duration:" + duration.Update());
                Console.WriteLine("durationStr:" + duration.GetString());
                Console.WriteLine("position:" + position.Update());
                Console.WriteLine("positionStr:" + position.GetString());
                Console.WriteLine("date added:" + dateAdded.GetString());
                Console.WriteLine("last played:" + lastplayed.GetString());
                Thread.Sleep(1000);
                //title.ExecuteBang("TogglePlayer".Split(' '));
            }
        }
    }
}

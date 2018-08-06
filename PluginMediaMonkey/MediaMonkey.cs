using MediaMonkeyNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginMediaMonkey
{
    public class MediaMonkey
    {
        public MediaMonkeySession Session { get; set; }

        public void TempConnect()
        {
            LogMessageToFile("aa");
            try
            {
                //Session = new MediaMonkeySession();
                //Session.OpenSessionAsync().Wait();
                //Session.Player.RefreshAsync().Wait();
                //Session.RefreshCurrentTrackAsync().Wait();
                //Session.EnableUpdates().GetAwaiter();
                //while (true)
                //{
                //    Console.WriteLine("Current Track:");
                //    Console.WriteLine("Title:" + Session.CurrentTrack.Title);
                //    Console.WriteLine("Artist:" + Session.CurrentTrack.Artist);
                //    Console.WriteLine("Rating:" + Session.CurrentTrack.Rating);
                //    Console.WriteLine("Is Playing:" + Session.Player.IsPlaying);
                //    System.Threading.Thread.Sleep(4000);
                //}
                //Session.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private string GetTempPath()
        {
            string path = System.Environment.GetEnvironmentVariable("TEMP");
            if (!path.EndsWith("\\")) path += "\\";
            return path;
        }

        private void LogMessageToFile(string msg)
        {
            try
            {
                using (var sw = System.IO.File.AppendText(GetTempPath() + "rainmeterdebug.txt"))
                {
                    string logLine = System.String.Format("{0:HH:mm:ss.fff}: {1}.", System.DateTime.Now, msg);
                    sw.WriteLine(logLine);
                    sw.Close();
                }

            }
            catch (Exception)
            {
            }
        }
    }
}

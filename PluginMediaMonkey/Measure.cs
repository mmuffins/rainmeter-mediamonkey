using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MediaMonkeyNet;

namespace PluginMediaMonkey
{
    public class Measure : IDisposable
    {
        public enum MeasureType
        {
            Album,
            Title
        }

        private static MediaMonkeySession mmSession;

        public MeasureType Type { get; set; }
        public IntPtr buffer = IntPtr.Zero;

        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }

        private async Task InitMMSession()
        {
            try
            {
                mmSession = new MediaMonkeySession();
                await mmSession.OpenSessionAsync().ConfigureAwait(false);
                await mmSession.RefreshCurrentTrackAsync().ConfigureAwait(false);
                await mmSession.Player.RefreshAsync().ConfigureAwait(false);
                mmSession.EnableUpdates().GetAwaiter();
            }
            catch (Exception e)
            {
                mmSession.Dispose();
                mmSession = null;
                throw;
            }
        }

        public void Reload(MeasureType type)
        {
            Type = type;
            if (mmSession == null)
            {
                InitMMSession().GetAwaiter();
            }
        }

        public double Update()
        {
            LogMessageToFile("Update");
            if (mmSession == null)
            {
                InitMMSession().GetAwaiter();
                return 0.0;
            }

            return 6.0;
        }

        public string GetString()
        {
            LogMessageToFile("getString");
            if (mmSession == null)
            {
                InitMMSession().GetAwaiter();
                return string.Empty;
            }

            switch (Type)
            {
                case MeasureType.Album:
                    return mmSession.CurrentTrack.Album;
                case MeasureType.Title:
                    return mmSession.CurrentTrack.Title;
                default:
                    return string.Empty;
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (mmSession != null)
                    {
                        mmSession.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        // ~Measure() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
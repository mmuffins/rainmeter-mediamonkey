using System;
using System.Diagnostics;
using System.Linq;
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
            AlbumArtist,
            Artist,
            Composer,
            Conductor,
            Cover,
            Custom1,
            Custom2,
            Custom3,
            Custom4,
            Custom5,
            Custom6,
            Custom7,
            Custom8,
            Custom9,
            Custom10,
            Disc,
            Duration,
            File,
            FileID,
            Genre,
            Grouping,
            Number,
            Position,
            Progress,
            Publisher,
            Rating,
            Repeat,
            Shuffle,
            State,
            Status,
            Title,
            Volume,
            Year
        }

        private static MediaMonkeySession mmSession;
        private static bool InitInProgress = false;
        private static bool RefreshInProgress = false;
        private static Process MMProcess;
        private static Process[] MMengineProcessAr;
        private const int StartUpDelay = 800;

        public MeasureType Type { get; set; }
        public IntPtr buffer = IntPtr.Zero;

        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }

        private async Task InitMMSession()
        {
            if(InitInProgress || IsMMRunning() == false) { return; }

            InitInProgress = true;
            Console.WriteLine("init");

            var tempSession = new MediaMonkeySession();
            try
            {
                Console.WriteLine("Opensession");
                await tempSession.OpenSessionAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("Dispose");
                tempSession.Dispose();
                tempSession = null;
                //throw;
            }

            if(tempSession == null)
            {
                InitInProgress = false;
                return;
            }

            try
            {
                Console.WriteLine("isReadyInit:");
                await tempSession.RefreshCurrentTrackAsync().ConfigureAwait(false);
                await tempSession.Player.RefreshAsync().ConfigureAwait(false);
                tempSession.EnableUpdates().GetAwaiter();
                mmSession = tempSession;
            }
            catch (Exception e)
            {
                Console.WriteLine("Dispose2");
                tempSession.Dispose();
                tempSession = null;
            }
            finally
            {
                InitInProgress = false;
            }
        }

        private async Task RefreshPlayer()
        {
            if (RefreshInProgress || IsMMRunning() == false || mmSession == null) { return; }
            RefreshInProgress = true;

            try
            {
                Console.WriteLine("refresh");
                await mmSession.Player.RefreshTrackPositionAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine("Dispose");
                mmSession.Dispose();
                mmSession = null;
                //throw;
            }
            finally
            {
                RefreshInProgress = false;
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
            return Update(false);
        }

        public double Update(bool skipRefresh)
        {
            if (mmSession == null)
            {
                InitMMSession().GetAwaiter();
                return 0.0;
            }

            switch (Type)
            {
                case MeasureType.State:
                    switch (mmSession.Player.State)
                    {
                        case PlayerState.Stopped:
                            return 0;
                        case PlayerState.Playing:
                            return 1;
                        case PlayerState.Paused:
                            return 2;
                        default:
                            return 0;
                    }
                case MeasureType.Progress:
                    if (!skipRefresh) { RefreshPlayer().GetAwaiter(); }
                    return mmSession.Player.Progress * 100;
                case MeasureType.Position:
                    if (!skipRefresh) { RefreshPlayer().GetAwaiter(); }
                    return mmSession.Player.TrackPosition / 1000;
                case MeasureType.Duration:
                    if (!skipRefresh) { RefreshPlayer().GetAwaiter(); }
                    return mmSession.Player.TrackLength / 1000.0;
                case MeasureType.Number:
                    return mmSession.CurrentTrack.TrackNumberInt;
                case MeasureType.Rating:
                    return mmSession.CurrentTrack.Rating == -1 ? 0 : mmSession.CurrentTrack.Rating / 20;
                case MeasureType.Status:
                    return IsMMRunning() ? 1 : 0;
                case MeasureType.Volume:
                    return mmSession.Player.Volume * 100;
                case MeasureType.Repeat:
                    return mmSession.Player.IsRepeat ? 1 : 0;
                case MeasureType.Shuffle:
                    return mmSession.Player.IsShuffle ? 1 : 0;
                case MeasureType.Year:
                    return mmSession.CurrentTrack.Year;
                case MeasureType.Disc:
                    return mmSession.CurrentTrack.DiscNumberInt;
                case MeasureType.FileID:
                    return mmSession.CurrentTrack.ID;
            }

            return 0.0;
        }

        public string GetString()
        {
            if (mmSession == null)
            {
                InitMMSession().GetAwaiter();
                return string.Empty;
            }

            switch (Type)
            {
                case MeasureType.Album:
                    return mmSession.CurrentTrack.Album;
                case MeasureType.AlbumArtist:
                    return mmSession.CurrentTrack.AlbumArtist;
                case MeasureType.Artist:
                    return mmSession.CurrentTrack.Artist;
                case MeasureType.Composer:
                    return mmSession.CurrentTrack.Composer;
                case MeasureType.Conductor:
                    return mmSession.CurrentTrack.Conductor;
                case MeasureType.Cover:
                    return string.Empty;
                case MeasureType.Custom1:
                    return mmSession.CurrentTrack.Custom1;
                case MeasureType.Custom2:
                    return mmSession.CurrentTrack.Custom2;
                case MeasureType.Custom3:
                    return mmSession.CurrentTrack.Custom3;
                case MeasureType.Custom4:
                    return mmSession.CurrentTrack.Custom4;
                case MeasureType.Custom5:
                    return mmSession.CurrentTrack.Custom5;
                case MeasureType.Custom6:
                    return mmSession.CurrentTrack.Custom6;
                case MeasureType.Custom7:
                    return mmSession.CurrentTrack.Custom7;
                case MeasureType.Custom8:
                    return mmSession.CurrentTrack.Custom8;
                case MeasureType.Custom9:
                    return mmSession.CurrentTrack.Custom9;
                case MeasureType.Custom10:
                    return mmSession.CurrentTrack.Custom10;
                case MeasureType.Disc:
                    return mmSession.CurrentTrack.DiscNumber;
                case MeasureType.File:
                    return mmSession.CurrentTrack.FileName;
                case MeasureType.FileID:
                    return mmSession.CurrentTrack.ID.ToString();
                case MeasureType.Genre:
                    return mmSession.CurrentTrack.Genre;
                case MeasureType.Grouping:
                    return mmSession.CurrentTrack.Grouping;
                case MeasureType.Publisher:
                    return mmSession.CurrentTrack.Publisher;
                case MeasureType.Title:
                    return mmSession.CurrentTrack.Title;
                case MeasureType.State:
                case MeasureType.Progress:
                case MeasureType.Position:
                case MeasureType.Duration:
                case MeasureType.Volume:
                case MeasureType.Status:
                case MeasureType.Number:
                case MeasureType.Rating:
                case MeasureType.Year:
                case MeasureType.Repeat:
                case MeasureType.Shuffle:
                    return Update(true).ToString();
            }

            return string.Empty;
        }

        public void ExecuteBang(string args)
        {
            string[] argsArray = args.Split(' ');

            switch (argsArray[0].ToLowerInvariant())
            {
                case "play":
                    mmSession.Player.StartPlaybackAsync().GetAwaiter();
                    break;

                case "pause":
                    mmSession.Player.PausePlaybackAsync().GetAwaiter();
                    break;

                case "playpause":
                    mmSession.Player.TogglePlaybackAsync().GetAwaiter();
                    break;

                case "stop":
                    mmSession.Player.StopPlaybackAsync().GetAwaiter();
                    break;

                case "previous":
                    mmSession.Player.PreviousTrackAsync().GetAwaiter();
                    break;

                case "next":
                    mmSession.Player.NextTrackAsync().GetAwaiter();
                    break;

                case "openplayer":
                    throw new NotImplementedException();
                    break;

                case "closeplayer":
                    throw new NotImplementedException();
                    break;

                case "toggleplayer":
                    throw new NotImplementedException();
                    break;

                case "setrating":
                    int argsRating;

                    if (int.TryParse(argsArray[1], out argsRating))
                    {
                        int mmRating = argsRating * 20;
                        if (mmRating <= 0) mmRating = -1;
                        mmSession.CurrentTrack.SetRatingAsync(-1).GetAwaiter();
                    }
                    break;

                case "setposition":
                    double argsPosition;

                    if (double.TryParse(argsArray[1], out argsPosition))
                    {
                        mmSession.Player.SetProgressAsync(argsPosition / 100.0).GetAwaiter();
                    }
                    break;

                case "setshuffle":
                    int argsShuffle;

                    if (int.TryParse(argsArray[1], out argsShuffle))
                    {
                        switch (argsShuffle)
                        {
                            case 0:
                            case 1:
                                mmSession.Player.SetShuffleAsync(argsShuffle == 1).GetAwaiter();
                                break;
                            case -1:
                                mmSession.Player.SetShuffleAsync(!mmSession.Player.IsShuffle).GetAwaiter();
                                break;
                        }
                    }
                    break;

                case "setrepeat":
                    int argsRepeat;

                    if (int.TryParse(argsArray[1], out argsRepeat))
                    {
                        switch (argsRepeat)
                        {
                            case 0:
                            case 1:
                                mmSession.Player.SetRepeatAsync(argsRepeat == 1).GetAwaiter();
                                break;

                            case -1:
                                mmSession.Player.SetRepeatAsync(!mmSession.Player.IsRepeat).GetAwaiter();
                                break;
                        }
                    }
                    break;

                case "setvolume":
                    int parsedVolume;

                    if (!string.IsNullOrWhiteSpace(argsArray[1]) && int.TryParse(argsArray[1], out parsedVolume))
                    {
                        if (argsArray[1].Substring(0, 1) == "+" || argsArray[1].Substring(0, 1) == "-")
                        {
                            mmSession.Player.SetVolumeAsync(mmSession.Player.Volume + parsedVolume);
                        }
                        else
                        {
                            mmSession.Player.SetVolumeAsync(parsedVolume);
                        }

                    }
                    break;
            }
        }

        public void GetMMProcess()
        {
            Process[] mmEngineProcessAr = Process.GetProcessesByName("MediaMonkeyEngine");
            if(mmEngineProcessAr.Length < 2) { return; }

            Process[] mmProcessAr = Process.GetProcessesByName("MediaMonkey");
            if (mmProcessAr.Length == 0) { return; }

            MMProcess = mmProcessAr[0];
            MMengineProcessAr = mmEngineProcessAr;
        }

        private bool IsMMRunning()
        {
            // Search for processes if not yet found or if they exited
            if(MMProcess == null || MMengineProcessAr == null || MMProcess.HasExited == true || MMengineProcessAr.Any(proc => proc.HasExited == true))
            {
                MMProcess = null;
                MMengineProcessAr = null;
                if (mmSession != null)
                {
                    mmSession.Dispose();
                    mmSession = null;
                }
                GetMMProcess();
            }

            // Check if both the main process and the engine processes were found
            if(MMProcess == null || MMengineProcessAr == null || MMengineProcessAr.Length < 2) { return false; }

            // Immediately after start, MM needs a couple of moments to initialize.
            // Sending commands to MM before it is ready can crash the application.
            // There is currently no (known) way to actually check for a ready state, so we wait for a 
            // few moments after mm was started to give a green light
            return (DateTime.Now.Subtract(MMengineProcessAr[1].StartTime).TotalMilliseconds >= StartUpDelay);
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
                        mmSession = null;
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
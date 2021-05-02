using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MediaMonkeyNet;
using Microsoft.Win32;
using Rainmeter;

namespace PluginMediaMonkey
{
    public class Measure : IDisposable
    {
        public enum MeasureType
        {
            Title,
            OriginalTitle,
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
            Volume,
            Year,
            DateAdded,
            FileType,
            ISRC,
            LastPlayedDate,
            OriginalArtist,
            OriginalLyricist,
            FilePath,
            Tempo,
            Mood,
            Occasion,
            Quality,
        }

        public enum BangType
        {
            Play,
            Pause,
            PlayPause,
            Stop,
            Previous,
            Next,
            SetRating,
            SetPosition,
            SetShuffle,
            SetVolume,
            SetRepeat,
            OpenPlayer,
            ClosePlayer,
            TogglePlayer
        }

        private static MediaMonkeySession mmSession;
        private static bool InitInProgress = false;
        private static bool RefreshInProgress = false;
        private static Process MMProcess;
        private static Process[] MMengineProcessAr;
        private const string MMDefaultInstallPath = @"C:\Program Files (x86)\MediaMonkey\MediaMonkey.exe";

        public MeasureType Type { get; set; }
        public API RainmeterAPI { get; set; }
        public int StartupDelay { get; set; } = 900;
        public string MMInstallLocation { get; set; }
        public bool DisableLeadingZero { get; set; }
        public IntPtr buffer = IntPtr.Zero;

        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }

        private async Task InitMMSession()
        {
            if(InitInProgress || IsMMRunning() == false) { return; }

            InitInProgress = true;

            var tempSession = new MediaMonkeySession();
            try
            {
                await tempSession.OpenSessionAsync();
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Error while establishing a connection to MediaMonkey ('InitMMSession'):{ex.Message}");
                tempSession.Dispose();
                tempSession = null;
            }
            catch (Exception ex)
            {
                RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Unknown error ('InitMMSession'):{ex.Message}");
                tempSession.Dispose();
                tempSession = null;
            }

            if (tempSession == null)
            {
                InitInProgress = false;
                return;
            }

            try
            {
                try { await tempSession.RefreshCurrentTrackAsync().ConfigureAwait(false); }
                catch (Newtonsoft.Json.JsonSerializationException ex)
                {
                    RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Error parsing playing track ('InitMMSession'):{ex.Message}");
                }

                try { await tempSession.Player.RefreshAsync().ConfigureAwait(false); }
                catch (Newtonsoft.Json.JsonSerializationException ex)
                {
                    RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Error parsing player state ('InitMMSession'):{ex.Message}");
                }

                if (tempSession != null &&  tempSession.CurrentTrack != null)
                {
                    await tempSession.CurrentTrack.LoadAlbumArt().ConfigureAwait(false);
                }

                if(tempSession != null)
                {
                    mmSession = tempSession;
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Unable to establish a connection to MediaMonkey ('InitMMSession'):{ex.Message}");
                tempSession.Dispose();
                tempSession = null;
            }
            catch (Exception ex)
            {
                RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Unknown error ('InitMMSession'):{ex.Message}");
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
                await mmSession.RefreshCurrentTrackAsync().ConfigureAwait(false);
                await mmSession.Player.RefreshAsync().ConfigureAwait(false);
            }
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Unable to parse track progress information ('RefreshPlayer'):{ex.Message}");
                mmSession.Dispose();
                mmSession = null;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Unable to establish a connection to MediaMonkey ('RefreshPlayer'):{ex.Message}");
                mmSession.Dispose();
                mmSession = null;
            }
            catch (Exception ex)
            {
                RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Unknown error ('RefreshPlayer'):{ex.Message}");
                mmSession.Dispose();
                mmSession = null;
            }
            finally
            {
                RefreshInProgress = false;
            }
        }

        public void Reload(MeasureType measureType, API api)
        {
            RainmeterAPI = api;
            Type = measureType;
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
            if (!IsMMRunning() && mmSession != null)
            {
                mmSession.Dispose();
                mmSession = null;
            }

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
                    return Math.Round((mmSession.Player.Progress * 100), 3);
                case MeasureType.Position:
                    if (!skipRefresh) { RefreshPlayer().GetAwaiter(); }
                    return Math.Round((mmSession.Player.TrackPosition / 1000.0));
                case MeasureType.Duration:
                    if (!skipRefresh) { RefreshPlayer().GetAwaiter(); }
                    return Math.Round(mmSession.Player.TrackLength / 1000.0);
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
            if (!IsMMRunning() && mmSession != null)
            {
                mmSession.Dispose();
                mmSession = null;
            }

            if (mmSession == null)
            {
                InitMMSession().GetAwaiter();
                return string.Empty;
            }

            switch (Type)
            {
                case MeasureType.Title:
                    return mmSession.CurrentTrack.Title;
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
                case MeasureType.OriginalTitle:
                    return mmSession.CurrentTrack.OriginalTitle;
                case MeasureType.Cover:
                    mmSession.LoadAlbumArt = true;
                    return GetCover();
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
                case MeasureType.DateAdded:
                    return mmSession.CurrentTrack.DateAdded.ToString();
                case MeasureType.FileType:
                    return mmSession.CurrentTrack.FileType;
                case MeasureType.ISRC:
                    return mmSession.CurrentTrack.ISRC;
                case MeasureType.LastPlayedDate:
                    return mmSession.CurrentTrack.LastTimePlayed.ToString();
                case MeasureType.OriginalArtist:
                    return mmSession.CurrentTrack.OriginalArtist;
                case MeasureType.OriginalLyricist:
                    return mmSession.CurrentTrack.OriginalLyricist;
                case MeasureType.FilePath:
                    return mmSession.CurrentTrack.Path;
                case MeasureType.Tempo:
                    return mmSession.CurrentTrack.Tempo;
                case MeasureType.Mood:
                    return mmSession.CurrentTrack.Mood;
                case MeasureType.Occasion:
                    return mmSession.CurrentTrack.Occasion;
                case MeasureType.Quality:
                    return mmSession.CurrentTrack.Quality;
                case MeasureType.Position:
                case MeasureType.Duration:
                    return DisableLeadingZero ?
                        TimeSpan.FromSeconds(Update(true)).ToString(@"m\:ss") :
                        TimeSpan.FromSeconds(Update(true)).ToString(@"mm\:ss");
                case MeasureType.State:
                case MeasureType.Progress:
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

        public string GetCover()
        {
            if (mmSession.CurrentTrack is null || mmSession.CurrentTrack.CoverList is null || mmSession.CurrentTrack.CoverList.Count == 0)
                return string.Empty;

            // The current track has at least a single cover object, check if a front cover is available
            Cover selectedCover = mmSession.CurrentTrack.CoverList
                .FirstOrDefault(x => x.FilePath != "" && x.CoverType == "Cover (front)");


            if (selectedCover != null)
                return selectedCover.FilePath;

            // No front cover was found, return the first available cover object
            selectedCover = mmSession.CurrentTrack.CoverList
                .FirstOrDefault(x => x.FilePath != "");

            if (selectedCover != null)
                return selectedCover.FilePath;

            return string.Empty;
        }

        public void ExecuteBang(string[] args)
        {
            Measure.BangType parsedBang;

            if (!Enum.TryParse(args[0], true, out parsedBang))
            {
                RainmeterAPI.LogF(API.LogType.Error, $"MediaMonkey.dll: Bang type '{args[0]}' is not valid.");
                return;
            }

            if (!IsMMRunning() && mmSession != null)
            {
                mmSession.Dispose();
                mmSession = null;
            }

            if (mmSession == null)
            {
                InitMMSession().Wait();
            }

            if(mmSession == null)
            {
                RainmeterAPI.LogF(API.LogType.Error, "MediaMonkey.dll: Could not initialize MediaMonkey. (ExecuteBang)");
                return;
            }

            try
            {
                switch (parsedBang)
                {
                    case BangType.Play:
                        mmSession.Player.StartPlaybackAsync().GetAwaiter();
                        break;

                    case BangType.Pause:
                        mmSession.Player.PausePlaybackAsync().GetAwaiter();
                        break;

                    case BangType.PlayPause:
                        mmSession.Player.TogglePlaybackAsync().GetAwaiter();
                        break;

                    case BangType.Stop:
                        mmSession.Player.StopPlaybackAsync().GetAwaiter();
                        break;

                    case BangType.Previous:
                        mmSession.Player.PreviousTrackAsync().GetAwaiter();
                        break;

                    case BangType.Next:
                        mmSession.Player.NextTrackAsync().GetAwaiter();
                        break;

                    case BangType.SetRating:
                        double argsRating;

                        if (double.TryParse(args[1], out argsRating))
                        {
                            int mmRating = (int)(argsRating * 20);
                            if (mmRating <= 0) mmRating = -1;
                            mmSession.CurrentTrack.SetRatingAsync(mmRating).GetAwaiter();
                        }
                        break;

                    case BangType.SetPosition:
                        double argsPosition;

                        if (double.TryParse(args[1], out argsPosition))
                        {
                            mmSession.Player.SetProgressAsync(argsPosition / 100.0).GetAwaiter();
                        }
                        break;

                    case BangType.SetShuffle:
                        int argsShuffle;

                        if (int.TryParse(args[1], out argsShuffle))
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

                    case BangType.SetRepeat:
                        int argsRepeat;

                        if (int.TryParse(args[1], out argsRepeat))
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

                    case BangType.SetVolume:
                        double parsedVolume;

                        if (!string.IsNullOrWhiteSpace(args[1]) && double.TryParse(args[1], out parsedVolume))
                        {
                            parsedVolume /= 100;
                            if (args[1].Substring(0, 1) == "+" || args[1].Substring(0, 1) == "-")
                            {
                                mmSession.Player.SetVolumeAsync(mmSession.Player.Volume + parsedVolume);
                            }
                            else
                            {
                                mmSession.Player.SetVolumeAsync(parsedVolume);
                            }

                        }
                        break;

                    case BangType.OpenPlayer:
                        if (!IsMMRunning())
                        {
                            Process.Start(GetMMExecutablePath());
                        }
                        break;

                    case BangType.ClosePlayer:
                        ClosePlayer();
                        break;

                    case BangType.TogglePlayer:
                        if (IsMMRunning())
                        {
                            ClosePlayer();
                        }
                        else
                        {
                            Process.Start(GetMMExecutablePath());
                        }
                        break;
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Unable to establish a connection to MediaMonkey:{ex.Message}");
                mmSession.Dispose();
                mmSession = null;
            }
            catch (Exception ex)
            {
                RainmeterAPI.LogF(API.LogType.Error, $"Mediamonkey.dll: Unknown error ('ExecuteBang','{parsedBang.ToString()}'):{ex.Message}");
                mmSession.Dispose();
                mmSession = null;
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
            return (DateTime.Now.Subtract(MMengineProcessAr[1].StartTime).TotalMilliseconds >= StartupDelay);
        }

        private void ClosePlayer()
        {
            if (IsMMRunning() && mmSession != null)
            {
                mmSession.ClosePlayer().GetAwaiter();
                mmSession.Dispose();
                mmSession = null;
            }
        }

        public string GetMMExecutablePath()
        {
            // Returns either the manually provided install path, the install path from the registry or the default install path,
            // in this order, depending on what is available.
            if (!string.IsNullOrEmpty(MMInstallLocation)) return MMInstallLocation;

            string regPath = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Clients\\Media\\MediaMonkey\\shell\\open\\command", "", "").ToString();
            if (!string.IsNullOrEmpty(regPath)) return regPath;

            return MMDefaultInstallPath;
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
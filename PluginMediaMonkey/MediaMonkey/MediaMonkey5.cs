using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;
using MediaMonkeyNet;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace MediaMonkey
{
    // ANY exception should to be handled, otherwise there is
    // a high risk of killing rainmeter

    public class MediaMonkey5 : IMediaMonkey
    {
        readonly int MMMajorVersion = 5;
        private const int CooldownDelay = 3000;
        private bool OnCooldown;

        private List<Cover> AlbumArt;
        private string _Cover = "";
        private Player Player;
        private Track CurrentTrack;
        private Task RefreshPlayerTask;
        private CancellationTokenSource RefreshPlayerCts;
        private Task RefreshTrackTask;
        private CancellationTokenSource RefreshTrackCts;
        private Task RefreshCoverTask;
        private CancellationTokenSource RefreshCoverCts;
        private Task InitializeTask;
        private CancellationTokenSource InitializeCts;

        int MediaMonkeyProcessId;
        string MediaMonkeyPath;

        private MediaMonkeyNet.MediaMonkeyNet mm;

        public MediaMonkey5()
        {
            MediaMonkeyPath = GetExecutablePath();
    }

        public MediaMonkey5(string ExecutablePath)
        {
            if (!string.IsNullOrWhiteSpace(ExecutablePath))
            {
                MediaMonkeyPath = ExecutablePath;
            }
        }


        public async void Initialize()
        {
            LogMessageToFile("init");
            if (OnCooldown || !IsRunning())
            {
                LogMessageToFile("init on cooldown");
                return;
            }

            // Polling at high rates is generally OK
            // but trying to initialize the api at high rates
            // can cause issues for MM


            OnCooldown = false;
            //OnCooldown = true;

            //System.Threading.Timer cooldown = null;
            //cooldown = new System.Threading.Timer((obj) =>
            //{
            //    OnCooldown = false;
            //    cooldown.Dispose();
            //},
            //null, CooldownDelay, System.Threading.Timeout.Infinite);
            if (InitializeTask != null) LogMessageToFile("update inittaskstatus:" + InitializeTask.Status.ToString());

            if (InitializeTask == null
                || InitializeTask.Status.Equals(TaskStatus.Canceled)
                || InitializeTask.Status.Equals(TaskStatus.Faulted)
                || InitializeTask.Status.Equals(TaskStatus.RanToCompletion)
                || InitializeTask.Status.Equals(TaskStatus.WaitingToRun)
                )
            {
                try
                {
                    LogMessageToFile("init task start");
                    InitializeTask = InitializeAsync();
                    LogMessageToFile("init task await");
                    await InitializeTask;
                    LogMessageToFile("init task done");

                }
                catch (Exception ex)
                {
                    LogMessageToFile("init catch ");
                    LogMessageToFile(ex.Message);
                    Dispose();
                }
            }
            LogMessageToFile("init done");
        }

        async private Task InitializeAsync()
        {
            try
            {
                InitializeCts = new CancellationTokenSource();
                await Task.Factory.StartNew(() => mm = new MediaMonkeyNet.MediaMonkeyNet(), InitializeCts.Token);
                await Task.Factory.StartNew(() => Player = new Player(mm), InitializeCts.Token);
            }
            catch (Exception ex)
            {
                LogMessageToFile("init catch 2");
                LogMessageToFile(ex.Message);
                mm = null;
                Dispose();
            }

        }

        public bool IsInitialized()
        {
            if (!IsRunning())
            {
                // Currently initializing or not running
                // try again later
                return false;
            }

            bool activeSession = false;

            if (mm != null)
            {
                try
                {
                    activeSession = mm.HasActiveSession();
                }
                catch
                {
                    Dispose();
                    return false;
                }
            }

            if (!activeSession)
            {
                Dispose();
            }

            return activeSession;
        }

        public bool IsRunning()
        {
            //To check if mediamonkey is running
            //First check if a process id was already set previously, 
            //Verify if that process is mediamonkey and return true
            //Otherwise clear the process id and initiated status
            //If no process id was found, look for the mediamonkey process
            //and if found, save its process id and return true

            if (MediaMonkeyProcessId == 0)
            {
                List<Process> mmProcessCollection = Process.GetProcessesByName("MediaMonkey").ToList();
                mmProcessCollection.AddRange(Process.GetProcessesByName("MEDIAM~2"));

                if (mmProcessCollection.Count > 0)
                {
                    // Make sure that the found the correct process

                    foreach (var proc in mmProcessCollection)
                    {
                        try
                        {
                            if (String.Equals(NormalizePath(proc.MainModule.FileName)
                                , MediaMonkeyPath
                                , StringComparison.OrdinalIgnoreCase))
                            {
                                MediaMonkeyProcessId = proc.Id;
                                return true;
                            }
                        }
                        catch (Exception)
                        {
                            // Nothing to do here but all exceptions
                            // MUST be handled to prevent crashing
                            // the host application
                        }
                    }
                }
                // Couldn't find the correct process, dispose of the mm object
                MediaMonkeyProcessId = 0;
                //Dispose();
            }
            else
            {
                try
                {
                    Process mmProc = Process.GetProcessById(MediaMonkeyProcessId);

                    // Verify that the previously found ID actually is Mediamonkey
                    // It's unlikely, but not impossible that another process has the id

                    if (String.Equals(NormalizePath(mmProc.MainModule.FileName), MediaMonkeyPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        MediaMonkeyProcessId = 0;
                        //Dispose();
                    }

                }
                catch (Exception)
                {
                    MediaMonkeyProcessId = 0;
                    //Dispose();
                }
            }
            return false;
        }

        private string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        public void Dispose()
        {
            LogMessageToFile("startdispose");
            if (InitializeCts != null) InitializeCts.Cancel();
            if (RefreshPlayerCts != null) RefreshPlayerCts.Cancel();
            if (RefreshTrackCts != null) RefreshTrackCts.Cancel();
            if (RefreshCoverCts != null) RefreshCoverCts.Cancel();
            CurrentTrack = null;
            Player = null;
            AlbumArt = null;
            _Cover = "";

            if (mm != null)
            {
                try
                {
                    if (IsRunning())
                    {
                        if (mm != null)
                        {
                            mm.Dispose();
                        }
                    } 

                }
                catch
                {
                }
                mm = null;
            }
            LogMessageToFile("enddispose");
        }

        public async void Update(bool updateAlbumArt)
        {
            // UpdateAlbumArt is by far the most expensive operation,
            // only update if needed

            LogMessageToFile("update start");
            if (!IsInitialized())
            {
                LogMessageToFile("update noinit");
                Initialize();
                return;
            }

            try
            {
                await UpdateTrack();
                await UpdatePlayer();
                LogMessageToFile("update cover start");
                if(updateAlbumArt) await UpdateAlbumArt();
                LogMessageToFile("update cover done");
            }
            catch (Exception ex)
            {
                LogMessageToFile("update catch");
                LogMessageToFile(ex.Message);
                LogMessageToFile(ex.InnerException.Message);
                Dispose();
            }
            LogMessageToFile("update done");
        }

        async private Task UpdateAsync()
        {
            try
            {
                RefreshPlayerCts = new CancellationTokenSource();
                await Task.Factory.StartNew(() => CurrentTrack = mm.GetCurrentTrack(), RefreshTrackCts.Token);
            }
            catch
            {
                LogMessageToFile("update catch 2");
                throw;
            }
        }

        public async Task UpdateTrack()
        {
            // Attempt to update the currently playing track
            LogMessageToFile("updatetrack start");

            if (RefreshTrackTask != null) LogMessageToFile("updatetrack taskstatus:" + RefreshTrackTask.Status.ToString());
            
            if (RefreshTrackTask == null
                || RefreshTrackTask.Status.Equals(TaskStatus.Canceled)
                || RefreshTrackTask.Status.Equals(TaskStatus.Faulted)
                || RefreshTrackTask.Status.Equals(TaskStatus.RanToCompletion)
                || RefreshTrackTask.Status.Equals(TaskStatus.WaitingForActivation)
                || RefreshTrackTask.Status.Equals(TaskStatus.WaitingToRun)
                )
            {
                try
                {
                    LogMessageToFile("update trackrefresh");
                    RefreshTrackTask = UpdateTrackAsync();
                    LogMessageToFile("update trackrefresh await");
                    await RefreshTrackTask;
                    //LogMessageToFile("update trackrefresh done");
                    LogMessageToFile("update tracktaskstatus:" + RefreshTrackTask.Status.ToString());
                }
                catch
                {
                    throw;
                }
            }
        }

        async private Task UpdateTrackAsync()
        {
            try
            {
                RefreshTrackCts = new CancellationTokenSource();
                await Task.Factory.StartNew(() => CurrentTrack = mm.GetCurrentTrack(), RefreshTrackCts.Token);
            }
            catch
            {
                LogMessageToFile("updatetrack catch 2");
                throw;
            }
            return;
        }

        public async Task UpdatePlayer()
        {
            LogMessageToFile("updatePlayer start");

            if (RefreshPlayerTask != null) LogMessageToFile("updatePlayer taskstatus:" + RefreshPlayerTask.Status.ToString());

            if (RefreshPlayerTask == null
                || RefreshPlayerTask.Status.Equals(TaskStatus.Canceled)
                || RefreshPlayerTask.Status.Equals(TaskStatus.Faulted)
                || RefreshPlayerTask.Status.Equals(TaskStatus.RanToCompletion)
                || RefreshPlayerTask.Status.Equals(TaskStatus.WaitingForActivation)
                || RefreshPlayerTask.Status.Equals(TaskStatus.WaitingToRun))
            {

                try
                {
                    LogMessageToFile("update playerrefresh");
                    RefreshPlayerTask = UpdatePlayerAsync();
                    LogMessageToFile("update playerrefresh await");
                    await RefreshPlayerTask;
                    LogMessageToFile("update playertaskstatus:" + RefreshPlayerTask.Status.ToString());
                }
                catch
                {
                    throw;
                }
            }
        }

        async private Task UpdatePlayerAsync()
        {
            try
            {
                RefreshPlayerCts = new CancellationTokenSource();
                await Task.Factory.StartNew(() => Player.Refresh(), RefreshPlayerCts.Token);
            }
            catch
            {
                LogMessageToFile("updateplayer catch 2");
                throw;
            }
            return;
        }

        public async Task UpdateAlbumArt()
        {
            // Returns path of the album art cover
            // Currently, art stored in tags is not supported
            // albumart tagged as 'Cover (front)' is preferred
            // If multiple covers are found, the higher sorted one
            // gets precedence
            // If no correctly tagged art ist found, the hightest sorted
            // gets precedence

            if (RefreshCoverTask == null
                || RefreshCoverTask.Status.Equals(TaskStatus.Canceled)
                || RefreshCoverTask.Status.Equals(TaskStatus.Faulted)
                || RefreshCoverTask.Status.Equals(TaskStatus.RanToCompletion)
                || RefreshCoverTask.Status.Equals(TaskStatus.WaitingForActivation)
                || RefreshCoverTask.Status.Equals(TaskStatus.WaitingToRun)
                )
            {
                try
                {
                    RefreshCoverTask = UpdateAlbumArtrAsync();
                    await RefreshCoverTask;

                    var filteredCoverList = AlbumArt.Where(x => x.CoverStorage == 1).ToList();

                    if (filteredCoverList.Count > 0)
                    {

                        var covers = filteredCoverList.Where(x => x.CoverType == 3).ToList();
                        if (covers.Count > 0)
                        {
                            _Cover = covers.FirstOrDefault().PicturePath;
                        }
                        else
                        {
                            // couldn't find art with the correct tag, use the first one
                            _Cover = filteredCoverList.FirstOrDefault().PicturePath;
                        }
                    }
                    else
                    {
                        _Cover = "";
                    }
                }
                catch
                {
                    throw;
                }
            }
        }

        async private Task UpdateAlbumArtrAsync()
        {
            try
            {
                RefreshCoverCts = new CancellationTokenSource();
                await Task.Factory.StartNew(() => AlbumArt = mm.GetCoverList(), RefreshCoverCts.Token);
            }
            catch
            {
                throw;
            }
        }


        //Properties

        public int MajorVersion()
        {
            return MMMajorVersion;
        }

        public string Title()
        {
            if (CurrentTrack != null)
            {
                string debugTitle = CurrentTrack.Title;
                LogMessageToFile(debugTitle);
                return debugTitle;
            }
            return "";
        }

        public string Artist()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.ArtistName;
            }
            return "";
        }

        public bool IsPlaying()
        {
            if (Player != null)
            {
                return Player.IsPlaying;
            }
            return false;
        }

        public bool IsPaused()
        {
            if (Player != null)
            {
                return Player.IsPaused;
            }
            return false;
        }

        public string Album()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.AlbumName;
            }
            return "";
        }

        public string Number()
        {
            //Value is returned as string
            //since MediaMonkey supports
            //string values as track number
            //TrackOrder as int is deprecated since MM3
            if (CurrentTrack != null)
            {
                return CurrentTrack.TrackOrder;
            }

            return "";
        }

        public int Year()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Year;
            }
            return 0;
        }

        public int Rating()
        {
            //Returns the numerical track rating:
            // -1 = Unknown
            // 0 = 0 Stars/Bomb
            // 10 = 0.5 stars
            // 20 = 1 star
            // 30 = 1.5 stars
            // 40 = 2 stars
            // 50 = 2.5 stars
            // 60 = 3 stars
            // 70 = 3.5 stars
            // 80 = 4 stars
            // 90 = 4.5 stars
            // 100 = 5 stars

            if (CurrentTrack != null)
            {
                return CurrentTrack.Rating;
            }

            return -1;
        }

        public bool IsShuffle()
        {
            if (Player != null)
            {
                return Player.IsShuffle;
            }
            return false;
        }

        public string Genre()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Genre;
            }
            return "";
        }

        public string Cover()
        {
            return _Cover;
        }

        //public string Cover()
        //{
        //    // Returns path of the album art cover
        //    // Currently, art stored in tags is not supported
        //    // albumart tagged as 'Cover (front)' is preferred
        //    // If multiple covers are found, the higher sorted one
        //    // gets precedence
        //    // If no correctly tagged art ist found, the hightest sorted
        //    // gets precedence

        //    string AlbumArtPath = "";

        //    if (!IsInitialized())
        //    {
        //        Initialize();
        //        return "";
        //    }

        //    if (!AsyncQueue["UpdateCover"])
        //    {
        //        try
        //        {
        //            List<Cover> coverList = mm.GetCoverList(false).Where(x => x.CoverStorage == 1).ToList();

        //            if (coverList.Count > 0)
        //            {

        //                var covers = coverList.Where(x => x.CoverType == 3).ToList();
        //                if (covers.Count > 0)
        //                {
        //                    AlbumArtPath = covers.FirstOrDefault().PicturePath;
        //                }
        //                else
        //                {
        //                    // couldn't find art with the correct tag, use the first one
        //                    AlbumArtPath = coverList.FirstOrDefault().PicturePath;
        //                }
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            Dispose();
        //        }
        //    }
        //    return AlbumArtPath;
        //}

        //public void UpdateCover()
        //{
        //    AsyncQueue["UpdateCover"] = true;

        //    try
        //    {
        //        List<Cover> coverList = mm.GetCoverList(false).Where(x => x.CoverStorage == 1).ToList();

        //        if (coverList.Count > 0)
        //        {

        //            var covers = coverList.Where(x => x.CoverType == 3).ToList();
        //            if (covers.Count > 0)
        //            {
        //                _Cover = covers.FirstOrDefault().PicturePath;
        //            }
        //            else
        //            {
        //                // couldn't find art with the correct tag, use the first one
        //                _Cover = coverList.FirstOrDefault().PicturePath;
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //        Dispose();
        //    }
        //    finally
        //    {
        //        AsyncQueue["UpdateCover"] = false;
        //    }
        //}

        public string File()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Path;
            }
            return "";
        }

        public int Duration()
        {
            //Length of current track in Seconds
            if (Player != null)
            {
                return (int)(Player.TrackLength / 1000);
            }
            return 0;
        }

        public int Position()
        {
            //Current position of the playing track in seconds
            if (Player != null)
            {
                return (int)(Player.TrackPosition / 1000);
            }
            return 0;
        }

        public int Progress()
        {
            //Playback percentage of the current track
            if (Player != null)
            {
                return (int)(Player.Progress * 100);
            }
            return 0;
        }

        public int Volume()
        {
            //Volume of the player between 0 and 100
            if (Player != null)
            {
                return (int)(Player.Volume * 100);
            }
            return 0;
        }

        public bool IsRepeat()
        {
            if (Player != null)
            {
                return Player.IsRepeat;
            }
            return false;
        }

        public string Disc()
        {
            //Value is returned as string
            //since MediaMonkey supports
            //string values as disc number

            if (CurrentTrack != null)
            {
                return CurrentTrack.DiscNumber;
            }
            return "";
        }

        public string Custom1()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Custom1;
            }
            return "";
        }

        public string Custom2()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Custom2;
            }
            return "";
        }

        public string Custom3()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Custom3;
            }
            return "";
        }

        public string Custom4()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Custom4;
            }
            return "";
        }

        public string Custom5()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Custom5;
            }
            return "";
        }

        public string Conductor()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Conductor;
            }
            return "";
        }

        public int FileID()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.ID;
            }
            return -1;
        }

        public string Grouping()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Grouping;
            }
            return "";
        }

        public string AlbumArtist()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.AlbumArtistName;
            }
            return "";
        }

        public string Publisher()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.Publisher;
            }
            return "";
        }

        public string Composer()
        {
            if (CurrentTrack != null)
            {
                return CurrentTrack.MusicComposer;
            }
            return "";
        }

        //Actions

        public void Pause()
        {
            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                Player.PausePlayback();
            }
        }

        public void Play()
        {
            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                Player.StartPlayback();
            }
        }

        public void PlayPause()
        {
            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                Player.TogglePlayback();
            }
        }

        public void Stop()
        {
            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                Player.StopPlayback();
            }
        }

        public void Next()
        {
            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                Player.NextTrack();
            }
        }

        public void Previous()
        {
            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                Player.PreviousTrack();
            }
        }

        public void SetRating(int Rating)
        {
            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (CurrentTrack != null)
            {
                CurrentTrack.SetRating(mm, Rating);
            }
        }

        public string GetExecutablePath()
        {
            //Attempts to find path to the mediamonkey executable
            return Registry.GetValue(
                "HKEY_LOCAL_MACHINE\\SOFTWARE\\Clients\\Media\\MediaMonkey\\shell\\open\\command"
                , "", "").ToString();
        }

        public void OpenPlayer()
        {
            //TODO:Add feedback in main api if the path is empty
            if (MediaMonkeyPath != "")
            {
                try
                {
                    Process.Start(MediaMonkeyPath);
                }
                catch (Exception)
                {
                }
            }

        }

        public void ClosePlayer()
        {

            //TODO:Implement better process validation logic from IsRunning
            if (IsRunning())
            {
                try
                {
                    Process mmProc = Process.GetProcessById(MediaMonkeyProcessId);
                    mmProc.CloseMainWindow();
                }
                catch
                {
                }
                finally
                {
                    Dispose();
                }
            }
        }

        public void TogglePlayer()
        {
            if (IsRunning())
            {
                ClosePlayer();
            }
            else
            {
                OpenPlayer();
            }
        }

        public void SetPosition(int Position)
        {
            //Set playback position to the given value
            //e.g. SetPosition(50) jumps to 50% of the playing track 
            //Values above 100 or below 0 set the time to 100% and 0%
            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                if (Position > 100)
                {
                    Position = 100;
                }

                if (Position < 0)
                {
                    Position = 0;
                }

                Player.SetProgress((double)Position / 100);
            }
        }

        public void SetShuffle(int Shuffle)
        {
            // 1 - Shuffle on
            // 0 - Shuffle off
            // -1 - Toggle shuffle

            //TODO:Add feedback in main plugin for invalid values

            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                switch (Shuffle)
                {
                    case 1:
                        Player.SetShuffle(true);
                        break;

                    case 0:
                        Player.SetShuffle(false);
                        break;

                    case -1:
                        if (Player.IsShuffle)
                        {
                            Player.SetShuffle(false);
                        }
                        else
                        {
                            Player.SetShuffle(true);
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public void SetRepeat(int Repeat)
        {
            // 1 - Repeat on
            // 0 - Repeat off
            // -1 - Toggle repeat
            //TODO:Add feedback in main plugin for invalid values

            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                switch (Repeat)
                {
                    case 1:
                        Player.SetRepeat(true);
                        break;

                    case 0:
                        Player.SetRepeat(false);
                        break;

                    case -1:
                        if (Player.IsRepeat)
                        {
                            Player.SetRepeat(false);
                        }
                        else
                        {
                            Player.SetRepeat(true);
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public void SetVolume(int Volume)
        {
            //Set volume of the player to the given value
            //Values above 100 or below 0 set the volume to 100% and 0%

            //TODO:Add feedback in main plugin for invalid values

            if (!IsInitialized())
            {
                Initialize();
                return;
            }

            if (Player != null)
            {
                if (Volume > 100)
                {
                    Volume = 100;
                }

                if (Volume < 0)
                {
                    Volume = 0;
                }
                Player.SetVolume((double)Volume / 100);
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;
using System.Diagnostics;
using Microsoft.Win32;
using MediaMonkeyNet;
using System.IO;

namespace MediaMonkey
{
    // ANY exception should to be handled, otherwise there is
    // a high risk of killing rainmeter

    public class MediaMonkey5 : IMediaMonkey
    {
        readonly int MMMajorVersion = 5;
        private int CooldownDelay = 3000; //in milliseconds
        private bool OnCooldown;

        private Track CurrentTrack;

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

        public bool Initialize()
        {
            if (OnCooldown || !IsRunning())
            {
                return false;
            }

            // Polling at high rates is generally OK
            // but trying to initialize the api at high rates
            // can cause issues for MM

            OnCooldown = true;

            System.Threading.Timer cooldown = null;
            cooldown = new System.Threading.Timer((obj) =>
            {
                OnCooldown = false;
                cooldown.Dispose();
            },
            null, CooldownDelay, System.Threading.Timeout.Infinite);

            try
            {
                LogMessageToFile("tryinitnewMM");
                // mm = new MediaMonkeyNet.MediaMonkeyNet();
                mm = new MediaMonkeyNet.MediaMonkeyNet("http://localhost:9222", false);
                LogMessageToFile("tryinitnewstep2");


                List<RemoteSessionsResponse> sessions = mm.GetAvailableSessions();
                if(sessions.Count == 0)
                {
                    mm = null;
                    return false;
                }
                mm.SetActiveSession(sessions.FirstOrDefault().webSocketDebuggerUrl);

                LogMessageToFile("aftertryinitnewMM");
                return IsInitialized();
            }
            catch
            {
                LogMessageToFile("catchinitnewMM");
                mm = null;
                Dispose();
                return false;
            }
        }

        public bool IsInitialized()
        {
            try
            {
                if (IsRunning() && mm != null)
                {
                    LogMessageToFile("intializedchecksession");

                    return mm.HasActiveSession();
                }
            }
            catch
            {
                Dispose();
            }

            return false;
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
                Dispose();
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
                        Dispose();
                    }

                }
                catch (Exception)
                {
                    MediaMonkeyProcessId = 0;
                    Dispose();
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
            CurrentTrack = null;

            if (mm != null)
            {
                try
                {
                    if (IsRunning())
                    {
                        LogMessageToFile("beforedispose");
                        mm.Dispose();
                        LogMessageToFile("afterdispose");
                    }
                }
                catch
                {
                }
                mm = null;
            }
        }

        public void Update()
        {
            LogMessageToFile("startupdate");
            // Attempt to update the currently playing track
            if (IsInitialized() || Initialize())
            {
                try
                {
                    LogMessageToFile("trygetcurrenttrack");
                    CurrentTrack = mm.GetCurrentTrack();
                    LogMessageToFile("aftergetcurrenttrack");
                }
                catch (Exception)
                {
                    LogMessageToFile("catchUpdateDispose");
                    Dispose();
                    LogMessageToFile("aftercatchUpdateDispose");
                }
            }
            else
            {
                CurrentTrack = null;
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
                return CurrentTrack.Title;
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
            if (IsInitialized() || Initialize())
            {
                try
                {
                    return mm.IsPlaying;
                }
                catch (Exception)
                {
                    Dispose();
                }
            }
            return false;
        }

        public bool IsPaused()
        {
            if (IsInitialized() || Initialize())
            {
                try
                {
                    return mm.IsPaused;
                }
                catch (Exception)
                {
                    Dispose();
                }
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
            if (IsInitialized() || Initialize())
            {
                try
                {
                    return mm.IsShuffle;
                }
                catch (Exception)
                {
                    Dispose();
                }
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
            // Returns path of the album art cover
            // Currently, art stored in tags is not supported
            // albumart tagged as 'Cover (front)' is preferred
            // If multiple covers are found, the higher sorted one
            // gets precedence
            // If no correctly tagged art ist found, the hightest sorted
            // gets precedence

            string AlbumArtPath = "";

            if (IsInitialized() || Initialize())
            {
                try
                {
                    List<Cover> coverList = mm.GetCoverList(false).Where(x => x.CoverStorage == 1).ToList();

                    if (coverList.Count > 0)
                    {

                        var covers = coverList.Where(x => x.CoverType == 3).ToList();
                        if (covers.Count > 0)
                        {
                            AlbumArtPath = covers.FirstOrDefault().PicturePath;
                        }
                        else
                        {
                            // couldn't find art with the correct tag, use the first one
                            AlbumArtPath = coverList.FirstOrDefault().PicturePath;
                        }
                    }
                }
                catch (Exception)
                {
                    Dispose();
                }
            }
            return AlbumArtPath;
        }

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
            if (IsInitialized() || Initialize())
            {
                try
                {
                    long mmDuration = mm.TrackLength;
                    return (int)(mmDuration / 1000);
                }
                catch (Exception)
                {
                    Dispose();
                }
            }
            return 0;
        }

        public int Position()
        {
            //Current position of the playing track in seconds
            if (IsInitialized() || Initialize())
            {
                try
                {
                    long mmDuration = mm.TrackPosition;
                    return (int)(mmDuration / 1000);
                }
                catch (Exception)
                {
                    Dispose();
                }
            }

            return 0;
        }

        public int Progress()
        {
            //Playback percentage of the current track
            if (IsInitialized() || Initialize())
            {
                return (int)(((double)mm.TrackPosition / mm.TrackLength) * 100);
            }

            return 0;
        }

        public int Volume()
        {
            //Volume of the player between 0 and 100
            if (IsInitialized() || Initialize())
            {
                return (int)(mm.Volume * 100);
            }

            return 0;
        }

        public bool IsRepeat()
        {
            if (IsInitialized() || Initialize())
            {
                try
                {
                    return mm.IsRepeat;
                }
                catch (Exception)
                {
                    Dispose();
                }
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
            if (IsInitialized() || Initialize())
            {
                mm.PausePlayback();
            }
        }

        public void Play()
        {
            if (IsInitialized() || Initialize())
            {
                mm.StartPlayback();
            }
        }

        public void PlayPause()
        {
            if (IsInitialized() || Initialize())
            {
                mm.TogglePlayback();
            }
        }

        public void Stop()
        {
            if (IsInitialized() || Initialize())
            {
                mm.StopPlayback();
            }
        }

        public void Next()
        {
            if (IsInitialized() || Initialize())
            {
                mm.NextTrack();
            }
        }

        public void Previous()
        {
            if (IsInitialized() || Initialize())
            {
                mm.PreviousTrack();
            }
        }

        public void SetRating(int Rating)
        {
            if (IsInitialized() || Initialize())
            {
                mm.SetRating(Rating);
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
            if (MediaMonkeyPath == "")
            {
                MediaMonkeyPath = GetExecutablePath();
            }

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
            if (IsRunning())
            {
                try
                {
                    Process mmProc = Process.GetProcessById(MediaMonkeyProcessId);
                    mmProc.CloseMainWindow();
                }
                catch (Exception)
                {
                }
                finally
                {
                    MediaMonkeyProcessId = 0;
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
            if (IsInitialized() || Initialize())
            {
                if (Position > 100)
                {
                    Position = 100;
                }

                if (Position < 0)
                {
                    Position = 0;
                }
                mm.SetTrackPosition(((int)mm.TrackLength / 100) * Position);
            }
        }

        public void SetShuffle(int Shuffle)
        {
            // 1 - Shuffle on
            // 0 - Shuffle off
            // -1 - Toggle shuffle
            if (IsInitialized() || Initialize())
            {
                switch (Shuffle)
                {
                    case 1:
                        mm.SetShuffle(true);
                        break;

                    case 0:
                        mm.SetShuffle(false);
                        break;

                    case -1:
                        if (mm.IsShuffle)
                        {
                            mm.SetShuffle(false);
                        }
                        else
                        {
                            mm.SetShuffle(true);
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

            if (IsInitialized() || Initialize())
            {
                switch (Repeat)
                {
                    case 1:
                        mm.SetRepeat(true);
                        break;

                    case 0:
                        mm.SetRepeat(false);
                        break;

                    case -1:
                        if (mm.IsRepeat)
                        {
                            mm.SetRepeat(false);
                        }
                        else
                        {
                            mm.SetRepeat(true);
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
            if (IsInitialized() || Initialize())
            {
                if (Volume > 100)
                {
                    Volume = 100;
                }

                if (Volume < 0)
                {
                    Volume = 0;
                }
                mm.SetVolume((double)Volume / 100.0);
            }
        }


        public string GetTempPath()
        {
            string path = System.Environment.GetEnvironmentVariable("TEMP");
            if (!path.EndsWith("\\")) path += "\\";
            return path;
        }

        public void LogMessageToFile(string msg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                GetTempPath() + "rainmeterdebug.txt");
            try
            {
                string logLine = System.String.Format(
                    "{0:G}: {1}.", System.DateTime.Now, msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }
    }
}

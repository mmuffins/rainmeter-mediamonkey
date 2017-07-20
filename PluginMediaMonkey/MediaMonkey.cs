using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Win32;
using MediaMonkeyNet;

namespace MediaMonkey
{
    // ANY exception should to be handled, otherwise there is
    // a high risk of killing rainmeter

    public interface IMediaMonkey
    {
        void Pause();
        void Play();
        void PlayPause();
        void Stop();
        void Previous();
        void Next();
        void OpenPlayer();
        void ClosePlayer();
        void TogglePlayer();
        void SetRating(int Rating);
        void SetPosition(int Position);
        void SetShuffle(int Shuffle);
        void SetRepeat(int Repeat);
        void SetVolume(int Volume);

        bool Initialize();
        bool IsInitialized();
        bool IsActive();
        bool IsRunning();
        bool IsPlaying();
        bool IsPaused();
        bool IsShuffle();
        bool IsRepeat();

        string Title();
        string Artist();
        string Album();
        string Number();
        string Genre();
        string Cover();
        string File();

        int MajorVersion();
        int Year();
        int Rating();
        int Duration();
        int Position();
        int Progress();
        int Volume();
    }

    public class MediaMonkey4 : IMediaMonkey
    {
        readonly int MMMajorVersion = 4;
        DateTime DisposeDate;
        static int DisposeTimeOut = 4000; //in milliseconds
        static int MediaMonkeyProcessId;

        static bool Initialized = false;
        static string MediaMonkeyPath;


        static SongsDB.SDBPlayer SDBPlayer;
        static SongsDB.SDBApplication SDBApplication;
        static SongsDB.SDBSongData SDBSongData;

        public MediaMonkey4()
        {
            MediaMonkeyPath = GetExecutablePath();
            IsActive();
        }

        public MediaMonkey4(string ExecutablePath)
        {
            if (string.IsNullOrEmpty(ExecutablePath))
            {
                MediaMonkeyPath = GetExecutablePath();
            }
            else
            {
                MediaMonkeyPath = ExecutablePath;
            }

            IsActive();
        }

        public bool Initialize()
        {

            //MediaMonkey autostarts whenever the com object is
            //instantized. If updates happen too frequently
            //it's possible a second instance of MM is created
            //when the com interface is queried shortly after the application was
            //closed, likely because the application is still technically running
            //while it's in the process of shutting down, but the com interface
            //was already disposed and therefore is re-initialized,
            //which results in a second instance of the application

            DateTime CurrentDate = DateTime.Now;

            if (((CurrentDate - DisposeDate).TotalMilliseconds) > DisposeTimeOut)
            {
                try
                {
                    SDBApplication = new SongsDB.SDBApplication();
                    SDBPlayer = new SongsDB.SDBPlayer();
                    SDBSongData = new SongsDB.SDBSongData();
                    SDBApplication.ShutdownAfterDisconnect = false;
                    Initialized = true;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        public bool IsInitialized()
        {
            return Initialized;
        }

        public bool IsRunning()
        {
            //To check if mediamonkey is running
            //First check if a process id was already set previously, 
            //Verify if that process is actually mediamonkey and return true
            //Otherwise clear the process id and the initiated status
            //If no process id was found, look for the mediamonkey process
            //and if found, save its process id and return true

            if (MediaMonkeyProcessId == 0)
            {
                Process[] mmProcessCollection = Process.GetProcessesByName("MediaMonkey");
                if (mmProcessCollection.Length > 0)
                {
                    MediaMonkeyProcessId = mmProcessCollection[0].Id;
                    return true;
                }
            }
            else
            {
                try
                {
                    Process mmProc = Process.GetProcessById(MediaMonkeyProcessId);

                    //Make sure that the process actually is MediaMonkey
                    //in case the application was closed and the ID was recycled

                    if (mmProc.MainModule.FileName == MediaMonkeyPath)
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

        public bool IsActive()
        {
            //Attempts to only return true if Mediamonkey
            //is running and the com interface properly initialized

            if (IsRunning() && (Initialized || Initialize()))
            {
                try
                {
                    return SDBApplication.IsRunning;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        public void Dispose()
        {
            Initialized = false;
            DisposeDate = DateTime.Now;
        }

        //Properties

        public int MajorVersion()
        {
            return this.MMMajorVersion;
        }

        public string Title()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Title;
            }

            return "";
        }

        public string Artist()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.ArtistName;
            }

            return "";
        }

        public bool IsPlaying()
        {
            if (IsActive())
            {
                return SDBPlayer.isPlaying;
            }
            else
            {
                return false;
            }
        }

        public bool IsPaused()
        {
            if (IsActive())
            {
                return SDBPlayer.isPaused;
            }
            else
            {
                return false;
            }
        }

        public string Album()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Album.Name;
            }

            return "";
        }

        public string Number()
        {
            //Value is returned as string
            //since MediaMonkey supports
            //string values as track number
            //TrackOrder as int is deprecated since MM3
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.TrackOrderStr;
            }

            return "";
        }

        public int Year()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Year;
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

            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Rating;
            }

            return -1;
        }

        public bool IsShuffle()
        {
            if (IsActive())
            {
                return SDBPlayer.isShuffle;
            }

            return false;
        }

        public string Genre()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Genre;
            }

            return "";
        }

        public string Cover()
        {
            //Returns path of the album art cover
            //Art that is stored as tag can not be displayed
            //Only albumart tagged as 'Cover (front)' in MM is considered
            //If multiple covers are found, the higher sorted one
            //gets precedence

            string AlbumArtPath = "";

            if (IsActive())
            {
                SongsDB.SDBAlbumArtList AlbumArt = SDBPlayer.CurrentSong.AlbumArt;

                if (AlbumArt.Count > 0)
                {
                    for (int i = AlbumArt.Count - 1; i >= 0; i--)
                    {
                        if ((AlbumArt.Item[i].ItemType == 3) && (AlbumArt.Item[i].ItemStorage == 1))
                        {
                            AlbumArtPath = AlbumArt.Item[i].PicturePath;
                        }
                    }
                }
            }

            return AlbumArtPath;
        }

        public string File()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Path;
            }

            return "";
        }

        public int Duration()
        {
            //Length of playing track in Seconds
            if (IsActive())
            {
                int mmDuration = SDBPlayer.CurrentSongLength;
                return (mmDuration / 1000);
            }

            return 0;
        }

        public int Position()
        {
            //Current position of the playing track in seconds
            if (IsActive())
            {
                int mmPosition = SDBPlayer.PlaybackTime;
                return (mmPosition / 1000);
            }

            return 0;
        }

        public int Progress()
        {
            //Playback percentage of the current track
            if (IsActive())
            {
                return (int)(((double)SDBPlayer.PlaybackTime / SDBPlayer.CurrentSongLength) * 100);
            }

            return 0;
        }

        public int Volume()
        {
            //Volume of the player between 0 and 100
            if (IsActive())
            {
                return (int)(SDBPlayer.Volume * 100);
            }

            return 0;
        }

        public bool IsRepeat()
        {
            if (IsActive())
            {
                return SDBPlayer.isRepeat;
            }

            return false;
        }

        //Actions

        public void Pause()
        {
            if (IsActive())
            {
                SDBPlayer.Pause();
            }
        }

        public void Play()
        {
            if (IsActive())
            {
                SDBPlayer.Play();
            }
        }

        public void PlayPause()
        {
            if (IsActive())
            {
                if (SDBPlayer.isPlaying)
                {
                    SDBPlayer.Pause();
                }
                else
                {
                    SDBPlayer.Play();
                }
            }
        }

        public void Stop()
        {
            if (IsActive())
            {
                SDBPlayer.Stop();
            }
        }

        public void Next()
        {
            if (IsActive())
            {
                SDBPlayer.Next();
            }
        }

        public void Previous()
        {
            if (IsActive())
            {
                SDBPlayer.Previous();
            }
        }

        public void SetRating(int Rating)
        {
            if (IsActive())
            {
                SDBPlayer.CurrentSong.Rating = Rating;
                SDBPlayer.CurrentSong.UpdateDB();
            }
        }

        public string GetExecutablePath()
        {
            //Attempts to find path to the mediamonkey exe
            return Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Clients\\Media\\MediaMonkey\\shell\\open\\command", "", "").ToString();
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
            if (IsActive())
            {
                if (Position > 100)
                {
                    Position = 100;
                }

                if (Position < 0)
                {
                    Position = 0;
                }

                SDBPlayer.PlaybackTime = (SDBPlayer.CurrentSongLength / 100) * Position;
            }
        }

        public void SetShuffle(int Shuffle)
        {
            //1 - Shuffle on
            //0 - Shuffle off
            //-1 - Toggle shuffle
            if (IsActive())
            {
                switch (Shuffle)
                {
                    case 1:
                        SDBPlayer.isShuffle = true;
                        break;

                    case 0:
                        SDBPlayer.isShuffle = false;
                        break;

                    case -1:
                        if (SDBPlayer.isShuffle)
                        {
                            SDBPlayer.isShuffle = false;
                        }
                        else
                        {
                            SDBPlayer.isShuffle = true;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public void SetRepeat(int Repeat)
        {
            //1 - Repeat on
            //0 - Repeat off
            //-1 - Toggle repeat

            if (IsActive())
            {
                switch (Repeat)
                {
                    case 1:
                        SDBPlayer.isRepeat = true;
                        break;

                    case 0:
                        SDBPlayer.isRepeat = false;
                        break;

                    case -1:
                        if (SDBPlayer.isRepeat)
                        {
                            SDBPlayer.isRepeat = false;
                        }
                        else
                        {
                            SDBPlayer.isRepeat = true;
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
            if (IsActive())
            {
                if (Volume > 100)
                {
                    Volume = 100;
                }

                if (Volume < 0)
                {
                    Volume = 0;
                }
                SDBPlayer.Volume = ((double)Volume / 100.0);
            }
        }
    }

    public class MediaMonkey5 : IMediaMonkey
    {
        readonly int MMMajorVersion = 5;
        static int CooldownDelay = 1000; //in milliseconds
        private bool OnCooldown;

        static int MediaMonkeyProcessId;
        static string MediaMonkeyPath;


        static SongsDB.SDBPlayer SDBPlayer;
        static SongsDB.SDBApplication SDBApplication;
        static SongsDB.SDBSongData SDBSongData;

        static MediaMonkeyNet.MediaMonkeyNet mm;

        public MediaMonkey5()
        {
            MediaMonkeyPath = GetExecutablePath();
            Initialize();
        }

        public MediaMonkey5(string ExecutablePath)
        {
            if (string.IsNullOrEmpty(ExecutablePath))
            {
                MediaMonkeyPath = GetExecutablePath();
            }
            else
            {
                MediaMonkeyPath = ExecutablePath;
            }
            Initialize();
        }

        public bool Initialize()
        {
            Console.WriteLine("init. cooldown = " + OnCooldown);
            if (OnCooldown)
            {
                Console.WriteLine("exit init");
                return false;
            }

            // Polling at high rates is generally OK
            // but trying to initialize the api at high rates
            // can cause issue for MM

            OnCooldown = true;
            Console.WriteLine("set cooldown = " + OnCooldown);

            System.Threading.Timer cooldown = null;
            cooldown = new System.Threading.Timer((obj) =>
            {
                OnCooldown = false;
                Console.WriteLine("reset cooldown = " + OnCooldown);
                cooldown.Dispose();
            },
            null, CooldownDelay, System.Threading.Timeout.Infinite);



            try
            {
                mm = new MediaMonkeyNet.MediaMonkeyNet("http://localhost:9222");
                List<RemoteSessionsResponse> sessions = mm.GetAvailableSessions();
                var endpointUrl = sessions[0].webSocketDebuggerUrl;
                mm.SetActiveSession(endpointUrl);
                return true;
            }
            catch
            {
                mm = null;
                return false;
            }
        }

        public bool IsInitialized()
        {
            return mm.HasActiveSession();
        }

        public bool IsRunning()
        {
            //To check if mediamonkey is running
            //First check if a process id was already set previously, 
            //Verify if that process is actually mediamonkey and return true
            //Otherwise clear the process id and the initiated status
            //If no process id was found, look for the mediamonkey process
            //and if found, save its process id and return true

            if (MediaMonkeyProcessId == 0)
            {
                Process[] mmProcessCollection = Process.GetProcessesByName("MediaMonkey");
                if (mmProcessCollection.Length > 0)
                {
                    MediaMonkeyProcessId = mmProcessCollection[0].Id;
                    return true;
                }
            }
            else
            {
                try
                {
                    Process mmProc = Process.GetProcessById(MediaMonkeyProcessId);

                    //Make sure that the process actually is MediaMonkey
                    //in case the application was closed and the ID was recycled

                    if (mmProc.MainModule.FileName == MediaMonkeyPath)
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

        public bool IsActive()
        {

            if (mm != null || Initialize())
            {
                try
                {
                    if (mm.HasActiveSession())
                    {
                        return true;
                    }
                }
                catch (Exception)
                {
                    mm = null;
                }
            }

            return false;
        }

        public void Dispose()
        {
            mm = null;
        }

        //Properties

        public int MajorVersion()
        {
            return this.MMMajorVersion;
        }

        public string Title()
        {
            if (IsActive())
            {
                Console.WriteLine("gettitle");

                try
                {
                    return mm.GetCurrentTrack().Title;
                }
                catch (Exception)
                {
                    mm = null;
                }
            }

            return "";
        }

        public string Artist()
        {
            if (IsActive())
            {
                return mm.GetCurrentTrack().Artist;

            }

            return "";
        }

        public bool IsPlaying()
        {
            if (IsActive())
            {
                return SDBPlayer.isPlaying;
            }
            else
            {
                return false;
            }
        }

        public bool IsPaused()
        {
            if (IsActive())
            {
                return SDBPlayer.isPaused;
            }
            else
            {
                return false;
            }
        }

        public string Album()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Album.Name;
            }

            return "";
        }

        public string Number()
        {
            //Value is returned as string
            //since MediaMonkey supports
            //string values as track number
            //TrackOrder as int is deprecated since MM3
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.TrackOrderStr;
            }

            return "";
        }

        public int Year()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Year;
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

            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Rating;
            }

            return -1;
        }

        public bool IsShuffle()
        {
            if (IsActive())
            {
                return SDBPlayer.isShuffle;
            }

            return false;
        }

        public string Genre()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Genre;
            }

            return "";
        }

        public string Cover()
        {
            //Returns path of the album art cover
            //Art that is stored as tag can not be displayed
            //Only albumart tagged as 'Cover (front)' in MM is considered
            //If multiple covers are found, the higher sorted one
            //gets precedence

            string AlbumArtPath = "";

            if (IsActive())
            {
                SongsDB.SDBAlbumArtList AlbumArt = SDBPlayer.CurrentSong.AlbumArt;

                if (AlbumArt.Count > 0)
                {
                    for (int i = AlbumArt.Count - 1; i >= 0; i--)
                    {
                        if ((AlbumArt.Item[i].ItemType == 3) && (AlbumArt.Item[i].ItemStorage == 1))
                        {
                            AlbumArtPath = AlbumArt.Item[i].PicturePath;
                        }
                    }
                }
            }

            return AlbumArtPath;
        }

        public string File()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Path;
            }

            return "";
        }

        public int Duration()
        {
            //Length of playing track in Seconds
            if (IsActive())
            {
                int mmDuration = SDBPlayer.CurrentSongLength;
                return (mmDuration / 1000);
            }

            return 0;
        }

        public int Position()
        {
            //Current position of the playing track in seconds
            if (IsActive())
            {
                int mmPosition = SDBPlayer.PlaybackTime;
                return (mmPosition / 1000);
            }

            return 0;
        }

        public int Progress()
        {
            //Playback percentage of the current track
            if (IsActive())
            {
                return (int)(((double)SDBPlayer.PlaybackTime / SDBPlayer.CurrentSongLength) * 100);
            }

            return 0;
        }

        public int Volume()
        {
            //Volume of the player between 0 and 100
            if (IsActive())
            {
                return (int)(SDBPlayer.Volume * 100);
            }

            return 0;
        }

        public bool IsRepeat()
        {
            if (IsActive())
            {
                return SDBPlayer.isRepeat;
            }

            return false;
        }

        //Actions

        public void Pause()
        {
            if (IsActive())
            {
                SDBPlayer.Pause();
            }
        }

        public void Play()
        {
            if (IsActive())
            {
                SDBPlayer.Play();
            }
        }

        public void PlayPause()
        {
            if (IsActive())
            {
                if (SDBPlayer.isPlaying)
                {
                    SDBPlayer.Pause();
                }
                else
                {
                    SDBPlayer.Play();
                }
            }
        }

        public void Stop()
        {
            if (IsActive())
            {
                SDBPlayer.Stop();
            }
        }

        public void Next()
        {
            if (IsActive())
            {
                SDBPlayer.Next();
            }
        }

        public void Previous()
        {
            if (IsActive())
            {
                SDBPlayer.Previous();
            }
        }

        public void SetRating(int Rating)
        {
            if (IsActive())
            {
                SDBPlayer.CurrentSong.Rating = Rating;
                SDBPlayer.CurrentSong.UpdateDB();
            }
        }

        public string GetExecutablePath()
        {
            //Attempts to find path to the mediamonkey exe
            return Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Clients\\Media\\MediaMonkey\\shell\\open\\command", "", "").ToString();
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
            if (IsActive())
            {
                if (Position > 100)
                {
                    Position = 100;
                }

                if (Position < 0)
                {
                    Position = 0;
                }

                SDBPlayer.PlaybackTime = (SDBPlayer.CurrentSongLength / 100) * Position;
            }
        }

        public void SetShuffle(int Shuffle)
        {
            //1 - Shuffle on
            //0 - Shuffle off
            //-1 - Toggle shuffle
            if (IsActive())
            {
                switch (Shuffle)
                {
                    case 1:
                        SDBPlayer.isShuffle = true;
                        break;

                    case 0:
                        SDBPlayer.isShuffle = false;
                        break;

                    case -1:
                        if (SDBPlayer.isShuffle)
                        {
                            SDBPlayer.isShuffle = false;
                        }
                        else
                        {
                            SDBPlayer.isShuffle = true;
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public void SetRepeat(int Repeat)
        {
            //1 - Repeat on
            //0 - Repeat off
            //-1 - Toggle repeat

            if (IsActive())
            {
                switch (Repeat)
                {
                    case 1:
                        SDBPlayer.isRepeat = true;
                        break;

                    case 0:
                        SDBPlayer.isRepeat = false;
                        break;

                    case -1:
                        if (SDBPlayer.isRepeat)
                        {
                            SDBPlayer.isRepeat = false;
                        }
                        else
                        {
                            SDBPlayer.isRepeat = true;
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
            if (IsActive())
            {
                if (Volume > 100)
                {
                    Volume = 100;
                }

                if (Volume < 0)
                {
                    Volume = 0;
                }
                SDBPlayer.Volume = ((double)Volume / 100.0);
            }
        }
    }


}

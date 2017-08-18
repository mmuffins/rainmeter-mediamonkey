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

    public class MediaMonkey4 : IMediaMonkey
    {
        readonly int MMMajorVersion = 4;
        DateTime DisposeDate;
        int DisposeTimeOut = 4000; //in milliseconds
        int MediaMonkeyProcessId;

        bool Initialized = false;
        string MediaMonkeyPath;


        SongsDB.SDBPlayer SDBPlayer;
        SongsDB.SDBApplication SDBApplication;
        SongsDB.SDBSongData SDBSongData;

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

        public void UpdateTrack()
        {
            // Not implemented for MM4
        }

        public void UpdatePlayer()
        {
            // Not implemented for MM4
        }

        public void UpdateAlbumArt()
        {
            // Not implemented for MM4
        }

        private string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }


        //Properties

        public int MajorVersion()
        {
            return MMMajorVersion;
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

        public string AlbumArtist()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.AlbumArtistName;
            }

            return "";
        }

        public string Custom1()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Custom1;
            }

            return "";
        }

        public string Custom2()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Custom2;
            }

            return "";
        }

        public string Custom3()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Custom3;
            }

            return "";
        }

        public string Custom4()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Custom4;
            }

            return "";
        }

        public string Custom5()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Custom5;
            }

            return "";
        }

        public string Publisher()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Publisher;
            }

            return "";
        }

        public int FileID()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.ID;
            }

            return -1;
        }

        public string Composer()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.MusicComposer;
            }

            return "";
        }

        public string Grouping()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Grouping;
            }

            return "";
        }

        public string Disc()
        {
            //Value is returned as string
            //since MediaMonkey supports
            //string values as disc number

            if (IsActive())
            {
                return SDBPlayer.CurrentSong.DiscNumberStr;
            }

            return "";
        }

        public string Conductor()
        {
            if (IsActive())
            {
                return SDBPlayer.CurrentSong.Conductor;
            }

            return "";
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

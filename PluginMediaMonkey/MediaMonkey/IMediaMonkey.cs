using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaMonkey
{
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
        void UpdateTrack();
        void UpdatePlayer();
        void UpdateAlbumArt();
        void Dispose();

        // bool Initialize();
        bool IsInitialized();
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
        string Custom1();
        string Custom2();
        string Custom3();
        string Custom4();
        string Custom5();
        string AlbumArtist();
        string Publisher();
        string Composer();
        string Grouping();
        string Disc();
        string Conductor();

        int MajorVersion();
        int Year();
        int Rating();
        int Duration();
        int Position();
        int Progress();
        int Volume();
        int FileID();
    }

}

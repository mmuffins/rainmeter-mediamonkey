#define DLLEXPORT_GETSTRING
#define DLLEXPORT_EXECUTEBANG

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Rainmeter;
using MediaMonkey;


namespace PluginMediaMonkey
{
    
    internal class Measure
    {

        internal enum MeasureType
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

        internal MeasureType Type = MeasureType.Title;
        internal IMediaMonkey MediaMonkey;

        internal virtual void Dispose()
        {
            MediaMonkey.Dispose();
        }

        internal virtual void Reload(Rainmeter.API rm, ref double maxValue)
        {
            string type = rm.ReadString("PlayerType", "", false);

            switch (type.ToLowerInvariant())
            {

                case "album":
                    Type = MeasureType.Album;
                    break;

                case "albumartist":
                    Type = MeasureType.AlbumArtist;
                    break;

                case "artist":
                    Type = MeasureType.Artist;
                    break;

                case "cover":
                    Type = MeasureType.Cover;
                    break;

                case "composer":
                    Type = MeasureType.Composer;
                    break;

                case "conductor":
                    Type = MeasureType.Conductor;
                    break;

                case "custom1":
                    Type = MeasureType.Custom1;
                    break;

                case "custom2":
                    Type = MeasureType.Custom2;
                    break;

                case "custom3":
                    Type = MeasureType.Custom3;
                    break;

                case "custom4":
                    Type = MeasureType.Custom4;
                    break;

                case "custom5":
                    Type = MeasureType.Custom5;
                    break;

                case "disc":
                    Type = MeasureType.Disc;
                    break;

                case "duration":
                    Type = MeasureType.Duration;
                    break;

                case "file":
                    Type = MeasureType.File;
                    break;

                case "fileid":
                    Type = MeasureType.FileID;
                    break;

                case "genre":
                    Type = MeasureType.Genre;
                    break;

                case "grouping":
                    Type = MeasureType.Grouping;
                    break;

                case "number":
                    Type = MeasureType.Number;
                    break;

                case "position":
                    Type = MeasureType.Position;
                    break;

                case "progress":
                    Type = MeasureType.Progress;
                    break;

                case "publisher":
                    Type = MeasureType.Publisher;
                    break;

                case "rating":
                    Type = MeasureType.Rating;
                    break;

                case "repeat":
                    Type = MeasureType.Repeat;
                    break;

                case "shuffle":
                    Type = MeasureType.Shuffle;
                    break;

                case "state":
                    Type = MeasureType.State;
                    break;

                case "status":
                    Type = MeasureType.Status;
                    break;

                case "title":
                    Type = MeasureType.Title;
                    break;

                case "volume":
                    Type = MeasureType.Volume;
                    break;

                case "year":
                    Type = MeasureType.Year;
                    break;

                default:
                    API.Log(API.LogType.Error, "MediaMonkey.dll: Measure type=" + type + " not valid");
                    break;
            }
        }

        internal virtual double Update()
        {
            return 0.0;
        }

#if DLLEXPORT_GETSTRING
        internal virtual string GetString()
        {
            return "";
        }
#endif

#if DLLEXPORT_EXECUTEBANG
        internal virtual void ExecuteBang(string args)
        {
        }
#endif
    }

    internal class ParentMeasure : Measure
    {
        internal static List<ParentMeasure> ParentMeasures = new List<ParentMeasure>();

        internal string Name;
        internal bool DisableLeadingZero = false;
        internal IntPtr Skin;
        internal bool EnableArtUpdate = false; // Updating albumart can be expensive, only update if needed


        internal ParentMeasure(IMediaMonkey MediaMonkey)
        {
            this.MediaMonkey = MediaMonkey;
            ParentMeasures.Add(this);

            API.Log(API.LogType.Debug, "Mediamonkey.dll: Created new ParentMeasure " + Name);
        }

        internal override void Dispose()
        {
            ParentMeasures.Remove(this);
        }

        internal override void Reload(Rainmeter.API api, ref double maxValue)
        {
            base.Reload(api, ref maxValue);

            Name = api.GetMeasureName();
            Skin = api.GetSkin();

            string DisableLeadingZeroString = api.ReadString("DisableLeadingZero", "false");

            if (DisableLeadingZeroString == "1")
            {
                DisableLeadingZero = true;
            }
            else if (DisableLeadingZeroString == "0")
            {
                DisableLeadingZero = false;
            }
            else
            {
                API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for DisableLeadingZero =" + DisableLeadingZeroString);
            }
        }

        internal override double Update()
        {
            MediaMonkey.Update(EnableArtUpdate);
            return ReturnValue(Type);
        }

        internal double ReturnValue(MeasureType type)
        {
            switch (type)
            {
                case MeasureType.Duration:
                    return MediaMonkey.Duration();

                case MeasureType.FileID:
                    return MediaMonkey.FileID();

                case MeasureType.Position:
                    return MediaMonkey.Position();

                case MeasureType.Progress:
                    return MediaMonkey.Progress();

                case MeasureType.Rating:
                    int mmRating = MediaMonkey.Rating();
                    double Stars = MediaMonkeyRatingToStars(mmRating);

                    if (Stars == -2.0)
                    {
                        API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid was returned for Rating =" + mmRating);
                        return 0.0;
                    }
                    else
                    {
                        return Stars;
                    }

                case MeasureType.Repeat:
                    return MediaMonkey.IsRepeat() ? 1.0 : 0.0;

                case MeasureType.Shuffle:
                    return MediaMonkey.IsShuffle() ? 1.0 : 0.0;

                case MeasureType.State:
                    if (MediaMonkey.IsPlaying())
                    {
                        return MediaMonkey.IsPaused() ? 2.0 : 1.0;
                    }
                    else
                    {
                        return (double)0.0;
                    }

                case MeasureType.Status:
                    return MediaMonkey.IsRunning() ? 1.0 : 0.0;

                case MeasureType.Volume:
                    return MediaMonkey.Volume();

                case MeasureType.Year:
                    return MediaMonkey.Year();

                default:
                    break;
            }
            return 0.0;
        } 

#if DLLEXPORT_GETSTRING
        internal override string GetString()
        {
            return ReturnString(Type);
        }

        internal string ReturnString(MeasureType type)
        {
            switch (type)
            {
                case MeasureType.Album:
                    return MediaMonkey.Album();

                case MeasureType.AlbumArtist:
                    return MediaMonkey.AlbumArtist();

                case MeasureType.Artist:
                    return MediaMonkey.Artist();

                case MeasureType.Composer:
                    return MediaMonkey.Composer();

                case MeasureType.Conductor:
                    return MediaMonkey.Conductor();

                case MeasureType.Cover:
                    EnableArtUpdate = true;
                    return MediaMonkey.Cover();

                case MeasureType.Custom1:
                    return MediaMonkey.Custom1();

                case MeasureType.Custom2:
                    return MediaMonkey.Custom2();

                case MeasureType.Custom3:
                    return MediaMonkey.Custom3();

                case MeasureType.Custom4:
                    return MediaMonkey.Custom4();

                case MeasureType.Custom5:
                    return MediaMonkey.Custom5();

                case MeasureType.Disc:
                    return MediaMonkey.Disc();

                case MeasureType.Duration:
                    TimeSpan mmDuration = TimeSpan.FromSeconds(MediaMonkey.Duration());

                    string mmDurationFormat;

                    if (DisableLeadingZero)
                    {
                        mmDurationFormat = mmDuration.ToString(@"m\:ss");
                    }
                    else
                    {
                        mmDurationFormat = mmDuration.ToString(@"mm\:ss");
                    }

                    return mmDurationFormat;

                case MeasureType.File:
                    return MediaMonkey.File();

                case MeasureType.FileID:
                    return MediaMonkey.FileID().ToString();


                case MeasureType.Genre:
                    return MediaMonkey.Genre();

                case MeasureType.Grouping:
                    return MediaMonkey.Grouping();

                case MeasureType.Number:
                    return MediaMonkey.Number();

                case MeasureType.Position:
                    TimeSpan mmPosition = TimeSpan.FromSeconds(MediaMonkey.Position());
                    string mmPositionFormat = DisableLeadingZero ? mmPosition.ToString(@"m\:ss") : mmPosition.ToString(@"mm\:ss");

                    return mmPositionFormat;

                case MeasureType.Progress:
                    return MediaMonkey.Progress().ToString();

                case MeasureType.Publisher:
                    return MediaMonkey.Publisher();

                case MeasureType.Rating:
                    int mmRating = MediaMonkey.Rating();
                    double Stars = MediaMonkeyRatingToStars(mmRating);

                    if (Stars == -2.0)
                    {
                        return "";
                    }
                    else
                    {
                        return Stars.ToString();
                    }

                case MeasureType.Repeat:
                    return MediaMonkey.IsRepeat() ? "1" : "0";

                case MeasureType.Shuffle:
                    return MediaMonkey.IsShuffle() ? "1" : "0";

                case MeasureType.State:
                    if (MediaMonkey.IsPlaying())
                    {
                        return MediaMonkey.IsPaused() ? "2" : "1";
                    }
                    else
                    {
                        return "0";
                    }

                case MeasureType.Status:
                    return MediaMonkey.IsRunning() ? "1" : "0";

                case MeasureType.Title:
                    return MediaMonkey.Title();

                case MeasureType.Volume:
                    return MediaMonkey.Volume().ToString();

                case MeasureType.Year:
                    return MediaMonkey.Year().ToString();

                default:
                    break;
            }

            return "";
        }
#endif

#if DLLEXPORT_EXECUTEBANG
        internal override void ExecuteBang(string args)
        {
            //Some bangs pass multiple parameters, only the first one is
            //needed to identify the correct bang
            string[] argsArray = args.Split(' ');

            switch (argsArray[0].ToLowerInvariant())
            {
                case "play":
                    MediaMonkey.Play();
                    break;

                case "pause":
                    MediaMonkey.Pause();
                    break;

                case "playpause":
                    MediaMonkey.PlayPause();
                    break;

                case "stop":
                    MediaMonkey.Stop();
                    break;

                case "previous":
                    MediaMonkey.Previous();
                    break;

                case "next":
                    MediaMonkey.Next();
                    break;

                case "openplayer":
                    MediaMonkey.OpenPlayer();
                    break;

                case "closeplayer":
                    MediaMonkey.ClosePlayer();
                    break;

                case "toggleplayer":
                    MediaMonkey.TogglePlayer();
                    break;

                case "setrating":
                    double argsRating;

                    if (double.TryParse(argsArray[1], out argsRating))
                    {
                        int mmRating = StarsToMediaMonkeyRating(argsRating);

                        if (mmRating == -2)
                        {
                            API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for SetRating =" + argsArray[1]);
                        }
                        else
                        {
                            API.Log(API.LogType.Error, "MediaMonkey.dll: call SetRating =" + mmRating);
                            MediaMonkey.SetRating(mmRating);
                        }
                    }
                    else
                    {
                        API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for SetRating =" + argsArray[1]);
                    }
                    break;

                case "setposition":
                    int argsPosition;

                    if (int.TryParse(argsArray[1], out argsPosition))
                    {
                        MediaMonkey.SetPosition(argsPosition);
                    }
                    else
                    {
                        API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for SetPosition =" + argsArray[1]);
                    }
                    break;

                case "setshuffle":
                    int argsShuffle;

                    if (int.TryParse(argsArray[1], out argsShuffle))
                    {
                        switch (argsShuffle)
                        {
                            case -1:
                            case 0:
                            case 1:
                                MediaMonkey.SetShuffle(argsShuffle);
                                break;

                            default:
                                API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for SetShuffle =" + argsArray[1]);
                                break;
                        }
                    }
                    else
                    {
                        API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for SetShuffle =" + argsArray[1]);
                    }
                    break;

                case "setrepeat":
                    int argsRepeat;

                    if (int.TryParse(argsArray[1], out argsRepeat))
                    {
                        switch (argsRepeat)
                        {
                            case -1:
                            case 0:
                            case 1:
                                MediaMonkey.SetRepeat(argsRepeat);
                                break;

                            default:
                                API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for SetRepeat =" + argsArray[1]);
                                break;
                        }
                    }
                    else
                    {
                        API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for SetRepeat =" + argsArray[1]);
                    }
                    break;

                case "setvolume":
                    int argsVolume;

                    Match VolumeRegex = Regex.Match(argsArray[1], "([+-]?)(\\d+)");

                    if (int.TryParse(VolumeRegex.Groups[2].Value, out argsVolume))
                    {
                        if (VolumeRegex.Groups[1].Value == "")
                        {
                            MediaMonkey.SetVolume(argsVolume);
                        }
                        else if (VolumeRegex.Groups[1].Value == "+")
                        {
                            IncreaseVolume(argsVolume);
                        }
                        else if (VolumeRegex.Groups[1].Value == "-")
                        {
                            DecreaseVolume(argsVolume);
                        }
                        else
                        {
                            API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for SetVolume =" + argsArray[1]);
                        }
                    }
                    else
                    {
                        API.Log(API.LogType.Error, "MediaMonkey.dll: Invalid parameter for SetVolume =" + argsArray[1]);
                    }



                    break;
            }
        }
#endif
        internal int StarsToMediaMonkeyRating(double Rating)
        {
            //Converts Ratings from -1 to 5 to the 
            //value needed internally for MediaMonkey
            //For invalid values -2 will be returned
            int mmRating = -2;

            if (Rating >= -1.0 && Rating <= 5.0)
            {
                if (Rating == -1.0)
                {
                    mmRating = -1;
                }
                else if (Rating == 0.0)
                {
                    mmRating = 0;
                }
                else if (Rating == 0.5)
                {
                    mmRating = 10;
                }
                else if (Rating == 1.0)
                {
                    mmRating = 20;
                }
                else if (Rating == 1.5)
                {
                    mmRating = 30;
                }
                else if (Rating == 2.0)
                {
                    mmRating = 40;
                }
                else if (Rating == 2.5)
                {
                    mmRating = 50;
                }
                else if (Rating == 3.0)
                {
                    mmRating = 60;
                }
                else if (Rating == 3.5)
                {
                    mmRating = 70;
                }
                else if (Rating == 4.0)
                {
                    mmRating = 80;
                }
                else if (Rating == 4.5)
                {
                    mmRating = 90;
                }
                else if (Rating == 5.0)
                {
                    mmRating = 100;
                }
            }

            return mmRating;
        }

        internal double MediaMonkeyRatingToStars(int MediaMonkeyRating)
        {
            //Converts the Media Monkey Rating value
            //to a numerical value between 0 and 5
            //For invalid values -2 will be returned

            double Rating = -2;

            if (MediaMonkeyRating >= -1 && MediaMonkeyRating <= 100)
            {
                switch (MediaMonkeyRating)
                {
                    case -1:
                        Rating = -1.0;
                        break;

                    case 0:
                        Rating = 0.0;
                        break;

                    case 10:
                        Rating = 0.5;
                        break;

                    case 20:
                        Rating = 1.0;
                        break;

                    case 30:
                        Rating = 1.5;
                        break;

                    case 40:
                        Rating = 2.0;
                        break;

                    case 50:
                        Rating = 2.5;
                        break;

                    case 60:
                        Rating = 3.0;
                        break;

                    case 70:
                        Rating = 3.5;
                        break;

                    case 80:
                        Rating = 4.0;
                        break;

                    case 90:
                        Rating = 4.5;
                        break;

                    case 100:
                        Rating = 5.0;
                        break;

                    default:
                        break;
                }
            }

            return Rating;
        }

        internal void IncreaseVolume(int Volume)
        {
            //Increases Player volume by the passed percentage

            int CurrentVolume = MediaMonkey.Volume();
            MediaMonkey.SetVolume((CurrentVolume + Volume));
        }

        internal void DecreaseVolume(int Volume)
        {
            //Decreases Player volume by the passed percentage

            int CurrentVolume = MediaMonkey.Volume();
            MediaMonkey.SetVolume((CurrentVolume - Volume));
        }
    }

    internal class ChildMeasure : Measure
        {
            private ParentMeasure ParentMeasure = null;
            internal string Name;

            internal override void Reload(Rainmeter.API api, ref double maxValue)
            {
                Name = api.GetMeasureName();
                API.Log(API.LogType.Debug, "Mediamonkey.dll: Reloading ChildMeasure=" + Name);

                base.Reload(api, ref maxValue);

                string parentName = api.ReadString("PlayerName", "", false).Replace("[", "").Replace("]","");
                IntPtr skin = api.GetSkin();

                API.Log(API.LogType.Debug, "Mediamonkey.dll: Looking for ParentMeasure" + parentName + "in skin " + skin + "for ChildMeasure " + Name);

                // Find parent using name AND the skin handle to be sure that it's the right one.
                ParentMeasure = null;
                foreach (ParentMeasure parentMeasure in ParentMeasure.ParentMeasures)
                {
                    if (parentMeasure.Skin.Equals(skin) && parentMeasure.Name.Equals(parentName))
                    {
                        API.Log(API.LogType.Debug, "Mediamonkey.dll: Found ParentMeasure " + parentMeasure.Name + " for ChildMeasure " + Name);
                        ParentMeasure = parentMeasure;
                    }
                }

                if (ParentMeasure == null)
                {
                    API.Log(API.LogType.Error, "Mediamonkey.dll: PlayerName=" + parentName + " not valid");
                }
            }

        internal override double Update()
        {
            if (ParentMeasure != null)
            {
                // API.Log(API.LogType.Debug, "Debug: childreload " + Type);
                return ParentMeasure.ReturnValue(Type);
            }
            return 0.0;
        }

            internal override string GetString()
            {
                if (ParentMeasure != null)
                    {
                    return ParentMeasure.ReturnString(Type);
                    }
                return "";
            }

            internal override void ExecuteBang(string args)
            {
                if (ParentMeasure != null)
                    {
                        ParentMeasure.ExecuteBang(args);
                    }
            }
        }

    public static class Plugin
    {
#if DLLEXPORT_GETSTRING
        static IntPtr StringBuffer = IntPtr.Zero;
#endif

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            Rainmeter.API api = new Rainmeter.API(rm);

            string mname = api.GetMeasureName();
            API.Log(API.LogType.Debug, "Mediamonkey.dll: Initializing Plugin for Measure " + mname);

            string mmVersion = api.ReadString("MMVersion", "");
            string PlayerName = api.ReadString("PlayerName", "",false);
            string PlayerPath = api.ReadString("PlayerPath", "", false);

            Measure measure;

            if (string.IsNullOrEmpty(PlayerName))
            {
                if (string.IsNullOrEmpty(PlayerPath))
                {
                    API.Log(API.LogType.Debug, "Mediamonkey.dll: Creating new ParentMeasure " + mname + " for MediaMonkey Version " + mmVersion);
                    switch (mmVersion)
                    {
                        case "4":
                            measure = new ParentMeasure(new MediaMonkey4());
                            break;

                        case "5":
                            measure = new ParentMeasure(new MediaMonkey5());
                            break;

                        default:
                            API.Log(API.LogType.Error, "MediaMonkey.dll: MMVersion=" + mmVersion + " not valid or no MMVersion value found, defaulting to Version 4");
                            measure = new ParentMeasure(new MediaMonkey4());
                            break;
                    }
                }
                else
                {
                    API.Log(API.LogType.Debug, "Mediamonkey.dll: Creating new ParentMeasure " + mname + " for MediaMonkey Version " + mmVersion + " with path " + PlayerPath);
                    switch (mmVersion)
                    {
                        case "4":
                            measure = new ParentMeasure(new MediaMonkey4(PlayerPath));
                            break;

                        case "5":
                            measure = new ParentMeasure(new MediaMonkey5(PlayerPath));
                            break;

                        default:
                            API.Log(API.LogType.Error, "MediaMonkey.dll: MMVersion=" + mmVersion + " not valid or no MMVersion value found, defaulting to Version 4");
                            measure = new ParentMeasure(new MediaMonkey4(PlayerPath));
                            break;
                    }
                }
            }
            else
            {
                API.Log(API.LogType.Debug, "Mediamonkey.dll: Creating new ChildMeasure " + mname + " for ParentMeasure " + PlayerName);
                measure = new ChildMeasure();
            }

            data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));

        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();

#if DLLEXPORT_GETSTRING
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
#endif
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

#if DLLEXPORT_GETSTRING
        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetString();
            if (stringValue != null)
            {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }
#endif

#if DLLEXPORT_EXECUTEBANG
        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(Marshal.PtrToStringUni(args));
        }
#endif
    }
}

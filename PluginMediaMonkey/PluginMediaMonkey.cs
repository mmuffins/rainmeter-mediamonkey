using System;
using System.Runtime.InteropServices;
using Rainmeter;

namespace PluginMediaMonkey
{
    public class Plugin
    {
        //private static MediaMonkey mm;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
            API api = (Rainmeter.API)rm;
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
            }
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            var api = (API)rm;
            var measure = (Measure)data;

            string measureType = api.ReadString("PlayerType", "", false);

            Measure.MeasureType parsedMeasure;
            if (!(Enum.TryParse(measureType, true, out parsedMeasure)))
            {
                api.LogF(API.LogType.Error, "MediaMonkey.dll: Measure type=" + measureType + " is not valid.");
                return;
            }

            string playerPath = api.ReadString("PlayerPath", "", false);
            if (!string.IsNullOrWhiteSpace(playerPath))
            {
                measure.MMInstallLocation = playerPath;
            }

            bool disableLeadingZero;
            if (bool.TryParse(api.ReadString("DisableLeadingZero", "0", false), out disableLeadingZero))
            {
                measure.DisableLeadingZero = disableLeadingZero;
            }

            int startupDelay;
            if (int.TryParse(api.ReadString("StartupDelay", "900", false), out startupDelay))
            {
                measure.StartupDelay = startupDelay;
            }

            measure.Reload(parsedMeasure, api);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)data;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            string measureString = measure.GetString();
            if(measureString != null)
            {
                measure.buffer = Marshal.StringToHGlobalUni(measureString);
            }

            return measure.buffer;
        }

        [DllExport]
        public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)]String args)
        {
            Measure measure = (Measure)data;
            measure.ExecuteBang(args.Split(' '));
        }

        //[DllExport]
        //public static IntPtr (IntPtr data, int argc,
        //    [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 1)] string[] argv)
        //{
        //    Measure measure = (Measure)data;
        //    if (measure.buffer != IntPtr.Zero)
        //    {
        //        Marshal.FreeHGlobal(measure.buffer);
        //        measure.buffer = IntPtr.Zero;
        //    }
        //
        //    measure.buffer = Marshal.StringToHGlobalUni("");
        //
        //    return measure.buffer;
        //}
    }
}


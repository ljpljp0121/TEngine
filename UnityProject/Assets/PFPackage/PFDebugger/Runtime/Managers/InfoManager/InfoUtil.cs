using UnityEngine;

namespace PFDebugger
{
    public static class InfoUtil
    {
        private const float INCHES_TO_CENTIMETERS = 2.54f;

        public static string GetBatteryLevelString(float batteryLevel)
        {
            if (batteryLevel < 0f)
            {
                return "Unavailable";
            }

            return batteryLevel.ToString("P0");
        }

        public static string GetResolutionString(Resolution resolution)
        {
#if UNITY_2022_3_OR_NEWER
            return $"{resolution.width} x {resolution.height} @ {resolution.refreshRateRatio}Hz";
#else
            return $"{resolution.width} x {resolution.height} @ {resolution.refreshRate}Hz";
#endif
        }

        public static float ScreenDPI
        {
            get
            {
                var dpi = Screen.dpi;
                if (dpi <= 0)
                {
                    dpi = 96f;
                }
                return dpi;
            }
        }

        public static float GetInchesFromPixels(float pixels)
        {
            return pixels / ScreenDPI;
        }

        public static float GetCentimetersFromPixels(float pixels)
        {
            return INCHES_TO_CENTIMETERS * pixels / ScreenDPI;
        }


        public static string GetSleepTimeoutDescription(int sleepTimeout)
        {
            if (sleepTimeout == UnityEngine.SleepTimeout.NeverSleep)
            {
                return "Never Sleep";
            }

            if (sleepTimeout == UnityEngine.SleepTimeout.SystemSetting)
            {
                return "System Setting";
            }

            return sleepTimeout.ToString();
        }

        public static string GetCutoutsString(Rect[] cutouts)
        {
            string[] cutoutStrings = new string[cutouts.Length];
            for (int i = 0; i < cutouts.Length; i++)
            {
                cutoutStrings[i] = cutouts[i].ToString();
            }

            return string.Join("; ", cutoutStrings);
        }

        public static string GetResolutionsString(Resolution[] resolutions)
        {
            string[] resolutionStrings = new string[resolutions.Length];
            for (int i = 0; i < resolutions.Length; i++)
            {
                resolutionStrings[i] = GetResolutionString(resolutions[i]);
            }

            return string.Join("; ", resolutionStrings);
        }
    }
}
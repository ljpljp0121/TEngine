using System;

namespace PFPackage
{
    public static class PFPackageUtils
    {
        public static string FormatPublishDate(string isoDate)
        {
            if (string.IsNullOrEmpty(isoDate) || isoDate == "Unknown")
                return "未知时间";

            if (DateTime.TryParse(isoDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dateTime))
            {
                DateTime localTime = dateTime.ToLocalTime();
                return localTime.ToString("yyyy年MM月dd日HH时mm分");
            }
            return isoDate;
        }

        public static bool IsUnityPackage(this string packageName)
        {
            return packageName.StartsWith("com.unity.");
        }
    }
}
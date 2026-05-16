using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Input/Location", 6)]
    public class Input_Location_Info : InfoBase
    {
        [GmCommand("system.location", "设置Unity原生GPS定位")]
        public static void OpenLocation(bool isOpen)
        {
            if (isOpen)
                Input.location.Start();
            else
                Input.location.Stop();
            Debug.Log($"location status {Input.location.status}");
        }

        [InfoItem("Is Enabled By User")] public string IsEnabledByUser => Input.location.isEnabledByUser.ToString();
        [InfoItem("Status")]
        public string Status => Input.location.isEnabledByUser ? Input.location.status.ToString() : "";
        [InfoItem("Horizontal Accuracy")]
        public string HorizontalAccuracy =>
            Input.location.isEnabledByUser ? Input.location.lastData.horizontalAccuracy.ToString() : "";
        [InfoItem("Vertical Accuracy")]
        public string VerticalAccuracy =>
            Input.location.isEnabledByUser ? Input.location.lastData.verticalAccuracy.ToString() : "";
        [InfoItem("Longitude")]
        public string Longitude => Input.location.isEnabledByUser ? Input.location.lastData.longitude.ToString() : "";
        [InfoItem("Latitude")]
        public string Latitude => Input.location.isEnabledByUser ? Input.location.lastData.latitude.ToString() : "";
        [InfoItem("Altitude")]
        public string Altitude => Input.location.isEnabledByUser ? Input.location.lastData.altitude.ToString() : "";
        [InfoItem("Timestamp")]
        public string Timestamp => Input.location.isEnabledByUser ? Input.location.lastData.timestamp.ToString() : "";
    }
}
using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Input/Compass", 9)]
    public class Input_Compass_Info : InfoBase
    {
        [GmCommand("system.compass", "开启 Unity 原生「电子罗盘 / 指南针」传感器")]
        public static void OpenGyroscope(bool isEnable)
        {
            Input.compass.enabled = isEnable;
            Debug.Log($"compass enable {Input.compass.enabled}");
        }
        
        [InfoItem("Enabled")] public string Enabled => Input.compass.enabled.ToString();
        [InfoItem("Heading Accuracy")] public string HeadingAccuracy => Input.compass.headingAccuracy.ToString();
        [InfoItem("Magnetic Heading")] public string MagneticHeading => Input.compass.magneticHeading.ToString();
        [InfoItem("Raw Vector")] public string RawVector => Input.compass.rawVector.ToString();
        [InfoItem("Timestamp")] public string Timestamp => Input.compass.timestamp.ToString();
        [InfoItem("True Heading")] public string TrueHeading => Input.compass.trueHeading.ToString();
    }
}

using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Input/Gyroscope", 8)]
    public class Input_Gyroscope_Info : InfoBase
    {
        [GmCommand("system.gyroscope", "设置陀螺仪开启和关闭")]
        public static void OpenGyroscope(bool isEnable)
        {
            Input.gyro.enabled = isEnable;
            Debug.Log($"gyro enable {Input.gyro.enabled}");
        }


        [InfoItem("Enabled")] public string Enabled => Input.gyro.enabled.ToString();
        [InfoItem("Update Interval")] public string UpdateInterval => Input.gyro.updateInterval.ToString();
        [InfoItem("Attitude")] public string Attitude => Input.gyro.attitude.eulerAngles.ToString();
        [InfoItem("Gravity")] public string Gravity => Input.gyro.gravity.ToString();
        [InfoItem("Rotation Rate")] public string RotationRate => Input.gyro.rotationRate.ToString();
        [InfoItem("Rotation Rate Unbiased")]
        public string RotationRateUnbiased => Input.gyro.rotationRateUnbiased.ToString();
        [InfoItem("User Acceleration")] public string UserAcceleration => Input.gyro.userAcceleration.ToString();
    }
}
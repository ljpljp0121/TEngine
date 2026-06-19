using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("System", 0)]
    public class SystemInformation : InfoBase
    {
        /// <summary> 系统信息 </summary>
        [InfoItem("Device Unique Identifier")]
        public string DeviceUniqueIdentifier => SystemInfo.deviceUniqueIdentifier;
        [InfoItem("Device Name")] public string DeviceName => SystemInfo.deviceName;
        [InfoItem("Device Type")] public string DeviceType => SystemInfo.deviceType.ToString();
        [InfoItem("Device Model")] public string DeviceModel => SystemInfo.deviceModel;
        [InfoItem("CPU Type")] public string CpuType => SystemInfo.processorType;
        [InfoItem("CPU Count")] public string CpuCount => SystemInfo.processorCount.ToString();
        [InfoItem("CPU Frequency")] public string CpuFrequency => $"{SystemInfo.processorFrequency} MHz";
        [InfoItem("System Memory Size")] public string SystemMemorySize => $"{SystemInfo.systemMemorySize} MB";
        [InfoItem("Operating System Family")]
        public string OperatingSystemFamily => SystemInfo.operatingSystemFamily.ToString();
        [InfoItem("Operating System")] public string OperatingSystem => SystemInfo.operatingSystem;
        [InfoItem("Battery Status")] public string BatteryStatus => SystemInfo.batteryStatus.ToString();
        [InfoItem("Battery Level")]
        public string BatteryLevel => InfoUtil.GetBatteryLevelString(SystemInfo.batteryLevel);
        [InfoItem("Supports Audio")] public string SupportsAudio => SystemInfo.supportsAudio.ToString();
        [InfoItem("Supports Location Service")]
        public string SupportsLocationService => SystemInfo.supportsLocationService.ToString();
        [InfoItem("Supports Accelerometer")]
        public string SupportsAccelerometer => SystemInfo.supportsAccelerometer.ToString();
        [InfoItem("Supports Gyroscope")] public string SupportsGyroscope => SystemInfo.supportsGyroscope.ToString();
        [InfoItem("Supports Vibration")] public string SupportsVibration => SystemInfo.supportsVibration.ToString();
        [InfoItem("Genuine")] public string Genuine => Application.genuine.ToString();
        [InfoItem("Genuine Check Available")]
        public string GenuineCheckAvailable => Application.genuineCheckAvailable.ToString();
    }
}
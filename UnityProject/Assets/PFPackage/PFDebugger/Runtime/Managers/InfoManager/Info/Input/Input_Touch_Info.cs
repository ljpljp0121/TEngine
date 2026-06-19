using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Input/Touch", 5)]
    public class Input_Touch_Info : InfoBase
    {
        [InfoItem("Touch Supported")] public string TouchSupported => Input.touchSupported.ToString();
        [InfoItem("Touch Pressure Supported")] public string TouchPressureSupported => Input.touchPressureSupported.ToString();
        [InfoItem("Stylus Touch Supported")] public string StylusTouchSupported => Input.stylusTouchSupported.ToString();
        [InfoItem("Simulate Mouse With Touches")] public string SimulateMouseWithTouches => Input.simulateMouseWithTouches.ToString();
        [InfoItem("Multi Touch Enabled")] public string MultiTouchEnabled => Input.multiTouchEnabled.ToString();
        [InfoItem("Touch Count")] public string TouchCount => Input.touchCount.ToString();
        [InfoItem("Touches")] public string Touches => GetTouchesString(Input.touches);

        private string GetTouchString(Touch touch)
        {
            return $"{touch.position}, {touch.deltaPosition}, {touch.rawPosition}, {touch.pressure}, {touch.phase}";
        }

        private string GetTouchesString(Touch[] touches)
        {
            var touchStrings = new string[touches.Length];
            for (int i = 0; i < touches.Length; i++)
                touchStrings[i] = GetTouchString(touches[i]);
            return string.Join("; ", touchStrings);
        }
    }
}

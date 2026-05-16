using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Input/Acceleration", 7)]
    public class Input_Acceleration_Info : InfoBase
    {
        [InfoItem("Acceleration")] public string Acceleration => Input.acceleration.ToString();
        [InfoItem("Acceleration Event Count")] public string AccelerationEventCount => Input.accelerationEventCount.ToString();
        [InfoItem("Acceleration Events")] public string AccelerationEvents => GetAccelerationEventsString(Input.accelerationEvents);

        private string GetAccelerationEventString(AccelerationEvent e)
        {
            return $"{e.acceleration}, {e.deltaTime}";
        }

        private string GetAccelerationEventsString(AccelerationEvent[] events)
        {
            var strings = new string[events.Length];
            for (int i = 0; i < events.Length; i++)
                strings[i] = GetAccelerationEventString(events[i]);
            return string.Join("; ", strings);
        }
    }
}

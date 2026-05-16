using System.Globalization;
using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Other/Time", 12)]
    public class Other_Time_Info : InfoBase
    {
        [InfoItem("Time Scale")] public string TimeScale => $"{Time.timeScale} [{GetTimeScaleDescription(Time.timeScale)}]";
        [InfoItem("Realtime Since Startup")] public string RealtimeSinceStartup => Time.realtimeSinceStartup.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Time Since Level Load")] public string TimeSinceLevelLoad => Time.timeSinceLevelLoad.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Time")] public string DebuggerTime => Time.time.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Fixed Time")] public string FixedTime => Time.fixedTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Unscaled Time")] public string UnscaledTime => Time.unscaledTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Fixed Unscaled Time")] public string FixedUnscaledTime => Time.fixedUnscaledTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Delta Time")] public string DeltaTime => Time.deltaTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Fixed Delta Time")] public string FixedDeltaTime => Time.fixedDeltaTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Unscaled Delta Time")] public string UnscaledDeltaTime => Time.unscaledDeltaTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Fixed Unscaled Delta Time")] public string FixedUnscaledDeltaTime => Time.fixedUnscaledDeltaTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Smooth Delta Time")] public string SmoothDeltaTime => Time.smoothDeltaTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Maximum Delta Time")] public string MaximumDeltaTime => Time.maximumDeltaTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Maximum Particle Delta Time")] public string MaximumParticleDeltaTime => Time.maximumParticleDeltaTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("Frame Count")] public string FrameCount => Time.frameCount.ToString();
        [InfoItem("Rendered Frame Count")] public string RenderedFrameCount => Time.renderedFrameCount.ToString();
        [InfoItem("Capture Framerate")] public string CaptureFramerate => Time.captureFramerate.ToString();
        [InfoItem("Capture Delta Time")] public string CaptureDeltaTime => Time.captureDeltaTime.ToString(CultureInfo.InvariantCulture);
        [InfoItem("In Fixed Time Step")] public string InFixedTimeStep => Time.inFixedTimeStep.ToString();

        private string GetTimeScaleDescription(float timeScale)
        {
            if (timeScale <= 0f) return "Pause";
            if (timeScale < 1f) return "Slower";
            if (timeScale > 1f) return "Faster";
            return "Normal";
        }
    }
}

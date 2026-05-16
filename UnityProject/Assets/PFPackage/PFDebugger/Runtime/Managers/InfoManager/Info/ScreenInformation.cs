using System;
using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Screen", 2)]
    public class ScreenInformation : InfoBase
    {
        /// <summary> фад╩пео╒ </summary>
        [InfoItem("Current Resolution")]
        public string CurrentResolution => InfoUtil.GetResolutionString(Screen.currentResolution);
        [InfoItem("Screen Width")]
        public string ScreenWidth => String.Format("{0} px / {1:F2} in / {2:F2} cm", Screen.width,
            InfoUtil.GetInchesFromPixels(Screen.width), InfoUtil.GetCentimetersFromPixels(Screen.width));
        [InfoItem("Screen Height")]
        public string ScreenHeight => String.Format("{0} px / {1:F2} in / {2:F2} cm", Screen.height,
            InfoUtil.GetInchesFromPixels(Screen.height),
            InfoUtil.GetCentimetersFromPixels(Screen.height));
        [InfoItem("Screen DPI")] public string ScreenDpi => Screen.dpi.ToString("F2");
        [InfoItem("Is Full Screen")] public string IsFullScreen => Screen.orientation.ToString();
        [InfoItem("Full Screen Mode")] public string FullScreenMode => Screen.fullScreen.ToString();
        [InfoItem("Sleep Timeout")] public string SleepTimeout => InfoUtil.GetSleepTimeoutDescription(Screen.sleepTimeout);
        [InfoItem("Brightness")] public string Brightness => Screen.brightness.ToString("F2");
        [InfoItem("Cursor Visible")] public string CursorVisible => Cursor.visible.ToString();
        [InfoItem("Cursor Lock State")] public string CursorLockState => Cursor.lockState.ToString();
        [InfoItem("Auto Landscape Left")] public string AutoLandscapeLeft => Screen.autorotateToLandscapeLeft.ToString();
        [InfoItem("Auto Landscape Right")] public string AutoLandscapeRight => Screen.autorotateToLandscapeRight.ToString();
        [InfoItem("Auto Portrait")] public string AutoPortrait => Screen.autorotateToPortrait.ToString();
        [InfoItem("Auto Portrait Upside Down")] public string AutoPortraitUpsideDown => Screen.autorotateToPortrait.ToString();
        [InfoItem("SafeArea")] public string SafeArea => Screen.safeArea.ToString();
        [InfoItem("Cutouts")] public string Cutouts => InfoUtil.GetCutoutsString(Screen.cutouts);
        [InfoItem("Support Resolutions")] public string SupportResolutions => InfoUtil.GetResolutionsString(Screen.resolutions);
    }
}
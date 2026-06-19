using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Input/Summary", 4)]
    public class Input_Summary_Info : InfoBase
    {
        [InfoItem("Back Button Leaves App")] public string BackButtonLeavesApp => Input.backButtonLeavesApp.ToString();
        [InfoItem("Device Orientation")] public string DeviceOrientation => Input.deviceOrientation.ToString();
        [InfoItem("Mouse Present")] public string MousePresent => Input.mousePresent.ToString();
        [InfoItem("Mouse Position")] public string MousePosition => Input.mousePosition.ToString();
        [InfoItem("Mouse Scroll Delta")] public string MouseScrollDelta => Input.mouseScrollDelta.ToString();
        [InfoItem("Any Key")] public string AnyKey => Input.anyKey.ToString();
        [InfoItem("Any Key Down")] public string AnyKeyDown => Input.anyKeyDown.ToString();
        [InfoItem("Input String")] public string InputString => Input.inputString;
        [InfoItem("IME Is Selected")] public string IMEIsSelected => Input.imeIsSelected.ToString();
        [InfoItem("IME Composition Mode")] public string IMECompositionMode => Input.imeCompositionMode.ToString();
        [InfoItem("Compensate Sensors")] public string CompensateSensors => Input.compensateSensors.ToString();
        [InfoItem("Composition Cursor Position")]
        public string CompositionCursorPosition => Input.compositionCursorPos.ToString();
        [InfoItem("Composition String")] public string CompositionString => Input.compositionString;
    }
}
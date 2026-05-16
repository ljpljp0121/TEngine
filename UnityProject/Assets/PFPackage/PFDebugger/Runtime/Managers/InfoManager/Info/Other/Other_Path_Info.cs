using System;
using UnityEngine;

namespace PFDebugger
{
    [InfoMenu("Other/Path", 11)]
    public class Other_Path_Info : InfoBase
    {
        [InfoItem("Current Directory")] public string CurrentDirectory => Environment.CurrentDirectory.Replace('\\', '/');
        [InfoItem("Data Path")] public string DataPath => Application.dataPath.Replace('\\', '/');
        [InfoItem("Persistent Data Path")] public string PersistentDataPath => Application.persistentDataPath.Replace('\\', '/');
        [InfoItem("Streaming Assets Path")] public string StreamingAssetsPath => Application.streamingAssetsPath.Replace('\\', '/');
        [InfoItem("Temporary Cache Path")] public string TemporaryCachePath => Application.temporaryCachePath.Replace('\\', '/');
        [InfoItem("Console Log Path")] public string ConsoleLogPath => Application.consoleLogPath.Replace('\\', '/');
    }
}

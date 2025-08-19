using UnityEngine;

namespace TEngine
{
    [CreateAssetMenu(menuName = "TEngine/SaveSetting", fileName = "SaveSetting")]
    public class SaveSetting : ScriptableObject
    {
        public SaveModule.SaveModuleType saveModuleType = SaveModule.SaveModuleType.Json;
    }
}
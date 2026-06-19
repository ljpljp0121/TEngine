using GameConfig;
using TEngine;
using UnityEngine;

public class LubanJsonLoader : IJsonTableLoader
{
    public string LoadJson(string fileName)
    {
        TextAsset asset = ModuleSystem.GetModule<IResourceModule>().LoadAsset<TextAsset>(fileName);
        return asset.text;
    }
}

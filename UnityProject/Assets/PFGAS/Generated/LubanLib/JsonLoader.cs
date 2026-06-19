using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace GameConfig
{
    public class JsonLoader : IJsonTableLoader
    {
        private const string JsonRoot = "Assets/PFGAS/Generated/Data/Json";

        public string LoadJson(string fileName)
        {
#if UNITY_EDITOR
            string assetPath = $"{JsonRoot}/{fileName}.json";
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (textAsset == null)
            {
                throw new System.IO.FileNotFoundException($"Json table asset not found: {assetPath}", assetPath);
            }

            return textAsset.text;
#else
            throw new System.NotSupportedException("JsonLoader currently supports editor AssetDatabase loading only.");
#endif
        }
    }

}

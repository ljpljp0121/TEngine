using UnityEditor;
using UnityEngine;

namespace PFPackage
{
    [CreateAssetMenu(fileName = "PFPackageConfig", menuName = "PFCoding/ScriptableObject/PFPackageConfig")]
    public class PFPackageConfig : ScriptableObject
    {
        private static PFPackageConfig instance;
        
        public static PFPackageConfig I
        {
            get
            {
                if (instance == null)
                {
                   string[] guids = AssetDatabase.FindAssets("t:PFPackageConfig");
                   if (guids.Length >= 1)
                   {
                       string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                       instance = AssetDatabase.LoadAssetAtPath<PFPackageConfig>(path);
                   }
                }
                return instance;
            }
        }

        public string InstallPath = "Assets/PFPackage";
        public string RegistryUrl = "";
        public string UserName = "";
        public string Password = "";
        public int MaxConcurrency = 10;
    }
}
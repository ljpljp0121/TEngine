using UnityEngine;
using UnityEngine.SceneManagement;

namespace PFDebugger
{
    [InfoMenu("Other/Scene", 10)]
    public class Other_Scene_Info : InfoBase
    {
        [InfoItem("Scene Count")] public string SceneCount => SceneManager.sceneCount.ToString();
        [InfoItem("Scene Count In Build Settings")]
        public string SceneCountInBuildSettings => SceneManager.sceneCountInBuildSettings.ToString();
        [InfoItem("Active Scene Handle")]
        public string ActiveSceneHandle => SceneManager.GetActiveScene().handle.ToString();
        [InfoItem("Active Scene Name")] public string ActiveSceneName => SceneManager.GetActiveScene().name;
        [InfoItem("Active Scene Path")] public string ActiveScenePath => SceneManager.GetActiveScene().path;
        [InfoItem("Active Scene Build Index")]
        public string ActiveSceneBuildIndex => SceneManager.GetActiveScene().buildIndex.ToString();
        [InfoItem("Active Scene Is Dirty")]
        public string ActiveSceneIsDirty => SceneManager.GetActiveScene().isDirty.ToString();
        [InfoItem("Active Scene Is Loaded")]
        public string ActiveSceneIsLoaded => SceneManager.GetActiveScene().isLoaded.ToString();
        [InfoItem("Active Scene Is Valid")]
        public string ActiveSceneIsValid => SceneManager.GetActiveScene().IsValid().ToString();
        [InfoItem("Active Scene Root Count")]
        public string ActiveSceneRootCount => SceneManager.GetActiveScene().rootCount.ToString();
        [InfoItem("Active Scene Is Sub Scene")]
        public string ActiveSceneIsSubScene => SceneManager.GetActiveScene().isSubScene.ToString();
    }
}
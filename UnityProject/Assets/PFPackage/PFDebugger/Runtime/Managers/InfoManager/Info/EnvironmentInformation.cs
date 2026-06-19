using UnityEngine;
using UnityEngine.Rendering;

namespace PFDebugger
{
    /// <summary> 环境信息 </summary>
    [InfoMenu("Environment", 1)]
    public class EnvironmentInformation : InfoBase
    {
        [InfoItem("Product Name")] public string ProductName => Application.productName;
        [InfoItem("Company Name")] public string CompanyName => Application.companyName;
        [InfoItem("Game Identifier")] public string GameIdentifier => Application.identifier;
        [InfoItem("Application Version")] public string ApplicationVersion => Application.version;
        [InfoItem("Unity Version")] public string UnityVersion => Application.unityVersion;
        [InfoItem("Platform")] public string Platform => Application.platform.ToString();
        [InfoItem("System Language")] public string SystemLanguage => Application.systemLanguage.ToString();
        [InfoItem("Cloud Project ID")] public string CloudProjectId => Application.cloudProjectId;
        [InfoItem("Build Guid")] public string BuildGuid => Application.buildGUID;
        [InfoItem("Target Frame Rate")] public string TargetFrameRate => Application.targetFrameRate.ToString();
        [InfoItem("Internet Reachability")]
        public string InternetReachability => Application.internetReachability.ToString();
        [InfoItem("Background Loading Priority")]
        public string BackgroundLoadingPriority => Application.backgroundLoadingPriority.ToString();
        [InfoItem("Is Playing")] public string IsPlaying => Application.isPlaying.ToString();
        [InfoItem("Splash Screen Is Finished")]
        public string SplashScreenIsFinished => SplashScreen.isFinished.ToString();
        [InfoItem("Run In Background")] public string RunInBackground => Application.runInBackground.ToString();
        [InfoItem("Install Name")] public string InstallName => Application.installerName;
        [InfoItem("Install Mode")] public string InstallMode => Application.installMode.ToString();
        [InfoItem("Sandbox Type")] public string SandboxType => Application.sandboxType.ToString();
        [InfoItem("Is Mobile Platform")] public string IsMobilePlatform => Application.isMobilePlatform.ToString();
        [InfoItem("Is Console Platform")] public string IsConsolePlatform => Application.isConsolePlatform.ToString();
        [InfoItem("Is Editor")] public string IsEditor => Application.isEditor.ToString();
        [InfoItem("Is Debug Build")] public string IsDebugBuild => Debug.isDebugBuild.ToString();
        [InfoItem("Is Focused")] public string IsFocused => Application.isFocused.ToString();
        [InfoItem("Is Batch Mode")] public string IsBatchMode => Application.isBatchMode.ToString();
    }
}
using UnityEditor;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 依赖状态帮助信息
    /// </summary>
    public static class DependencyStatusHelp
    {
        /// <summary>
        /// 显示依赖状态说明窗口
        /// </summary>
        [MenuItem("Window/PF Package Manager/Dependency Status Help")]
        public static void ShowHelpWindow()
        {
            EditorUtility.DisplayDialog(
                "依赖状态说明",
                "依赖包状态图标说明：\n\n" +
                "✅ 已安装且版本兼容\n" +
                "⚠️ 已安装但版本不匹配\n" +
                "📦 Unity官方包 - 未安装（可点击安装）\n" +
                "❌ 第三方包 - 未安装\n\n" +
                "Unity官方包会通过Unity Package Manager安装到Packages目录。\n" +
                "第三方包会安装到Assets/PFPackage目录（可修改源码）。",
                "了解"
            );
        }
    }
}
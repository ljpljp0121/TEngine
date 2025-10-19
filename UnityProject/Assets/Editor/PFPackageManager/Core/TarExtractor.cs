using UnityEngine;
using System.Diagnostics;

namespace PFPackageManager
{
    /// <summary>
    /// TAR文件解压工具
    /// </summary>
    public static class TarExtractor
    {
        /// <summary>
        /// 解压 tar 文件（使用系统命令）
        /// </summary>
        public static void ExtractTar(string tarPath, string outputPath)
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Windows 10+ 自带 tar 命令
                Process process = new Process();
                process.StartInfo.FileName = "tar";
                process.StartInfo.Arguments = $"-xf \"{tarPath}\" -C \"{outputPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new System.Exception($"tar 解压失败，退出码: {process.ExitCode}");
                }
            }
            else
            {
                // macOS/Linux
                Process process = new Process();
                process.StartInfo.FileName = "tar";
                process.StartInfo.Arguments = $"-xf \"{tarPath}\" -C \"{outputPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                process.WaitForExit();
            }
        }
    }
}
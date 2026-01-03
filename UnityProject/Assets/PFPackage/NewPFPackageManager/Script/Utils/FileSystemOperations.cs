using System.IO;
using UnityEditor;

namespace PFPackage
{
    /// <summary>
    /// 文件系统操作工具
    /// </summary>
    public static class FileSystemOperations
    {
        /// <summary>
        /// 从package.json读取版本号
        /// </summary>
        public static string ReadPackageVersion(string packagePath)
        {
            string packageJsonPath = Path.Combine(packagePath, "package.json");

            if (!File.Exists(packageJsonPath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(packageJsonPath);
                var packageJson = JSON.Parse(json);
                return packageJson["version"];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 删除Package整个目录及其meta文件
        /// </summary>
        public static void DeleteDirectoryWithMeta(string packagePath)
        {
            if (Directory.Exists(packagePath))
            {
                Directory.Delete(packagePath, true);

                // 删除 .meta 文件
                string metaFile = packagePath + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }
            }
        }

        public static void RefreshAssetDatabase()
        {
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 递归复制目录文件
        /// </summary>
        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            
            // 复制所有文件
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(targetDir, fileName);
                File.Copy(file, destFile, true);
            }
            
            // 递归复制子目录
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destDir = Path.Combine(targetDir, dirName);
                CopyDirectory(subDir, destDir);
            }
        }
    }
}
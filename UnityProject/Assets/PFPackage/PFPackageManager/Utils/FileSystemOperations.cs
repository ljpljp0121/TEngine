using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace PFPackageManager
{
    /// <summary>
    /// 文件系统操作工具
    /// </summary>
    public static class FileSystemOperations
    {
        /// <summary>
        /// 递归复制目录
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

        /// <summary>
        /// 删除目录及其meta文件
        /// </summary>
        public static void DeleteDirectoryWithMeta(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);

                // 删除 .meta 文件
                string metaFile = directoryPath + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }
            }
        }

        /// <summary>
        /// 刷新Unity资源数据库
        /// </summary>
        public static void RefreshAssetDatabase()
        {
            AssetDatabase.Refresh();
        }

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
        /// 从package.json读取显示名称
        /// </summary>
        public static string ReadPackageDisplayName(string packagePath)
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

                // 优先使用 displayName，如果没有则使用 name
                if (packageJson.HasKey("displayName") && !string.IsNullOrEmpty(packageJson["displayName"]))
                {
                    return packageJson["displayName"];
                }

                return packageJson["name"];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从package.json读取包名
        /// </summary>
        public static string ReadPackageName(string packagePath)
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
                return packageJson["name"];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 包名映射表：packageName -> directoryName
        /// </summary>
        private static readonly string PackageMappingFile = $"{PFPackageManagerWindow.INSTALL_PATH}/PFPackageMappings.json";
        private static Dictionary<string, string> packageMappings;

        /// <summary>
        /// 获取包的安装目录名（通过映射表或查找）
        /// </summary>
        public static string GetPackageDirectory(string packageName, string installPath)
        {
            LoadPackageMappings();

            // 如果映射表中有，直接返回
            if (packageMappings.ContainsKey(packageName))
            {
                return packageMappings[packageName];
            }

            // 否则遍历已安装的目录，查找package.json
            if (Directory.Exists(installPath))
            {
                var directories = Directory.GetDirectories(installPath);
                foreach (var dir in directories)
                {
                    string dirName = Path.GetFileName(dir);
                    string packageJsonPath = Path.Combine(dir, "package.json");

                    if (File.Exists(packageJsonPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(packageJsonPath);
                            var packageJson = JSON.Parse(json);
                            if (packageJson["name"] == packageName)
                            {
                                // 找到了，更新映射表
                                packageMappings[packageName] = dirName;
                                SavePackageMappings();
                                return dirName;
                            }
                        }
                        catch
                        {
                            // 忽略解析错误的文件
                        }
                    }
                }
            }

            // 没找到，返回包名作为默认值
            return packageName;
        }

        /// <summary>
        /// 注册包的目录映射
        /// </summary>
        public static void RegisterPackageMapping(string packageName, string directoryName)
        {
            LoadPackageMappings();
            packageMappings[packageName] = directoryName;
            SavePackageMappings();
        }

        /// <summary>
        /// 移除包的目录映射
        /// </summary>
        public static void UnregisterPackageMapping(string packageName)
        {
            LoadPackageMappings();
            if (packageMappings.ContainsKey(packageName))
            {
                packageMappings.Remove(packageName);
                SavePackageMappings();
            }
        }

        /// <summary>
        /// 加载包映射表
        /// </summary>
        private static void LoadPackageMappings()
        {
            if (packageMappings == null)
            {
                packageMappings = new Dictionary<string, string>();

                if (File.Exists(PackageMappingFile))
                {
                    try
                    {
                        string json = File.ReadAllText(PackageMappingFile);
                        var data = JSON.Parse(json);
                        packageMappings = new Dictionary<string, string>();

                        foreach (var key in data.Keys)
                        {
                            packageMappings[key] = data[key];
                        }
                    }
                    catch
                    {
                        packageMappings = new Dictionary<string, string>();
                    }
                }
            }
        }

        /// <summary>
        /// 保存包映射表
        /// </summary>
        private static void SavePackageMappings()
        {
            try
            {
                var data = new JSONObject();
                foreach (var kvp in packageMappings)
                {
                    data[kvp.Key] = kvp.Value;
                }
                File.WriteAllText(PackageMappingFile, data.ToString(2));
            }
            catch
            {
                // 忽略保存失败
            }
        }
    }
}
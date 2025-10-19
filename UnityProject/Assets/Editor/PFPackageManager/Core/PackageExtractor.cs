using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace PFPackageManager
{
    /// <summary>
    /// 专门负责解压包文件
    /// </summary>
    public class PackageExtractor
    {
        private readonly string tempPath;

        public PackageExtractor()
        {
            this.tempPath = Path.Combine(Application.temporaryCachePath, "PFPackages");
        }

        /// <summary>
        /// 解压 .tgz 文件（tar.gz 格式）
        /// </summary>
        public string ExtractPackage(string tgzPath, string packageName)
        {
            string extractPath = Path.Combine(tempPath, packageName);

            // 删除旧的解压目录
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
            Directory.CreateDirectory(extractPath);

            // .tgz 是 tar.gz 格式，需要先 gunzip 再 untar
            // Unity C# 可以用 GZipStream 解压 .gz
            using (FileStream originalFileStream = File.OpenRead(tgzPath))
            using (GZipStream gzipStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
            {
                string tarPath = tgzPath.Replace(".tgz", ".tar");
                using (FileStream decompressedFileStream = File.Create(tarPath))
                {
                    gzipStream.CopyTo(decompressedFileStream);
                }

                // 解压 tar 文件
                TarExtractor.ExtractTar(tarPath, extractPath);

                // 删除临时 tar 文件
                File.Delete(tarPath);
            }

            // NPM 包解压后会有一个 "package" 目录
            string packageDir = Path.Combine(extractPath, "package");
            return packageDir;
        }
    }
}
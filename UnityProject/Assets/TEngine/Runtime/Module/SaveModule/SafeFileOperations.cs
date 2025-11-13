using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace TEngine
{
    /// <summary>
    /// 内部安全文件操作工具 - 为IOTool提供安全的底层文件操作
    /// </summary>
    internal static class SafeFileOperations
    {
        private const int MAX_FILE_SIZE_MB = 100;
        private const int MAX_RETRIES = 3;
        private const int RETRY_DELAY_MS = 100;

        /// <summary>
        /// 安全地写入文本文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="content">文件内容</param>
        /// <returns>是否成功</returns>
        public static bool WriteAllTextSafe(string path, string content)
        {
            if (!ValidatePathSafety(path) || content == null)
            {
                Log.Error($"[SafeFileOperations] 无效的路径或内容: {path}");
                return false;
            }

            return ExecuteWithRetry(() =>
            {
                EnsureDirectoryExists(path);
                File.WriteAllText(path, content);
                return true;
            });
        }

        /// <summary>
        /// 安全地读取文本文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <param name="content">输出内容</param>
        /// <returns>是否成功</returns>
        public static bool ReadAllTextSafe(string path, out string content)
        {
            content = null;
            
            if (!ValidatePathSafety(path))
            {
                Log.Error($"[SafeFileOperations] 无效的文件路径: {path}");
                return false;
            }

            if (!File.Exists(path))
            {
                return false; // 文件不存在，不是错误
            }

            if (!ValidateFileSize(path))
            {
                Log.Error($"[SafeFileOperations] 文件过大: {path}");
                return false;
            }

            string tempContent = null;
            bool success = ExecuteWithRetry(() =>
            {
                tempContent = File.ReadAllText(path);
                return true;
            });

            if (success)
            {
                content = tempContent;
            }

            return success;
        }

        /// <summary>
        /// 安全地序列化对象到文件
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="path">文件路径</param>
        /// <returns>是否成功</returns>
        public static bool SerializeObjectSafe(object obj, string path)
        {
            if (!ValidatePathSafety(path) || obj == null)
            {
                Log.Error($"[SafeFileOperations] 无效的对象或路径: {path}");
                return false;
            }

            return ExecuteWithRetry(() =>
            {
                EnsureDirectoryExists(path);
                using (var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fileStream, obj);
                    fileStream.Flush();
                }
                return true;
            });
        }

        /// <summary>
        /// 安全地从文件反序列化对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <param name="result">输出对象</param>
        /// <returns>是否成功</returns>
        public static bool DeserializeObjectSafe<T>(string path, out T result) where T : class
        {
            result = null;

            if (!ValidatePathSafety(path))
            {
                Log.Error($"[SafeFileOperations] 无效的文件路径: {path}");
                return false;
            }

            if (!File.Exists(path))
            {
                return false; // 文件不存在
            }

            if (!ValidateFileSize(path))
            {
                Log.Error($"[SafeFileOperations] 文件过大: {path}");
                return false;
            }

            T tempResult = null;
            bool success = ExecuteWithRetry(() =>
            {
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var formatter = new BinaryFormatter();
                    var obj = formatter.Deserialize(fileStream);
                    tempResult = obj as T;
                    return tempResult != null;
                }
            });

            if (success)
            {
                result = tempResult;
            }

            return success;
        }

        /// <summary>
        /// 安全地删除文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否成功</returns>
        public static bool DeleteFileSafe(string path)
        {
            if (!ValidatePathSafety(path))
            {
                Log.Error($"[SafeFileOperations] 无效的文件路径: {path}");
                return false;
            }

            return ExecuteWithRetry(() =>
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return true;
            });
        }

        private static bool ExecuteWithRetry(Func<bool> operation)
        {
            Exception lastException = null;

            for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
            {
                try
                {
                    return operation();
                }
                catch (UnauthorizedAccessException ex)
                {
                    lastException = ex;
                    Log.Warning($"[SafeFileOperations] 文件访问权限不足，重试 {attempt + 1}/{MAX_RETRIES}");
                }
                catch (DirectoryNotFoundException ex)
                {
                    lastException = ex;
                    Log.Warning($"[SafeFileOperations] 目录不存在，重试 {attempt + 1}/{MAX_RETRIES}");
                }
                catch (IOException ex)
                {
                    lastException = ex;
                    Log.Warning($"[SafeFileOperations] IO错误，重试 {attempt + 1}/{MAX_RETRIES}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Log.Error($"[SafeFileOperations] 文件操作失败: {ex.GetType().Name} - {ex.Message}");
                    break; // 非IO异常，不重试
                }

                if (attempt < MAX_RETRIES - 1)
                {
                    System.Threading.Thread.Sleep(RETRY_DELAY_MS * (attempt + 1));
                }
            }

            Log.Error($"[SafeFileOperations] 操作最终失败: {lastException?.Message}");
            return false;
        }

        private static bool ValidatePathSafety(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // 防止路径遍历攻击
            if (path.Contains(".."))
                return false;

            try
            {
                // 验证路径格式有效性
                Path.GetFullPath(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ValidateFileSize(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                long maxSizeInBytes = MAX_FILE_SIZE_MB * 1024 * 1024;
                return fileInfo.Length <= maxSizeInBytes;
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
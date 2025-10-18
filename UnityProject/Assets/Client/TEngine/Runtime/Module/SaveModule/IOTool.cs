using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace TEngine
{
    /// <summary>
    /// 本地存储工具
    /// </summary>
    public static class IOTool
    {

        /// <summary>
        /// 保存Json
        /// </summary>
        /// <param name="saveObj">保存的类</param>
        /// <param name="path">路径</param>
        public static void SaveJson(object saveObj, string path)
        {
            try
            {
                if (saveObj == null)
                {
                    Log.Error("[IOTool] SaveJson: saveObj为null");
                    return;
                }

                string jsonData = JsonUtility.ToJson(saveObj);
                if (string.IsNullOrEmpty(jsonData))
                {
                    Log.Error("[IOTool] SaveJson: Json序列化失败");
                    return;
                }

#if UNITY_EDITOR
                string encryptedData = jsonData;
#else
                string encryptedData = EncryptionUtility.Encrypt(jsonData);
                if (string.IsNullOrEmpty(encryptedData))
                {
                    Log.Warning("[IOTool] SaveJson: 加密失败，使用明文保存");
                    encryptedData = jsonData;
                }
#endif

                if (!SafeFileOperations.WriteAllTextSafe(path, encryptedData))
                {
                    Log.Error($"[IOTool] SaveJson: 文件写入失败 - {path}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[IOTool] SaveJson异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取Json为指定的类型对象
        /// </summary>
        public static T LoadJson<T>(string path) where T : class
        {
            try
            {
                if (!SafeFileOperations.ReadAllTextSafe(path, out string encryptedData))
                {
                    return null; // 文件不存在或读取失败
                }

                if (string.IsNullOrEmpty(encryptedData))
                {
                    Log.Warning("[IOTool] LoadJson: 文件内容为空");
                    return null;
                }

#if UNITY_EDITOR
                string jsonData = encryptedData;
#else
                string jsonData = EncryptionUtility.Decrypt(encryptedData);
                if (string.IsNullOrEmpty(jsonData))
                {
                    Log.Warning("[IOTool] LoadJson: 解密失败，尝试直接解析");
                    jsonData = encryptedData;
                }
#endif

                T result = JsonUtility.FromJson<T>(jsonData);
                if (result == null)
                {
                    Log.Error($"[IOTool] LoadJson: Json反序列化失败，目标类型: {typeof(T).Name}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"[IOTool] LoadJson异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存文件
        /// </summary>
        /// <param name="saveObject">保存对象</param>
        /// <param name="path">保存路径</param>
        public static void SaveFile(object saveObject, string path)
        {
            try
            {
                if (saveObject == null)
                {
                    Log.Error("[IOTool] SaveFile: saveObject为null");
                    return;
                }

                if (!SafeFileOperations.SerializeObjectSafe(saveObject, path))
                {
                    Log.Error($"[IOTool] SaveFile: 二进制序列化失败 - {path}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[IOTool] SaveFile异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载文件
        /// </summary>
        /// <typeparam name="T">加载类型</typeparam>
        /// <param name="path">加载路径</param>
        public static T LoadFile<T>(string path) where T : class
        {
            try
            {
                if (!SafeFileOperations.DeserializeObjectSafe<T>(path, out T result))
                {
                    return null; // 文件不存在或反序列化失败
                }

                if (result == null)
                {
                    Log.Error($"[IOTool] LoadFile: 反序列化结果为null，目标类型: {typeof(T).Name}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"[IOTool] LoadFile异常: {ex.Message}");
                return null;
            }
        }
    }
}
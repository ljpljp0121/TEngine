using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace PFPackage.FeiShuExcel
{
    [CreateAssetMenu(fileName = "FeiShuExcelSetting", menuName = "PFCoding/ScriptableObject/FeiShuExcelSetting")]
    public class FeiShuExcelSetting : ScriptableObject
    {
        private static FeiShuExcelSetting instance;
        public static FeiShuExcelSetting I
        {
            get
            {
                if (instance == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:FeiShuExcelSetting");
                    if (guids.Length >= 1)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        instance = AssetDatabase.LoadAssetAtPath<FeiShuExcelSetting>(path);
                    }
                }
                return instance;
            }
        }

        public string APPID;
        public string APPSecret;
        public string FeiShuFolderRootToken;
        [FormerlySerializedAs("LocalExcelFolderPath")]
        public string LocalRootPath;

        public SerializableDictionary<string, FeiShuExcelInfo> ExcelInfoDic = new SerializableDictionary<string, FeiShuExcelInfo>();

        [InfoBox("主要原因是因为飞书创建电子表格API限制一分钟只能20次,所以可以先用这个按钮每个一分钟创建一次，把表格先创建完再说")]
        [Button("同步文件结构")]
        public async Task CreateFolder()
        {
            await FeiShuUtils.SyncDirectoryStructure(LocalRootPath, FeiShuFolderRootToken);
        }

        [Button("写入飞书表格")]
        public async Task WriteExcel()
        {
            FeiShuExcelInfo info = await FeiShuUtils.GetSheetInfo("F5ucsODCjhi00EtD5i1cKlRxn6d");
            Debug.Log($"[飞书读表] 获取飞书表格信息成功: {info.ExcelToken} {string.Join(",", info.SheetInfos.Select(x => x.SheetTitle))}");
        }
    }
}
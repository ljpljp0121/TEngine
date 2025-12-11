using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using UnityEngine;

namespace PFPackage.FeiShuExcel
{
    public partial class FeiShuUtils
    {
        /// <summary>
        /// 读取本地Excel文件内容
        /// </summary>
        /// <param name="localFilePath"></param>
        /// <returns>List，每个元素是一个工作表的二维数组</returns>
        private static LocalExcelInfo ReadExcelFile(string localFilePath)
        {
            var allSheetInfo = new LocalExcelInfo();
            try
            {
                var fileInfo = new FileInfo(localFilePath);
                if (!fileInfo.Exists)
                {
                    return null;
                }

                using var package = new ExcelPackage(fileInfo);
                var worksheets = package.Workbook.Worksheets;

                // 遍历所有工作表
                foreach (var worksheet in worksheets)
                {
                    // 读取所有有数据的单元格
                    var dimension = worksheet.Dimension;
                    if (dimension == null)
                    {
                        Debug.LogWarning($"[飞书读表] 工作表为空: {worksheet.Name}");
                        continue;
                    }

                    int rows = dimension.End.Row - dimension.Start.Row + 1;
                    int cols = dimension.End.Column - dimension.Start.Column + 1;
                    var sheetData = new string[rows, cols];

                    for (int row = dimension.Start.Row; row <= dimension.End.Row; row++)
                    {
                        for (int col = dimension.Start.Column; col <= dimension.End.Column; col++)
                        {
                            sheetData[row - dimension.Start.Row, col - dimension.Start.Column] = worksheet.Cells[row, col].Text;
                        }
                    }
                    // 获取合并单元格信息
                    var mergeCells = GetMergeCells(worksheet);

                    allSheetInfo.SheetInfos.Add(new LocalSheetInfo()
                    {
                        SheetTitle = worksheet.Name,
                        Index = worksheet.Index,
                        SheetData = sheetData,
                        MergeCells = mergeCells
                    });
                }

                return allSheetInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 读取Excel文件失败: {localFilePath}\n错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 判断是否为 Excel 文件
        /// </summary>
        private static bool IsExcelFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLower();
            return extension == ".xlsx" || extension == ".xls";
        }

        /// <summary>
        /// 获取工作表的合并单元格信息
        /// </summary>
        private static List<MergeCellInfo> GetMergeCells(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            var mergeCells = new List<MergeCellInfo>();

            try
            {
                if (worksheet.MergedCells != null)
                {
                    foreach (var merge in worksheet.MergedCells)
                    {
                        mergeCells.Add(new MergeCellInfo { Range = merge });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 获取合并单元格信息失败: {ex.Message}");
            }

            return mergeCells;
        }

        /// <summary> 将LocalExcelInfo写入本地Excel文件 </summary>
        public static void WriteExcelFile(string localFilePath, LocalExcelInfo localInfo)
        {
            try
            {
                var fileInfo = new FileInfo(localFilePath);

                using var package = new ExcelPackage();

                // 创建所有工作表
                foreach (var sheetInfo in localInfo.SheetInfos)
                {
                    var worksheet = package.Workbook.Worksheets.Add(sheetInfo.SheetTitle);

                    if (sheetInfo.SheetData != null)
                    {
                        int rows = sheetInfo.SheetData.GetLength(0);
                        int cols = sheetInfo.SheetData.GetLength(1);

                        // 写入数据
                        for (int row = 0; row < rows; row++)
                        {
                            for (int col = 0; col < cols; col++)
                            {
                                worksheet.Cells[row + 1, col + 1].Value = sheetInfo.SheetData[row, col];
                            }
                        }
                    }

                    // 处理合并单元格
                    if (sheetInfo.MergeCells != null)
                    {
                        foreach (var mergeCell in sheetInfo.MergeCells)
                        {
                            if (!string.IsNullOrEmpty(mergeCell.Range))
                            {
                                worksheet.Cells[mergeCell.Range].Merge = true;
                            }
                        }
                    }
                }
                
                package.SaveAs(fileInfo);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书同步] 写入本地Excel文件失败: {localFilePath}, 错误: {ex.Message}");
            }
        }
        
        /// <summary> 写入本地Excel文件 </summary>
        public static async Task WriteLocalExcel(string excelToken, string localFilePath)
        {
            try
            {
                FeiShuExcelInfo onlineInfo = await GetOnlineSheetInfo(excelToken);
                LocalExcelInfo localInfo = ReadExcelFile(localFilePath);
                if (localInfo == null) return;

                int localCount = localInfo.SheetInfos.Count;
                int onlineCount = onlineInfo.SheetInfos.Count;

                Debug.Log($"[飞书同步] {excelToken} 本地工作表: {localCount}个, 云端工作表:{onlineCount}个");

                if (onlineCount > localCount)
                {
                    int toCreate = onlineCount - localCount;

                    for (int i = 0; i < toCreate; i++)
                    {
                        int index = localCount + i;
                        var onlineSheet = onlineInfo.SheetInfos[index];
                        localInfo.SheetInfos.Add(new LocalSheetInfo()
                        {
                            SheetTitle = onlineSheet.SheetTitle,
                            Index = onlineSheet.Index,
                            SheetData = new string[1, 1] { { "" } },
                            MergeCells = new List<MergeCellInfo>()
                        });
                    }

                    Debug.Log($"[飞书同步]  {excelToken} 创建 {toCreate} 个本地工作表");
                }
                else if (onlineCount < localCount)
                {
                    int toDelete = localCount - onlineCount;
                    for (int i = 0; i < toDelete; i++)
                    {
                        localInfo.SheetInfos.RemoveAt(localCount - 1 - i);
                    }

                    Debug.Log($"[飞书同步]  {excelToken} 删除 {toDelete} 个本地工作表");
                }

                var syncTasks = new List<Task>();
                
                for (int i = 0; i < onlineInfo.SheetInfos.Count; i++)
                {
                    int index = i;
                    var onlineSheet = onlineInfo.SheetInfos[index];
                    localInfo.SheetInfos[index].SheetTitle = onlineSheet.SheetTitle;
                    localInfo.SheetInfos[index].Index = onlineSheet.Index;
                    var task = GetSheetData(excelToken, onlineSheet.SheetId)
                        .ContinueWith(t => {
                            localInfo.SheetInfos[index].SheetData = t.Result;
                        });
                    syncTasks.Add(task);
                }
                await Task.WhenAll(syncTasks);
                // 写入本地文件
                WriteExcelFile(localFilePath, localInfo);
                Debug.Log($"[飞书同步] {excelToken} 本地表格同步完成");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[飞书读表] 写入本地表格时发生错误: {ex.Message}");
            }
        }
    }
}
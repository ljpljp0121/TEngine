using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    allSheetInfo.SheetInfos.Add(new LocalSheetInfo()
                    {
                        SheetTitle = worksheet.Name,
                        Index = worksheet.Index,
                        SheetData = sheetData,
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
    }
}
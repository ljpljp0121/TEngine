using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace PFGAS.Editor
{
    public sealed class PFTagExcelService
    {
        private const int HeaderRow = 1;
        private const int DataStartRow = 5;

        public PFTagExcelDocument Read(string excelPath = null)
        {
            excelPath = string.IsNullOrWhiteSpace(excelPath) ? PFTagExcelPaths.TagExcelPath : excelPath;
            if (!File.Exists(excelPath))
            {
                throw new FileNotFoundException("PFTag Excel not found.", excelPath);
            }

            using var package = new ExcelPackage(new FileInfo(excelPath));
            var worksheet = GetFirstWorksheet(package, excelPath);
            var document = CreateDocument(excelPath, worksheet);

            var byId = document.Rows
                .GroupBy(r => r.Id)
                .Where(g => g.Count() == 1)
                .ToDictionary(g => g.Key, g => g.First());
            foreach (var row in document.Rows)
            {
                row.FullPath = PFTagNameUtility.BuildFullPath(row, byId);
            }

            return document;
        }

        public void Save(PFTagExcelDocument document, IReadOnlyList<PFTagExcelRow> rows)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            Save(document.ExcelPath, rows);
        }

        public void Save(string excelPath, IReadOnlyList<PFTagExcelRow> rows)
        {
            if (rows == null)
            {
                throw new ArgumentNullException(nameof(rows));
            }

            EnsureWritable(excelPath);
            File.Copy(excelPath, excelPath + ".bak", true);

            using var package = new ExcelPackage(new FileInfo(excelPath));
            var worksheet = GetFirstWorksheet(package, excelPath);
            var document = CreateDocument(excelPath, worksheet);
            WriteRows(worksheet, document, rows);
            package.Save();
        }

        private static void EnsureWritable(string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException ex)
            {
                throw new PFTagExcelFileLockedException(path, ex);
            }
        }

        private static ExcelWorksheet GetFirstWorksheet(ExcelPackage package, string excelPath)
        {
            var worksheet = package.Workbook.Worksheets.Count > 0 ? package.Workbook.Worksheets[1] : null;
            if (worksheet == null)
            {
                throw new InvalidDataException($"PFTag.xlsx has no readable worksheet: {excelPath}");
            }

            return worksheet;
        }

        private static PFTagExcelDocument CreateDocument(string excelPath, ExcelWorksheet worksheet)
        {
            var header = ReadRow(worksheet, HeaderRow);
            if (header.Count == 0)
            {
                throw new InvalidDataException("PFTag.xlsx is missing Luban ##var header row.");
            }

            var document = new PFTagExcelDocument
            {
                ExcelPath = excelPath,
                IdColumn = FindColumn(header, "Id"),
                ParentIdColumn = FindColumn(header, "ParentId"),
                NameColumn = FindColumn(header, "Name"),
                DescColumn = FindColumn(header, "Desc"),
                MaxColumn = worksheet.Dimension?.End.Column ?? header.Keys.DefaultIfEmpty(0).Max(),
            };

            if (document.IdColumn <= 0 ||
                document.ParentIdColumn <= 0 ||
                document.NameColumn <= 0 ||
                document.DescColumn <= 0)
            {
                throw new InvalidDataException("PFTag.xlsx must contain Id, ParentId, Name and Desc columns.");
            }

            document.FullPathColumn = FindColumn(header, "FullPath");
            if (document.FullPathColumn <= 0)
            {
                document.FullPathColumn = Math.Max(document.DescColumn + 1, document.MaxColumn);
            }

            document.MaxColumn = Math.Max(document.MaxColumn, document.FullPathColumn);

            var maxRow = worksheet.Dimension?.End.Row ?? 0;
            for (var rowIndex = DataStartRow; rowIndex <= maxRow; rowIndex++)
            {
                var row = ReadRow(worksheet, rowIndex);
                var idText = GetCellValue(row, document.IdColumn);
                var name = GetCellValue(row, document.NameColumn);
                if (string.IsNullOrWhiteSpace(idText) && string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                document.Rows.Add(new PFTagExcelRow
                {
                    Id = ParseInt(idText, rowIndex, "Id"),
                    ParentId = ParseInt(GetCellValue(row, document.ParentIdColumn), rowIndex, "ParentId"),
                    Name = name.Trim(),
                    Desc = GetCellValue(row, document.DescColumn),
                    FullPath = GetCellValue(row, document.FullPathColumn),
                });
            }

            return document;
        }

        private static SortedDictionary<int, string> ReadRow(ExcelWorksheet worksheet, int row)
        {
            var result = new SortedDictionary<int, string>();
            var maxColumn = worksheet.Dimension?.End.Column ?? 0;
            for (var column = 1; column <= maxColumn; column++)
            {
                var value = GetCellText(worksheet.Cells[row, column].Value);
                if (!string.IsNullOrEmpty(value))
                {
                    result[column] = value;
                }
            }

            return result;
        }

        private static int FindColumn(SortedDictionary<int, string> row, string value)
        {
            foreach (var kv in row)
            {
                var header = kv.Value.Split('#')[0];
                if (string.Equals(header, value, StringComparison.OrdinalIgnoreCase))
                {
                    return kv.Key;
                }
            }

            return -1;
        }

        private static string GetCellValue(SortedDictionary<int, string> row, int column)
        {
            return row.TryGetValue(column, out var value) ? value ?? string.Empty : string.Empty;
        }

        private static int ParseInt(string value, int row, string field)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }

            throw new InvalidDataException($"PFTag.xlsx row {row} has invalid {field}: {value}");
        }

        private static void WriteRows(
            ExcelWorksheet worksheet,
            PFTagExcelDocument document,
            IReadOnlyList<PFTagExcelRow> rows)
        {
            var styleByColumn = ReadDataStyles(worksheet, document.MaxColumn);
            ClearDataRows(worksheet, document.MaxColumn);

            var byId = rows.ToDictionary(r => r.Id, r => r);
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i].Clone();
                row.FullPath = PFTagNameUtility.BuildFullPath(row, byId);
                WriteDataRow(worksheet, DataStartRow + i, document, row, styleByColumn);
            }
        }

        private static Dictionary<int, int> ReadDataStyles(ExcelWorksheet worksheet, int maxColumn)
        {
            var result = new Dictionary<int, int>();
            var maxRow = worksheet.Dimension?.End.Row ?? 0;
            if (maxRow < DataStartRow)
            {
                return result;
            }

            for (var column = 1; column <= maxColumn; column++)
            {
                result[column] = worksheet.Cells[DataStartRow, column].StyleID;
            }

            return result;
        }

        private static void ClearDataRows(ExcelWorksheet worksheet, int maxColumn)
        {
            var maxRow = worksheet.Dimension?.End.Row ?? 0;
            if (maxRow < DataStartRow || maxColumn <= 0)
            {
                return;
            }

            for (var row = DataStartRow; row <= maxRow; row++)
            {
                for (var column = 1; column <= maxColumn; column++)
                {
                    worksheet.Cells[row, column].Value = null;
                }
            }
        }

        private static void WriteDataRow(
            ExcelWorksheet worksheet,
            int rowNumber,
            PFTagExcelDocument document,
            PFTagExcelRow row,
            IReadOnlyDictionary<int, int> styles)
        {
            SetCellValue(worksheet, rowNumber, document.IdColumn, row.Id, styles);
            SetCellValue(worksheet, rowNumber, document.ParentIdColumn, row.ParentId, styles);
            SetCellValue(worksheet, rowNumber, document.NameColumn, row.Name, styles);
            SetCellValue(worksheet, rowNumber, document.DescColumn, row.Desc, styles);
            SetCellValue(worksheet, rowNumber, document.FullPathColumn, row.FullPath, styles);
        }

        private static void SetCellValue(
            ExcelWorksheet worksheet,
            int rowNumber,
            int column,
            object value,
            IReadOnlyDictionary<int, int> styles)
        {
            var cell = worksheet.Cells[rowNumber, column];
            if (styles.TryGetValue(column, out var styleId))
            {
                cell.StyleID = styleId;
            }

            cell.Value = value;
        }

        private static string GetCellText(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is double doubleValue && Math.Abs(doubleValue % 1) < double.Epsilon)
            {
                return doubleValue.ToString("0", CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}

using System;
using System.Collections.Generic;

namespace PFPackage.FeiShuExcel
{
    [Serializable]
    public class FeiShuExcelInfo
    {
        public string ExcelToken;
        public List<FeiShuSheetInfo> SheetInfos = new List<FeiShuSheetInfo>();
    }

    [Serializable]
    public class FeiShuSheetInfo
    {
        public string SheetTitle;
        public string SheetId;
        public int Index;
        public List<MergeCellInfo> MergeCells = new List<MergeCellInfo>();
    }
}
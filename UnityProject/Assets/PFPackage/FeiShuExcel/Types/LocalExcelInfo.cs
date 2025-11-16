using System;
using System.Collections.Generic;

namespace PFPackage.FeiShuExcel
{
    public class LocalExcelInfo
    {
        public List<LocalSheetInfo> SheetInfos = new List<LocalSheetInfo>();
    }

    [Serializable]
    public class LocalSheetInfo
    {
        public string SheetTitle;
        public int Index;
        public string[,] SheetData;
    }
}
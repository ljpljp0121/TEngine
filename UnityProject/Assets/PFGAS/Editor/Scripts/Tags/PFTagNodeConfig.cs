using System;

namespace PFGAS.Editor
{
    [Serializable]
    public class PFTagNodeConfig
    {
        public int Id;
        public string Name;
        public int Depth;
        public int ParentId;
        public string Desc;
        public string FullPath;
    }
}

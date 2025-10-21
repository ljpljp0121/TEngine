using System.Collections.Generic;

namespace PFPackageManager
{
    /// <summary>
    /// 依赖分析结果
    /// </summary>
    public class DependencyAnalysis
    {
        public List<string> MissingUnityPackages { get; set; }
        public List<string> MissingThirdPartyPackages { get; set; }
        public List<string> IncompatibleDependencies { get; set; }
        public bool HasMissingOrIncompatibleDependencies { get; set; }

        public DependencyAnalysis()
        {
            MissingUnityPackages = new List<string>();
            MissingThirdPartyPackages = new List<string>();
            IncompatibleDependencies = new List<string>();
            HasMissingOrIncompatibleDependencies = false;
        }
    }
}
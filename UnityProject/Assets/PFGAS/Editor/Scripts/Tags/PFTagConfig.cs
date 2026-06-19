using System;
using UnityEngine;
using PFTreeView;

namespace PFGAS.Editor
{
    /// <summary>
    /// 编辑器 Tag 树中的单个节点配置。
    /// </summary>
    [Serializable]
    public class PFTagNodeConfig : PFTreeNodeConfig
    {
        public int TagId;
    }

    /// <summary>
    /// 保存 PFTag 编辑器树结构的配置资产。
    /// </summary>
    [CreateAssetMenu(fileName = "PFTagConfig", menuName = "PFGAS/PFTagConfig")]
    public class PFTagConfig : PFTreeConfig<PFTagNodeConfig>
    {
    }
}

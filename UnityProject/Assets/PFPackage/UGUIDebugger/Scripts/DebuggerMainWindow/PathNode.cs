/* 
****************************************************
* 文件：PathNode.cs
* 作者：PeiFeng
* 创建时间：2025/10/25 19:04:08 星期六
* 功能：按钮树节点
****************************************************
*/

using System;
using System.Collections.Generic;

namespace PFDebugger
{
    public class PathNode
    {
        public string Name;
        public string Path;
        public int Order;
        public PathNodeType NodeType;
        public IWindowBase Window;
        public Action Method;
        public List<PathNode> Children = new List<PathNode>();
        public PathNode Parent;

        public bool IsLeaf => Children == null||  Children.Count == 0 ;
    }
}
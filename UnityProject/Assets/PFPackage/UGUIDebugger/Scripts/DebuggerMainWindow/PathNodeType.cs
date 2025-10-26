/* 
****************************************************
* 文件：PathNodeType.cs
* 作者：PeiFeng
* 创建时间：2025/10/25 19:06:31 星期六
* 功能：节点树节点类型
****************************************************
*/

namespace PFDebugger
{
    public enum PathNodeType
    {
        /// <summary>
        /// 窗口节点类型。
        /// </summary>
        Window = 0,
        /// <summary>
        /// 方法节点类型。
        /// </summary>
        Method = 1,
        /// <summary>
        /// 菜单节点类型。
        /// </summary>
        Menu = 2,
    }
}
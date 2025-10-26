/* 
****************************************************
* 文件：DebuggerActiveWindowType.cs
* 作者：PeiFeng
* 创建时间：2025/10/25 18:56:37 星期六
* 功能：日志系统激活类型
****************************************************
*/

namespace PFDebugger
{
    public enum DebuggerActiveWindowType : byte
    {
        /// <summary>
        /// 总是打开。
        /// </summary>
        AlwaysOpen = 0,

        /// <summary>
        /// 仅在开发模式时打开。
        /// </summary>
        OnlyOpenWhenDevelopment,

        /// <summary>
        /// 仅在编辑器中打开。
        /// </summary>
        OnlyOpenInEditor,

        /// <summary>
        /// 总是关闭。
        /// </summary>
        AlwaysClose,
    }
}
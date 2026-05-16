namespace PFDebugger
{
    /// <summary>日志条目的时间信息</summary>
    public struct DebugLogEntryTimestamp
    {
        /// <summary>本地时间</summary>
        public readonly System.DateTime dateTime;
        /// <summary>游戏运行秒数</summary>
        public readonly float elapsedSeconds;
        /// <summary>帧号</summary>
        public readonly int frameCount;

        public DebugLogEntryTimestamp(System.DateTime dateTime, float elapsedSeconds, int frameCount)
        {
            this.dateTime = dateTime;
            this.elapsedSeconds = elapsedSeconds;
            this.frameCount = frameCount;
        }
    }
}

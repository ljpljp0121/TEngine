using System;

namespace TEngine
{
    /// <summary>
    /// 刷新回调委托
    /// </summary>
    /// <param name="refreshTime">刷新时间戳</param>
    /// <param name="args">附加参数</param>
    public delegate void RefreshCallback(long refreshTime, params object[] args);

    /// <summary>
    /// 倒计时回调委托
    /// </summary>
    /// <param name="remainingMilliseconds">剩余毫秒数</param>
    /// <param name="args">附加参数</param>
    public delegate void CountdownCallback(long remainingMilliseconds, params object[] args);
    
    internal partial class TimerModule
    {
        /// <summary>
        /// 刷新任务
        /// </summary>
        private class RefreshTask
        {
            /// <summary>
            /// 任务唯一标识
            /// </summary>
            public string Id { get; private set; }

            /// <summary>
            /// 刷新配置
            /// </summary>
            public RefreshConfig Config { get; private set; }

            /// <summary>
            /// 刷新回调
            /// </summary>
            public RefreshCallback OnRefresh { get; set; }

            /// <summary>
            /// 倒计时回调
            /// </summary>
            public CountdownCallback OnCountdown { get; set; }

            /// <summary>
            /// 附加参数
            /// </summary>
            public object[] Args { get; set; }

            /// <summary>
            /// 下次刷新时间
            /// </summary>
            public DateTime NextRefreshTime { get; private set; }

            public RefreshTask(string id, RefreshConfig config, RefreshCallback onRefresh = null,
                CountdownCallback onCountdown = null, params object[] args)
            {
                Id = id;
                Config = config;
                OnRefresh = onRefresh;
                OnCountdown = onCountdown;
                Args = args;
            }

            /// <summary>
            /// 更新下次刷新时间
            /// </summary>
            /// <param name="currentTime">当前时间</param>
            public void UpdateNextRefreshTime(DateTime currentTime)
            {
                NextRefreshTime = Config.CalculateNextRefreshTime(currentTime);
            }

            /// <summary>
            /// 检查是否需要刷新
            /// </summary>
            /// <param name="currentTime">当前时间</param>
            /// <returns>是否需要刷新</returns>
            public bool ShouldRefresh(DateTime currentTime)
            {
                return Config.ShouldRefresh(currentTime);
            }

            /// <summary>
            /// 执行刷新
            /// </summary>
            /// <param name="currentTime">当前时间</param>
            public void ExecuteRefresh(DateTime currentTime)
            {
                Config.LastRefreshTime = currentTime;
                UpdateNextRefreshTime(currentTime);
                // long refreshTimeStamp = TimeUtil.DateTimeToUnixTime(currentTime);
                // OnRefresh?.Invoke(refreshTimeStamp, Args);
                ExecuteCountdown(currentTime);
            }

            /// <summary>
            /// 执行倒计时回调
            /// </summary>
            /// <param name="currentTime">当前时间</param>
            public void ExecuteCountdown(DateTime currentTime)
            {
                if (OnCountdown == null) return;
                var timeUntilNext = NextRefreshTime - currentTime;
                long remainingMilliseconds = (long)timeUntilNext.TotalMilliseconds;
                if (remainingMilliseconds < 0) remainingMilliseconds = 0;
                OnCountdown.Invoke(remainingMilliseconds, Args);
            }

            /// <summary>
            /// 获取剩余时间
            /// </summary>
            /// <param name="currentTime">当前时间</param>
            /// <returns>剩余时间</returns>
            public TimeSpan GetRemainingTime(DateTime currentTime)
            {
                var timeUntilNext = NextRefreshTime - currentTime;
                return timeUntilNext.TotalMilliseconds > 0 ? timeUntilNext : TimeSpan.Zero;
            }
        }
    }
}
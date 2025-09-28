using System;

namespace TEngine
{
    public interface ITimerModule
    {
        /// <summary>
        /// 添加计时器。
        /// </summary>
        /// <param name="callback">计时器回调。</param>
        /// <param name="time">计时器间隔。</param>
        /// <param name="isLoop">是否循环。</param>
        /// <param name="isUnscaled">是否不收时间缩放影响。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        public int AddTimer(TimerHandler callback, float time, bool isLoop = false, bool isUnscaled = false, params object[] args);

        /// <summary>
        /// 暂停计时器。
        /// </summary>
        /// <param name="timerId">计时器Id。</param>
        public void Stop(int timerId);

        /// <summary>
        /// 恢复计时器。
        /// </summary>
        /// <param name="timerId">计时器Id。</param>
        public void Resume(int timerId);

        /// <summary>
        /// 计时器是否在运行中。
        /// </summary>
        /// <param name="timerId">计时器Id。</param>
        /// <returns>否在运行中。</returns>
        public bool IsRunning(int timerId);

        /// <summary>
        /// 获得计时器剩余时间。
        /// </summary>
        public float GetLeftTime(int timerId);

        /// <summary>
        /// 重置计时器,恢复到开始状态。
        /// </summary>
        public void Restart(int timerId);

        /// <summary>
        /// 重置计时器。
        /// </summary>
        public void ResetTimer(int timerId, TimerHandler callback, float time, bool isLoop = false, bool isUnscaled = false);

        /// <summary>
        /// 重置计时器。
        /// </summary>
        public void ResetTimer(int timerId, float time, bool isLoop, bool isUnscaled);

        /// <summary>
        /// 移除计时器。
        /// </summary>
        /// <param name="timerId">计时器Id。</param>
        public void RemoveTimer(int timerId);

        /// <summary>
        /// 移除所有计时器。
        /// </summary>
        public void RemoveAllTimer();

        /// <summary>
        /// 添加每日刷新任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="hour">刷新小时（0-23）</param>
        /// <param name="minute">刷新分钟（0-59）</param>
        /// <param name="lastRefreshTime">上次刷新时间戳（0表示从未刷新）</param>
        /// <param name="onRefresh">刷新回调</param>
        /// <param name="onCountdown">倒计时回调</param>
        /// <param name="args">附加参数</param>
        public void AddDailyRefresh(string taskId, int hour, int minute, long lastRefreshTime,
            RefreshCallback onRefresh, CountdownCallback onCountdown = null, params object[] args);

        /// <summary>
        /// 添加每周刷新任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="dayOfWeek">星期几</param>
        /// <param name="hour">刷新小时（0-23）</param>
        /// <param name="minute">刷新分钟（0-59）</param>
        /// <param name="lastRefreshTime">上次刷新时间戳（0表示从未刷新）</param>
        /// <param name="onRefresh">刷新回调</param>
        /// <param name="onCountdown">倒计时回调</param>
        /// <param name="args">附加参数</param>
        public void AddWeeklyRefresh(string taskId, DayOfWeek dayOfWeek, int hour, int minute, long lastRefreshTime,
            RefreshCallback onRefresh, CountdownCallback onCountdown = null, params object[] args);

        /// <summary>
        /// 添加每月刷新任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="day">刷新日期（1-28）</param>
        /// <param name="hour">刷新小时（0-23）</param>
        /// <param name="minute">刷新分钟（0-59）</param>
        /// <param name="lastRefreshTime">上次刷新时间戳（0表示从未刷新）</param>
        /// <param name="onRefresh">刷新回调</param>
        /// <param name="onCountdown">倒计时回调</param>
        /// <param name="args">附加参数</param>
        public void AddMonthlyRefresh(string taskId, int day, int hour, int minute, long lastRefreshTime,
            RefreshCallback onRefresh, CountdownCallback onCountdown = null, params object[] args);

        /// <summary>
        /// 添加间隔刷新任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <param name="intervalSeconds">间隔秒数</param>
        /// <param name="lastRefreshTime">上次刷新时间戳（0表示从未刷新）</param>
        /// <param name="onRefresh">刷新回调</param>
        /// <param name="onCountdown">倒计时回调</param>
        /// <param name="args">附加参数</param>
        public void AddIntervalRefresh(string taskId, int intervalSeconds, long lastRefreshTime,
            RefreshCallback onRefresh, CountdownCallback onCountdown = null, params object[] args);

        /// <summary>
        /// 移除刷新任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveRefreshTask(string taskId);

        /// <summary>
        /// 获取刷新任务剩余时间
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>剩余时间（毫秒），-1表示任务不存在</returns>
        public long GetRefreshTaskRemainingTime(string taskId);

        /// <summary>
        /// 立即执行刷新任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否执行成功</returns>
        public bool ExecuteRefreshTaskNow(string taskId);

        /// <summary>
        /// 清空所有刷新任务
        /// </summary>
        public void ClearAllRefreshTasks();
    }
}
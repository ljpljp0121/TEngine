using System;
using System.Collections.Generic;
using UnityEngine;

namespace TEngine
{
    internal partial class TimerModule : ITimerModule
    {
        /// <summary>
        /// 刷新任务字典
        /// </summary>
        private readonly Dictionary<string, RefreshTask> refreshTasks = new Dictionary<string, RefreshTask>();

        /// <summary>
        /// 刷新系统更新间隔（秒）
        /// </summary>
        private float refreshUpdateInterval = 1.0f;

        /// <summary>
        /// 刷新系统计时器
        /// </summary>
        private float refreshTimer = 0f;

        #region 刷新系统公开接口

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
            RefreshCallback onRefresh, CountdownCallback onCountdown = null, params object[] args)
        {
            var config = new DailyRefreshConfig(taskId, hour, minute);
            if (lastRefreshTime > 0)
            {
                // config.LastRefreshTime = TimeUtil.UnixTimeToDateTime(lastRefreshTime);
            }

            var task = new RefreshTask(taskId, config, onRefresh, onCountdown, args);
            task.UpdateNextRefreshTime(DateTime.Now);

            refreshTasks[taskId] = task;

            Log.Info($"[RefreshSystem] 添加每日刷新任务: {taskId}, 刷新时间: {hour:D2}:{minute:D2}");
        }

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
            RefreshCallback onRefresh, CountdownCallback onCountdown = null, params object[] args)
        {
            var config = new WeeklyRefreshConfig(taskId, dayOfWeek, hour, minute);
            if (lastRefreshTime > 0)
            {
                // config.LastRefreshTime = TimeUtil.UnixTimeToDateTime(lastRefreshTime);
            }

            var task = new RefreshTask(taskId, config, onRefresh, onCountdown, args);
            task.UpdateNextRefreshTime(DateTime.Now);

            refreshTasks[taskId] = task;

            Log.Info($"[RefreshSystem] 添加每周刷新任务: {taskId}, 刷新时间: {dayOfWeek} {hour:D2}:{minute:D2}");
        }

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
            RefreshCallback onRefresh, CountdownCallback onCountdown = null, params object[] args)
        {
            var config = new MonthlyRefreshConfig(taskId, day, hour, minute);
            if (lastRefreshTime > 0)
            {
                // config.LastRefreshTime = TimeUtil.UnixTimeToDateTime(lastRefreshTime);
            }

            var task = new RefreshTask(taskId, config, onRefresh, onCountdown, args);
            task.UpdateNextRefreshTime(DateTime.Now);

            refreshTasks[taskId] = task;

            Log.Info($"[RefreshSystem] 添加每月刷新任务: {taskId}, 刷新时间: 每月{day}日 {hour:D2}:{minute:D2}");
        }

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
            RefreshCallback onRefresh, CountdownCallback onCountdown = null, params object[] args)
        {
            var interval = TimeSpan.FromSeconds(intervalSeconds);
            var config = new IntervalRefreshConfig(taskId, interval);
            if (lastRefreshTime > 0)
            {
                // config.LastRefreshTime = TimeUtil.UnixTimeToDateTime(lastRefreshTime);
            }

            var task = new RefreshTask(taskId, config, onRefresh, onCountdown, args);
            task.UpdateNextRefreshTime(DateTime.Now);

            refreshTasks[taskId] = task;

            Log.Info($"[RefreshSystem] 添加间隔刷新任务: {taskId}, 间隔: {intervalSeconds}秒");
        }

        /// <summary>
        /// 移除刷新任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveRefreshTask(string taskId)
        {
            if (refreshTasks.Remove(taskId))
            {
                Log.Info($"[RefreshSystem] 移除刷新任务: {taskId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取刷新任务剩余时间
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>剩余时间（毫秒），-1表示任务不存在</returns>
        public long GetRefreshTaskRemainingTime(string taskId)
        {
            if (refreshTasks.TryGetValue(taskId, out var task))
            {
                var remaining = task.GetRemainingTime(DateTime.Now);
                return (long)remaining.TotalMilliseconds;
            }
            return -1;
        }

        /// <summary>
        /// 立即执行刷新任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否执行成功</returns>
        public bool ExecuteRefreshTaskNow(string taskId)
        {
            if (refreshTasks.TryGetValue(taskId, out var task))
            {
                task.ExecuteRefresh(DateTime.Now);
                Log.Info($"[RefreshSystem] 立即执行刷新任务: {taskId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清空所有刷新任务
        /// </summary>
        public void ClearAllRefreshTasks()
        {
            refreshTasks.Clear();
            Log.Info("[RefreshSystem] 清空所有刷新任务");
        }

        #endregion

        #region 内部刷新逻辑

        /// <summary>
        /// 更新刷新系统
        /// </summary>
        private void UpdateRefreshSystem(float realElapseSeconds)
        {
            refreshTimer += realElapseSeconds;

            if (refreshTimer >= refreshUpdateInterval)
            {
                refreshTimer -= refreshUpdateInterval;

                var currentTime = DateTime.Now;
                var tasksToRemove = new List<string>();

                foreach (var kvp in refreshTasks)
                {
                    var task = kvp.Value;
                    if (task.ShouldRefresh(currentTime))
                    {
                        task.ExecuteRefresh(currentTime);
                    }
                    else
                    {
                        task.ExecuteCountdown(currentTime);
                    }
                }
                foreach (var taskId in tasksToRemove)
                {
                    refreshTasks.Remove(taskId);
                }
            }
        }

        #endregion
    }
}
using System;
using UnityEditor;

namespace TEngine
{
    internal partial class TimerModule
    {
        /// <summary>
        /// 刷新配置基类
        /// </summary>
        private abstract class RefreshConfig
        {
            /// <summary>
            /// 配置唯一标识
            /// </summary>
            public string Id { get; protected set; }

            /// <summary>
            /// 上次刷新时间
            /// </summary>
            public DateTime LastRefreshTime { get; set; }

            protected RefreshConfig(string id)
            {
                Id = id;
                LastRefreshTime = DateTime.MinValue;
            }

            /// <summary>
            /// 计算下一次刷新时间
            /// </summary>
            /// <param name="currentTime">当前时间</param>
            /// <returns>下次刷新时间</returns>
            public abstract DateTime CalculateNextRefreshTime(DateTime currentTime);

            /// <summary>
            /// 检查是否应该刷新
            /// </summary>
            /// <param name="currentTime">当前时间</param>
            /// <returns>是否需要刷新</returns>
            public virtual bool ShouldRefresh(DateTime currentTime)
            {
                var nextRefreshTime = CalculateNextRefreshTime(currentTime);
                return currentTime >= nextRefreshTime;
            }
        }

        /// <summary>
        /// 每日刷新配置
        /// </summary>
        private class DailyRefreshConfig : RefreshConfig
        {
            /// <summary>
            /// 刷新小时（0-23）
            /// </summary>
            public int RefreshHour { get; private set; }

            /// <summary>
            /// 刷新分钟（0-59）
            /// </summary>
            public int RefreshMinute { get; private set; }

            public DailyRefreshConfig(string id, int hour, int minute = 0) : base(id)
            {
                RefreshHour = Math.Max(0, Math.Min(23, hour));
                RefreshMinute = Math.Max(0, Math.Min(59, minute));
            }

            public override DateTime CalculateNextRefreshTime(DateTime currentTime)
            {
                var todayRefreshTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, RefreshHour,
                    RefreshMinute, 0);

                // 如果今天的刷新时间还没到，返回今天的刷新时间
                if (currentTime < todayRefreshTime && LastRefreshTime < todayRefreshTime)
                {
                    return todayRefreshTime;
                }

                // 否则返回明天的刷新时间
                return todayRefreshTime.AddDays(1);
            }
        }

        /// <summary>
        /// 每周刷新配置
        /// </summary>
        private class WeeklyRefreshConfig : RefreshConfig
        {
            /// <summary>
            /// 刷新星期几
            /// </summary>
            public DayOfWeek RefreshDayOfWeek { get; private set; }

            /// <summary>
            /// 刷新小时（0-23）
            /// </summary>
            public int RefreshHour { get; private set; }

            /// <summary>
            /// 刷新分钟（0-59）
            /// </summary>
            public int RefreshMinute { get; private set; }

            public WeeklyRefreshConfig(string id, DayOfWeek dayOfWeek, int hour, int minute = 0) : base(id)
            {
                RefreshDayOfWeek = dayOfWeek;
                RefreshHour = Math.Max(0, Math.Min(23, hour));
                RefreshMinute = Math.Max(0, Math.Min(59, minute));
            }

            public override DateTime CalculateNextRefreshTime(DateTime currentTime)
            {
                // 计算这周的刷新时间
                int daysUntilRefresh = ((int)RefreshDayOfWeek - (int)currentTime.DayOfWeek + 7) % 7;
                var thisWeekRefreshTime = currentTime.Date.AddDays(daysUntilRefresh)
                    .AddHours(RefreshHour).AddMinutes(RefreshMinute);

                // 如果这周的刷新时间还没到且还没刷新过，返回这周的刷新时间
                if (currentTime < thisWeekRefreshTime && LastRefreshTime < thisWeekRefreshTime)
                {
                    return thisWeekRefreshTime;
                }

                // 否则返回下周的刷新时间
                return thisWeekRefreshTime.AddDays(7);
            }
        }

        /// <summary>
        /// 每月刷新配置
        /// </summary>
        private class MonthlyRefreshConfig : RefreshConfig
        {
            /// <summary>
            /// 刷新日期（1-28，避免月末问题）
            /// </summary>
            public int RefreshDay { get; private set; }

            /// <summary>
            /// 刷新小时（0-23）
            /// </summary>
            public int RefreshHour { get; private set; }

            /// <summary>
            /// 刷新分钟（0-59）
            /// </summary>
            public int RefreshMinute { get; private set; }

            public MonthlyRefreshConfig(string id, int day, int hour, int minute = 0) : base(id)
            {
                RefreshDay = Math.Max(1, Math.Min(28, day)); // 限制在1-28，避免月末问题
                RefreshHour = Math.Max(0, Math.Min(23, hour));
                RefreshMinute = Math.Max(0, Math.Min(59, minute));
            }

            public override DateTime CalculateNextRefreshTime(DateTime currentTime)
            {
                var thisMonthRefreshTime = new DateTime(currentTime.Year, currentTime.Month, RefreshDay, RefreshHour,
                    RefreshMinute, 0);

                // 如果这个月的刷新时间还没到且还没刷新过，返回这个月的刷新时间
                if (currentTime < thisMonthRefreshTime && LastRefreshTime < thisMonthRefreshTime)
                {
                    return thisMonthRefreshTime;
                }

                // 否则返回下个月的刷新时间
                return thisMonthRefreshTime.AddMonths(1);
            }
        }

        /// <summary>
        /// 间隔刷新配置
        /// </summary>
        private class IntervalRefreshConfig : RefreshConfig
        {
            /// <summary>
            /// 刷新间隔
            /// </summary>
            public TimeSpan RefreshInterval { get; private set; }

            public IntervalRefreshConfig(string id, TimeSpan interval) : base(id)
            {
                RefreshInterval = interval;
            }

            public override DateTime CalculateNextRefreshTime(DateTime currentTime)
            {
                if (LastRefreshTime == DateTime.MinValue)
                {
                    // 首次刷新，立即执行
                    return currentTime;
                }

                return LastRefreshTime.Add(RefreshInterval);
            }
        }
    }
}
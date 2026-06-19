using System;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 为 AbilityTask 注册完成、失败、取消和结束回调的链式扩展。
    /// </summary>
    public static class AbilityTaskCallbackExtensions
    {
        public static T OnCompleted<T>(this T task, Action<T> callback)
            where T : AbilityTask
        {
            if (callback != null)
            {
                task.Completed += completedTask => callback((T)completedTask);
            }

            return task;
        }

        public static T OnFailed<T>(this T task, Action<T> callback)
            where T : AbilityTask
        {
            if (callback != null)
            {
                task.Failed += failedTask => callback((T)failedTask);
            }

            return task;
        }

        public static T OnCancelled<T>(this T task, Action<T> callback)
            where T : AbilityTask
        {
            if (callback != null)
            {
                task.Cancelled += cancelledTask => callback((T)cancelledTask);
            }

            return task;
        }

        public static T OnFinished<T>(this T task, Action<T, bool> callback)
            where T : AbilityTask
        {
            if (callback != null)
            {
                task.Finished += (finishedTask, succeeded) => callback((T)finishedTask, succeeded);
            }

            return task;
        }
    }
}

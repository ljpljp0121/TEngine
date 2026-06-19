using System;
using System.Collections.Generic;
using UnityEngine;

namespace PFGraph
{
    [ViewModel(typeof(BasePort))]
    public class PortProcessor : ViewModel, IGraphElementProcessor
    {
        private BasePort model;
        private Type modelType;
        private bool hideLabel;

        private BaseNodeProcessor owner;

        internal readonly List<BaseConnectionProcessor> connections = new List<BaseConnectionProcessor>();
        [ThreadStatic] private static HashSet<string> evaluationStack;
        [ThreadStatic] private static Dictionary<string, object> evaluationCache;
        [ThreadStatic] private static int evaluationDepth;

        public event Action<BaseConnectionProcessor> OnConnected;
        public event Action<BaseConnectionProcessor> OnDisconnected;
        public event Action OnConnectionChanged;

        public PortProcessor(BasePort model)
        {
            this.model = model;
            this.modelType = model.GetType();
        }

        public BasePort Model => model;

        object IGraphElementProcessor.Model => model;

        public Type ModelType => modelType;

        public string Name => model.name;

        public BasePort.Direction Direction => model.direction;

        public BasePort.Capacity Capacity => model.capacity;

        public Type PortType
        {
            get => model.portType == null ? typeof(object) : model.portType;
            set => SetFieldValue(ref model.portType, value, nameof(BasePort.portType));
        }

        public bool HideLabel
        {
            get => hideLabel;
            set => SetFieldValue(ref hideLabel, value, nameof(hideLabel));
        }

        public IReadOnlyList<BaseConnectionProcessor> Connections => connections;

        public BaseNodeProcessor Owner
        {
            get => owner;
            internal set => owner = value;
        }

        public PortProcessor(string name, BasePort.Direction direction, BasePort.Capacity capacity, Type type = null)
        {
            this.model = new BasePort(name, direction, capacity, type);
            this.modelType = model.GetType();
        }

        #region API

        public void ConnectTo(BaseConnectionProcessor connection)
        {
            connections.Add(connection);

            switch (this.Direction)
            {
                case BasePort.Direction.Left:
                {
                    connections.QuickSort(ConnectionProcessorHorizontalComparer.ToPortSortDefault);
                    break;
                }
                case BasePort.Direction.Right:
                {
                    connections.QuickSort(ConnectionProcessorHorizontalComparer.FromPortSortDefault);
                    break;
                }
                case BasePort.Direction.Top:
                {
                    connections.QuickSort(ConnectionProcessorVerticalComparer.InPortSortDefault);
                    break;
                }
                case BasePort.Direction.Bottom:
                {
                    connections.QuickSort(ConnectionProcessorVerticalComparer.OutPortSortDefault);
                    break;
                }
            }

            OnConnected?.Invoke(connection);
            OnConnectionChanged?.Invoke();
        }

        public void DisconnectTo(BaseConnectionProcessor connection)
        {
            connections.Remove(connection);
            OnDisconnected?.Invoke(connection);
            OnConnectionChanged?.Invoke();
        }

        /// <summary>
        /// 整理
        /// </summary>
        public bool Trim()
        {
            var removeNum = connections.RemoveAll(ConnectionProcessorComparer.EmptyComparer);

            switch (Direction)
            {
                case BasePort.Direction.Left:
                    return removeNum != 0 &&
                           connections.QuickSort(ConnectionProcessorHorizontalComparer.ToPortSortDefault);
                case BasePort.Direction.Right:
                    return removeNum != 0 &&
                           connections.QuickSort(ConnectionProcessorHorizontalComparer.FromPortSortDefault);
                case BasePort.Direction.Top:
                    return removeNum != 0 &&
                           connections.QuickSort(ConnectionProcessorVerticalComparer.InPortSortDefault);
                case BasePort.Direction.Bottom:
                    return removeNum != 0 &&
                           connections.QuickSort(ConnectionProcessorVerticalComparer.OutPortSortDefault);
            }

            return removeNum != 0;
        }

        /// <summary>
        /// 获取连接的第一个接口的值（直接遍历，避免 LINQ 迭代器分配）
        /// </summary>
        public object GetConnectionValue()
        {
            if (!TryEnterEvaluation(out var evaluationKey))
                return null;

            try
            {
                if (TryGetCachedValue<object>(evaluationKey, out var cachedValue))
                    return cachedValue;

                object result = null;
                if (Model.direction == BasePort.Direction.Left)
                {
                    foreach (var connection in connections)
                    {
                        if (connection.FromNode is IGetPortValue fromPort)
                        {
                            result = fromPort.GetValue(connection.FromPortName);
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var connection in connections)
                    {
                        if (connection.ToNode is IGetPortValue toPort)
                        {
                            result = toPort.GetValue(connection.ToPortName);
                            break;
                        }
                    }
                }

                CacheValue(evaluationKey, result);
                return result;
            }
            finally
            {
                ExitEvaluation(evaluationKey);
            }
        }

        /// <summary>
        /// 获取连接的接口的值
        /// </summary>
        public IEnumerable<object> GetConnectionValues()
        {
            if (!TryEnterEvaluation(out var evaluationKey))
                yield break;

            try
            {
                if (Model.direction == BasePort.Direction.Left)
                {
                    foreach (var connection in Connections)
                    {
                        if (connection.FromNode is IGetPortValue fromPort)
                            yield return fromPort.GetValue(connection.FromPortName);
                    }
                }
                else
                {
                    foreach (var connection in Connections)
                    {
                        if (connection.ToNode is IGetPortValue toPort)
                            yield return toPort.GetValue(connection.ToPortName);
                    }
                }
            }
            finally
            {
                ExitEvaluation(evaluationKey);
            }
        }

        /// <summary>
        /// 获取连接的第一个接口的值（直接遍历，避免 LINQ 迭代器分配）
        /// </summary>
        public T GetConnectionValue<T>()
        {
            if (!TryEnterEvaluation(out var evaluationKey))
                return default;

            try
            {
                if (TryGetCachedValue<T>(evaluationKey, out var cachedValue))
                    return cachedValue;

                T result = default;
                var found = false;
                if (Model.direction == BasePort.Direction.Left)
                {
                    foreach (var connection in connections)
                    {
                        if (connection.FromNode is IGetPortValue<T> fromPort)
                        {
                            result = fromPort.GetValue(connection.FromPortName);
                            found = true;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var connection in connections)
                    {
                        if (connection.ToNode is IGetPortValue<T> toPort)
                        {
                            result = toPort.GetValue(connection.ToPortName);
                            found = true;
                            break;
                        }
                    }
                }

                if (found || typeof(T).IsValueType)
                    CacheValue(evaluationKey, result);

                return result;
            }
            finally
            {
                ExitEvaluation(evaluationKey);
            }
        }

        /// <summary>
        /// 获取连接的接口的值
        /// </summary>
        public IEnumerable<T> GetConnectionValues<T>()
        {
            if (!TryEnterEvaluation(out var evaluationKey))
                yield break;

            try
            {
                if (Model.direction == BasePort.Direction.Left)
                {
                    foreach (var connection in Connections)
                    {
                        if (connection.FromNode is IGetPortValue<T> fromPort)
                            yield return fromPort.GetValue(connection.FromPortName);
                    }
                }
                else
                {
                    foreach (var connection in Connections)
                    {
                        if (connection.ToNode is IGetPortValue<T> toPort)
                            yield return toPort.GetValue(connection.ToPortName);
                    }
                }
            }
            finally
            {
                ExitEvaluation(evaluationKey);
            }
        }

        private bool TryEnterEvaluation(out string evaluationKey)
        {
            var ownerId = Owner?.ID ?? 0;
            evaluationKey = ownerId + ":" + Name + ":" + Direction;
            var isRootEvaluation = evaluationDepth == 0;
            evaluationDepth++;
            if (isRootEvaluation)
                evaluationCache = new Dictionary<string, object>();
            evaluationStack ??= new HashSet<string>();
            if (evaluationStack.Add(evaluationKey))
                return true;

            evaluationDepth--;
            if (evaluationDepth == 0)
                evaluationCache?.Clear();

            var message =
                $"[GraphProcessor] Cyclic port evaluation detected at node={ownerId}, port={Name}, direction={Direction}.";
            Owner?.Owner?.ReportDiagnostic(message);
            Debug.LogError(message);
            return false;
        }

        private static void ExitEvaluation(string evaluationKey)
        {
            evaluationStack?.Remove(evaluationKey);
            evaluationDepth--;
            if (evaluationDepth <= 0)
            {
                evaluationDepth = 0;
                evaluationCache?.Clear();
            }
        }

        private static bool TryGetCachedValue<T>(string evaluationKey, out T value)
        {
            if (evaluationCache != null && evaluationCache.TryGetValue(evaluationKey, out var cached) &&
                cached is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        private static void CacheValue(string evaluationKey, object value)
        {
            evaluationCache ??= new Dictionary<string, object>();
            evaluationCache[evaluationKey] = value;
        }

        #endregion
    }
}
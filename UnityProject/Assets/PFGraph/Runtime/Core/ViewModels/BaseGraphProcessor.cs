using System;
using System.Collections.Generic;

namespace PFGraph
{
    [ViewModel(typeof(BaseGraph))]
    public partial class BaseGraphProcessor : ViewModel
    {
        /// <summary>
        /// Graph数据
        /// </summary>
        private readonly BaseGraph model;

        /// <summary>
        /// Graph数据Type
        /// </summary>
        private readonly Type modelType;

        /// <summary>
        /// Graph的操作事件
        /// </summary>
        private readonly GraphEvents graphEvents;

        /// <summary>
        /// 自定义事件
        /// </summary>
        private readonly EventStation<string> events;

        /// <summary>
        /// 黑板
        /// </summary>
        private readonly BlackboardProcessor<string> blackboard;

        /// <summary>
        /// 图加载/修复诊断信息
        /// </summary>
        private readonly List<string> diagnostics;

        public BaseGraphProcessor(BaseGraph model)
        {
            this.model = model;
            modelType = model.GetType();
            this.model.pan = this.model.pan == default ? InternalVector2Int.zero : this.model.pan;
            this.model.zoom = this.model.zoom == 0 ? 1 : this.model.zoom;
            // notes 在 BaseGraph 中已初始化，此处保持防御性空检查与 nodes/connections 对齐
            if (this.model.nodes == null) this.model.nodes = new List<BaseNode>();
            if (this.model.connections == null) this.model.connections = new List<BaseConnection>();
            if (this.model.groups == null) this.model.groups = new List<Group>();
            if (this.model.notes == null) this.model.notes = new List<StickyNote>();
            if (this.model.placemats == null) this.model.placemats = new List<PlacematData>();

            graphEvents = new GraphEvents();
            events = new EventStation<string>();
            blackboard = new BlackboardProcessor<string>(new Blackboard<string>(), new EventStation<string>());
            diagnostics = new List<string>(16);

            BeginInitNodes();
            BeginInitConnections();
            EndInitConnections();
            EndInitNodes();
            InitGroups();
            InitNotes();
            InitPlacemats();
        }

        public BaseGraph Model => model;

        public Type ModelType => modelType;

        public InternalVector2Int Pan
        {
            get => Model.pan;
            set => SetFieldValue(ref Model.pan, value, nameof(BaseGraph.pan));
        }

        public float Zoom
        {
            get => Model.zoom;
            set => SetFieldValue(ref Model.zoom, value, nameof(BaseGraph.zoom));
        }

        public GraphEvents GraphEvents => graphEvents;

        public EventStation<string> Events => events;

        public BlackboardProcessor<string> Blackboard => blackboard;

        public IReadOnlyList<string> Diagnostics => diagnostics;

        public GraphValidationResult ValidateModel()
        {
            return GraphValidationUtil.Validate(model);
        }

        public GraphValidationResult RepairModel()
        {
            var result = GraphValidationUtil.Repair(model);
            AppendDiagnostics(result.Messages);
            return result;
        }

        internal void ReportDiagnostic(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            diagnostics.Add(message);
        }

        internal void AppendDiagnostics(IEnumerable<string> messages)
        {
            if (messages == null)
                return;

            foreach (var message in messages)
            {
                ReportDiagnostic(message);
            }
        }
    }
}
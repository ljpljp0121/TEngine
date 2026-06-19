using System;
using System.Collections.Generic;

namespace PFGraph
{
    [ViewModel(typeof(Group))]
    public class GroupProcessor : ViewModel, IGraphElementProcessor, IGraphElementProcessor_Scope
    {
        private Group model;
        private Type modelType;
        private BaseGraphProcessor owner;


        public Group Model => model;

        object IGraphElementProcessor.Model => model;

        public Type ModelType => modelType;

        Type IGraphElementProcessor.ModelType => modelType;

        public long ID => Model.id;

        public IReadOnlyList<long> Nodes => Model.nodes;

        public BaseGraphProcessor Owner
        {
            get => owner;
            internal set => owner = value;
        }

        public string GroupName
        {
            get => Model.groupName;
            set => SetFieldValue(ref Model.groupName, value, nameof(Model.groupName));
        }

        public InternalVector2Int Position
        {
            get => Model.position;
            set => SetFieldValue(ref Model.position, value, nameof(Model.position));
        }

        public InternalColor BackgroundColor
        {
            get => Model.backgroundColor;
            set => SetFieldValue(ref Model.backgroundColor, value, nameof(Model.backgroundColor));
        }

        public GroupProcessor(Group model)
        {
            this.model = model;
            this.modelType = model.GetType();
            this.model.position = model.position == default ? InternalVector2Int.zero : model.position;
        }

        internal void NotifyNodeAdded(BaseNodeProcessor node)
        {
            Owner.GraphEvents.Publish(new AddNodesToGroupEventArgs(this, node));
        }

        internal void NotifyNodeRemoved(BaseNodeProcessor node)
        {
            Owner.GraphEvents.Publish(new RemoveNodesFromGroupEventArgs(this, node));
        }
    }
}
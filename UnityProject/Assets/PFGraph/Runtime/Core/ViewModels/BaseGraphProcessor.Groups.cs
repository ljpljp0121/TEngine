using System.Collections.Generic;

namespace PFGraph
{
    public partial class BaseGraphProcessor
    {
        private Groups groups;

        public Groups Groups => groups;

        private void InitGroups()
        {
            this.groups = new Groups();

            for (int i = 0; i < Model.groups.Count; i++)
            {
                var group = Model.groups[i];
                if (group == null)
                {
                    Model.groups.RemoveAt(i--);
                    continue;
                }

                for (int j = group.nodes.Count - 1; j >= 0; j--)
                {
                    if (!nodes.ContainsKey(group.nodes[j]))
                        group.nodes.RemoveAt(j);
                }

                var groupVM = (GroupProcessor)ViewModelFactory.ProduceViewModel(group);
                groupVM.Owner = this;
                groups.AddGroup(groupVM);
            }
        }

        #region API

        public void AddGroup(GroupProcessor group)
        {
            groups.AddGroup(group);
            model.groups.Add(group.Model);
            group.Owner = this;
            graphEvents.Publish(new AddGroupEventArgs(group));
        }

        public void RemoveGroup(GroupProcessor group)
        {
            groups.RemoveGroup(group);
            model.groups.Remove(group.Model);
            graphEvents.Publish(new RemoveGroupEventArgs(group));
        }

        public virtual GroupProcessor NewGroup(string groupName)
        {
            var group = new Group()
            {
                id = GraphProcessorUtil.GenerateId(),
                groupName = groupName
            };
            return ViewModelFactory.ProduceViewModel(group) as GroupProcessor;
        }

        #endregion
    }

    public class Groups
    {
        private Dictionary<long, GroupProcessor> groupMap = new Dictionary<long, GroupProcessor>();
        private Dictionary<long, GroupProcessor> nodeGroupMap = new Dictionary<long, GroupProcessor>();

        public IReadOnlyDictionary<long, GroupProcessor> GroupMap => groupMap;

        public IReadOnlyDictionary<long, GroupProcessor> NodeGroupMap => nodeGroupMap;

        public void AddNodeToGroup(GroupProcessor group, BaseNodeProcessor node)
        {
            if (nodeGroupMap.TryGetValue(node.ID, out var _group))
            {
                if (_group == group)
                {
                    return;
                }
                else
                {
                    _group.Model.nodes.Remove(node.ID);
                    _group.NotifyNodeRemoved(node);
                }
            }

            nodeGroupMap[node.ID] = group;
            group.Model.nodes.Add(node.ID);
            group.NotifyNodeAdded(node);
        }

        public void RemoveNodeFromGroup(BaseNodeProcessor node)
        {
            if (!nodeGroupMap.TryGetValue(node.ID, out var group))
                return;

            nodeGroupMap.Remove(node.ID);
            group.Model.nodes.Remove(node.ID);
            group.NotifyNodeRemoved(node);
        }

        public void AddGroup(GroupProcessor group)
        {
            this.groupMap.Add(group.ID, group);
            // 只处理新加入的 group 中的节点映射，不重复遍历已有 group
            foreach (var nodeID in group.Nodes)
            {
                this.nodeGroupMap[nodeID] = group;
            }
        }

        public bool RemoveGroup(GroupProcessor group)
        {
            if (groupMap.Remove(group.ID))
            {
                foreach (var nodeID in group.Nodes)
                {
                    nodeGroupMap.Remove(nodeID);
                }

                return true;
            }

            return false;
        }
    }
}
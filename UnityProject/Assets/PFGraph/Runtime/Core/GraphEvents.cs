using System;

namespace PFGraph
{
    public sealed class GraphEvents
    {
        private readonly EventStation<Type> eventStation = new EventStation<Type>();

        public bool HasEvent<TArg>() where TArg : struct, IGraphEvent
        {
            return eventStation.HasEvent(typeof(TArg));
        }

        public EventBase GetEvent<TArg>() where TArg : struct, IGraphEvent
        {
            return eventStation.GetEvent(typeof(TArg));
        }

        public void UnRegisterEvent<TArg>() where TArg : struct, IGraphEvent
        {
            eventStation.Unregister(typeof(TArg));
        }

        public void UnRegisterAllEvents()
        {
            eventStation.UnregisterAll();
        }

        public void Subscribe<TArg>(Action<TArg> handler) where TArg : struct, IGraphEvent
        {
            eventStation.Subscribe(typeof(TArg), handler);
        }

        public void Unsubscribe<TArg>(Action<TArg> handler) where TArg : struct, IGraphEvent
        {
            eventStation.Unsubscribe(typeof(TArg), handler);
        }

        public void Publish<TArg>(in TArg arg) where TArg : struct, IGraphEvent
        {
            eventStation.Publish(typeof(TArg), arg);
        }
    }

    public interface IGraphEvent
    {
    }

    public struct AddNodeEventArgs : IGraphEvent
    {
        public BaseNodeProcessor Node;

        public AddNodeEventArgs(BaseNodeProcessor addedNode)
        {
            Node = addedNode;
        }
    }

    public struct RemoveNodeEventArgs : IGraphEvent
    {
        public BaseNodeProcessor Node;

        public RemoveNodeEventArgs(BaseNodeProcessor removedNode)
        {
            Node = removedNode;
        }
    }

    public struct AddConnectionEventArgs : IGraphEvent
    {
        public BaseConnectionProcessor Connection;

        public AddConnectionEventArgs(BaseConnectionProcessor addedConnection)
        {
            Connection = addedConnection;
        }
    }

    public struct RemoveConnectionEventArgs : IGraphEvent
    {
        public BaseConnectionProcessor Connection;

        public RemoveConnectionEventArgs(BaseConnectionProcessor removedConnection)
        {
            Connection = removedConnection;
        }
    }

    public struct AddGroupEventArgs : IGraphEvent
    {
        public GroupProcessor Group;

        public AddGroupEventArgs(GroupProcessor addedGroup)
        {
            Group = addedGroup;
        }
    }

    public struct RemoveGroupEventArgs : IGraphEvent
    {
        public GroupProcessor Group;

        public RemoveGroupEventArgs(GroupProcessor removedGroup)
        {
            Group = removedGroup;
        }
    }

    public struct AddNodesToGroupEventArgs : IGraphEvent
    {
        public GroupProcessor Group;
        public BaseNodeProcessor Node;

        public AddNodesToGroupEventArgs(GroupProcessor group, BaseNodeProcessor addedNode)
        {
            Group = group;
            Node = addedNode;
        }
    }

    public struct RemoveNodesFromGroupEventArgs : IGraphEvent
    {
        public GroupProcessor Group;
        public BaseNodeProcessor Node;

        public RemoveNodesFromGroupEventArgs(GroupProcessor group, BaseNodeProcessor removedNode)
        {
            Group = group;
            Node = removedNode;
        }
    }

    public struct AddNoteEventArgs : IGraphEvent
    {
        public StickyNoteProcessor Note;

        public AddNoteEventArgs(StickyNoteProcessor addedNote)
        {
            Note = addedNote;
        }
    }

    public struct RemoveNoteEventArgs : IGraphEvent
    {
        public StickyNoteProcessor Note;

        public RemoveNoteEventArgs(StickyNoteProcessor removedNote)
        {
            Note = removedNote;
        }
    }

    public struct AddPlacematEventArgs : IGraphEvent
    {
        public PlacematProcessor Placemat;

        public AddPlacematEventArgs(PlacematProcessor addedPlacemat)
        {
            Placemat = addedPlacemat;
        }
    }

    public struct RemovePlacematEventArgs : IGraphEvent
    {
        public PlacematProcessor Placemat;

        public RemovePlacematEventArgs(PlacematProcessor removedPlacemat)
        {
            Placemat = removedPlacemat;
        }
    }
}

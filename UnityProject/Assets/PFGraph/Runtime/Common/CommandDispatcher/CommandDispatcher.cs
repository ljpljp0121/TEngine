using System;
using System.Collections.Generic;

namespace PFGraph
{
    public sealed class CommandDispatcher
    {
        private readonly object lock_ = new object();
        private readonly LinkedList<ICommand> undoList = new LinkedList<ICommand>();
        private readonly Stack<ICommand> redoList = new Stack<ICommand>();
        private CommandGroup currentGroup;
        private readonly int recordLimit;

        public CommandDispatcher(int recordLimit = 0)
        {
            this.recordLimit = recordLimit;
        }

        public void Register(ICommand command)
        {
            if (command == null)
                return;

            redoList.Clear();
            if (currentGroup != null)
            {
                currentGroup.Commands.Add(command);
            }
            else
            {
                undoList.AddLast(command);
                while (recordLimit > 0 && undoList.Count > recordLimit)
                {
                    undoList.RemoveFirst();
                }
            }
        }

        public bool CanUndo()
        {
            return undoList.Count > 0;
        }

        public bool CanRedo()
        {
            return redoList.Count > 0;
        }

        public void BeginGroup()
        {
            lock (lock_)
            {
                if (currentGroup != null)
                {
                    throw new Exception("Current is already in a group");
                }

                currentGroup = new CommandGroup();
            }
        }

        public void EndGroup()
        {
            lock (lock_)
            {
                if (currentGroup == null)
                {
                    throw new Exception("Current is not in a group");
                }

                if (currentGroup.Commands.Count != 0)
                {
                    Register(currentGroup);
                }

                currentGroup = null;
            }
        }

        public void Do(Action @do, Action @undo)
        {
            lock (lock_)
            {
                var command = new ActionCommand(@do, @do, @undo);
                Register(command);
                command.Do();
            }
        }

        public void Do(ICommand command)
        {
            lock (lock_)
            {
                Register(command);
                command.Do();
            }
        }

        public void Redo()
        {
            lock (lock_)
            {
                if (redoList.Count == 0)
                {
                    return;
                }

                var command = redoList.Pop();
                undoList.AddLast(command);

                if (command != null)
                {
                    command.Redo();
                }
            }
        }

        public void Undo()
        {
            lock (lock_)
            {
                if (undoList.Count == 0)
                {
                    return;
                }

                var command = undoList.Last.Value;
                undoList.RemoveLast();
                redoList.Push(command);

                if (command != null)
                {
                    command.Undo();
                }
            }
        }

        public void Clear()
        {
            lock (lock_)
            {
                undoList.Clear();
                redoList.Clear();
            }
        }

        internal class CommandGroup : ICommand
        {
            internal readonly List<ICommand> Commands = new List<ICommand>();

            public void Do()
            {
                for (int i = 0; i < Commands.Count; i++)
                {
                    var command = Commands[i];
                    if (command == null)
                        continue;

                    command.Do();
                }
            }

            public void Redo()
            {
                for (int i = 0; i < Commands.Count; i++)
                {
                    var command = Commands[i];
                    if (command == null)
                        continue;

                    command.Redo();
                }
            }

            public void Undo()
            {
                for (int i = Commands.Count - 1; i >= 0; i--)
                {
                    var command = Commands[i];
                    if (command == null)
                        continue;

                    command.Undo();
                }
            }
        }

        public class ActionCommand : ICommand
        {
            private Action m_Do;
            private Action m_Redo;
            private Action m_Undo;

            public ActionCommand(Action @do, Action @redo, Action @undo)
            {
                this.m_Do = @do;
                this.m_Redo = @redo;
                this.m_Undo = @undo;
            }

            public void Do()
            {
                m_Do?.Invoke();
            }

            public void Redo()
            {
                m_Redo?.Invoke();
            }

            public void Undo()
            {
                m_Undo?.Invoke();
            }
        }
    }
}
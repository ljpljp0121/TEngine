using System;
using System.Collections.Generic;

namespace PFGAS.Runtime
{
    internal sealed class GameplayEffectApplyTransaction
    {
        private readonly Stack<Action> rollbackActions = new Stack<Action>();
        private bool committed;

        public void AddRollback(Action action)
        {
            if (action != null)
            {
                rollbackActions.Push(action);
            }
        }

        public void Commit()
        {
            committed = true;
            rollbackActions.Clear();
        }

        public void Rollback()
        {
            if (committed)
            {
                return;
            }

            while (rollbackActions.Count > 0)
            {
                rollbackActions.Pop()();
            }
        }
    }
}

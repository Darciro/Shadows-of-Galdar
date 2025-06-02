using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public abstract class BaseAction : MonoBehaviour
    {
        // Static Events
        public static event EventHandler OnAnyActionStarted;
        public static event EventHandler OnAnyActionCompleted;

        // Protected Fields
        protected Unit unit;
        protected bool isActive;
        protected Action onActionComplete;

        // MonoBehaviour Lifecycle
        protected virtual void Awake()
        {
            unit = GetComponent<Unit>();
        }

        // Abstract Methods
        public abstract string GetActionName();
        public abstract void TakeAction(GridPosition gridPosition, Action onActionComplete);
        public abstract List<GridPosition> GetValidActionGridPositionList();
        public abstract EnemyAIAction GetEnemyAIAction(GridPosition gridPosition);

        // Virtual Methods
        public virtual bool IsValidActionGridPosition(GridPosition gridPosition)
        {
            return GetValidActionGridPositionList().Contains(gridPosition);
        }

        public virtual int GetActionPointsCost()
        {
            return 1;
        }

        // Action Control Flow
        protected void ActionStart(Action onActionComplete)
        {
            isActive = true;
            this.onActionComplete = onActionComplete;

            OnAnyActionStarted?.Invoke(this, EventArgs.Empty);
        }

        protected void ActionComplete()
        {
            isActive = false;
            onActionComplete?.Invoke();

            OnAnyActionCompleted?.Invoke(this, EventArgs.Empty);
        }

        // Accessors
        public Unit GetUnit()
        {
            return unit;
        }

        // AI Decision-Making
        public EnemyAIAction GetBestEnemyAIAction()
        {
            List<EnemyAIAction> actions = new List<EnemyAIAction>();

            foreach (GridPosition gridPos in GetValidActionGridPositionList())
            {
                actions.Add(GetEnemyAIAction(gridPos));
            }

            if (actions.Count == 0)
            {
                return null;
            }

            actions.Sort((a, b) => b.actionValue.CompareTo(a.actionValue));
            return actions[0];
        }
    }
}

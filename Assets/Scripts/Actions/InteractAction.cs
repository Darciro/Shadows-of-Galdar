using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public class InteractAction : BaseAction
    {
        private int maxInteractDistance = 1;

        private void Update()
        {
            if (!isActive)
            {
                return;
            }
        }

        public override string GetActionName()
        {
            return "Interact";
        }

        public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
        {
            return new EnemyAIAction
            {
                gridPosition = gridPosition,
                actionValue = 0
            };
        }

        public override List<GridPosition> GetValidActionGridPositionList()
        {
            List<GridPosition> validGridPositionList = new List<GridPosition>();

            GridPosition unitGridPosition = unit.GetGridPosition();

            for (int x = -maxInteractDistance; x <= maxInteractDistance; x++)
            {
                for (int y = -maxInteractDistance; y <= maxInteractDistance; y++)
                {
                    GridPosition offsetGridPosition = new GridPosition(x, y);
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                    if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    // Door door = LevelGrid.Instance.GetDoorAtGridPosition(testGridPosition);
                    IInteractable interactable = LevelGrid.Instance.GetInteractableAtGridPosition(testGridPosition);

                    if (interactable == null)
                    {
                        // No Door on this GridPosition
                        continue;
                    }

                    validGridPositionList.Add(testGridPosition);
                }
            }

            return validGridPositionList;
        }

        public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
        {
            // Door door = LevelGrid.Instance.GetDoorAtGridPosition(gridPosition);
            IInteractable interactable = LevelGrid.Instance.GetInteractableAtGridPosition(gridPosition);
            // door.Interact(OnInteractComplete);
            interactable.Interact(OnInteractComplete);

            ActionStart(onActionComplete);
        }

        private void OnInteractComplete()
        {
            ActionComplete();
        }

    }
}
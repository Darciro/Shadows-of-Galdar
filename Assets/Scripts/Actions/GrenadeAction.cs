using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DungeonMaster
{
    public class GrenadeAction : BaseAction
    {
        [SerializeField] private Transform grenadeProjectilePrefab;
        private int maxThrowDistance = 7;

        private void Update()
        {
            if (!isActive)
            {
                return;
            }
        }

        public override string GetActionName()
        {
            return "Grenade";
        }

        public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
        {
            return new EnemyAIAction
            {
                gridPosition = gridPosition,
                actionValue = 0,
            };
        }

        public override List<GridPosition> GetValidActionGridPositionList()
        {
            List<GridPosition> validGridPositionList = new List<GridPosition>();
            GridPosition unitGridPosition = unit.GetGridPosition();

            for (int x = -maxThrowDistance; x <= maxThrowDistance; x++)
            {
                for (int y = -maxThrowDistance; y <= maxThrowDistance; y++)
                {
                    GridPosition offsetGridPosition = new GridPosition(x, y);
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                    if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    int testDistance = Mathf.Abs(x) + Mathf.Abs(y);
                    if (testDistance > maxThrowDistance)
                    {
                        continue;
                    }

                    validGridPositionList.Add(testGridPosition);
                }
            }

            return validGridPositionList;
        }

        public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
        {
            Transform grenadeProjectileTransform = Instantiate(grenadeProjectilePrefab, unit.GetWorldPosition(), Quaternion.identity);
            GrenadeProjectile grenadeProjectile = grenadeProjectileTransform.GetComponent<GrenadeProjectile>();
            grenadeProjectile.Setup(gridPosition, OnGrenadeBehaviourComplete);

            ActionStart(onActionComplete);
        }

        private void OnGrenadeBehaviourComplete()
        {
            ActionComplete();
        }

    }
}
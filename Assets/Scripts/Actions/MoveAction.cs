using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DungeonMaster
{
    public class MoveAction : BaseAction
    {
        public event EventHandler OnStartMoving;
        public event EventHandler OnStopMoving;
        [SerializeField] private int maxMoveDistance = 4;
        private Vector3 targetPosition;

        protected override void Awake()
        {
            base.Awake();
            targetPosition = transform.position;
        }

        private void Update()
        {
            if (!isActive)
            {
                return;
            }

            float stoppingDistance = 0.1f;
            float moveSpeed = 4f;

            // Ensure we're only working in the 2D XY plane
            Vector3 currentPos = new Vector3(transform.position.x, transform.position.y, 0f);
            Vector3 targetPos = new Vector3(targetPosition.x, targetPosition.y, 0f);

            if (Vector3.Distance(currentPos, targetPos) > stoppingDistance)
            {
                Vector3 moveDirection = (targetPos - currentPos).normalized;
                transform.position += moveDirection * moveSpeed * Time.deltaTime;

                // unitAnimator.SetBool("IsWalking", true);

                // Optional: Flip sprite direction based on horizontal movement
                if (moveDirection.x != 0)
                {
                    Vector3 localScale = transform.localScale;
                    localScale.x = Mathf.Sign(moveDirection.x) * Mathf.Abs(localScale.x);
                    transform.localScale = localScale;
                }
            }
            else
            {
                // unitAnimator.SetBool("IsWalking", false);
                OnStopMoving?.Invoke(this, EventArgs.Empty);
                ActionComplete();
            }

        }

        public override void TakeAction(GridPosition gridPosition, Action onActionComplete)
        {
            ActionStart(onActionComplete);
            this.targetPosition = LevelGrid.Instance.GetWorldPosition(gridPosition);
            OnStartMoving?.Invoke(this, EventArgs.Empty);
        }

        public override List<GridPosition> GetValidActionGridPositionList()
        {
            List<GridPosition> validGridPositionList = new List<GridPosition>();

            GridPosition unitGridPosition = unit.GetGridPosition();

            for (int x = -maxMoveDistance; x <= maxMoveDistance; x++)
            {
                for (int y = -maxMoveDistance; y <= maxMoveDistance; y++)
                {
                    GridPosition offsetGridPosition = new GridPosition(x, y);
                    GridPosition testGridPosition = unitGridPosition + offsetGridPosition;

                    if (!LevelGrid.Instance.IsValidGridPosition(testGridPosition))
                    {
                        continue;
                    }

                    if (unitGridPosition == testGridPosition)
                    {
                        // Same Grid Position where the unit is already at
                        continue;
                    }

                    if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testGridPosition))
                    {
                        // Grid Position already occupied with another Unit
                        continue;
                    }

                    validGridPositionList.Add(testGridPosition);
                }
            }

            return validGridPositionList;
        }

        public override string GetActionName()
        {
            return "Move";
        }

        public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
        {
            int targetCountAtGridPosition = unit.GetAction<ShootAction>().GetTargetCountAtPosition(gridPosition);

            return new EnemyAIAction
            {
                gridPosition = gridPosition,
                actionValue = targetCountAtGridPosition * 10,
            };
        }

    }
}
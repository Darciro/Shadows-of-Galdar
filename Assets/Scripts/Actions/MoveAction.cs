using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace DungeonMaster
{
    public class MoveAction : BaseAction
    {
        public event EventHandler OnStartMoving;
        public event EventHandler OnStopMoving;

        [SerializeField] private int maxMoveDistance = 4;
        private List<Vector3> positionList;
        private int currentPositionIndex;

        public override string GetActionName() => "Move";

        public override void TakeAction(GridPosition targetGridPosition, Action onActionComplete)
        {
            Vector3 startWorld = LevelGrid.Instance.GetWorldPosition(unit.GetGridPosition());
            Vector3 targetWorld = LevelGrid.Instance.GetWorldPosition(targetGridPosition);

            // Use A* to find a path
            AstarPathfindingService.Instance.FindPath(startWorld, targetWorld, (List<Vector3> path) =>
            {
                if (path == null || path.Count == 0)
                {
                    Debug.LogWarning("No valid path to target!");
                    ActionComplete();
                    onActionComplete?.Invoke();
                    return;
                }

                currentPositionIndex = 0;
                positionList = path;
                OnStartMoving?.Invoke(this, EventArgs.Empty);
                ActionStart(onActionComplete);
            });
        }

        private void Update()
        {
            if (!isActive || positionList == null) return;

            Vector3 targetPosition = positionList[currentPositionIndex];
            float stoppingDistance = 0.1f;
            Vector3 moveDir = (targetPosition - unit.transform.position).normalized;
            float moveSpeed = 4f;

            if (Vector3.Distance(unit.transform.position, targetPosition) > stoppingDistance)
            {
                unit.transform.position += moveDir * moveSpeed * Time.deltaTime;
            }
            else
            {
                unit.transform.position = targetPosition;
                currentPositionIndex++;
                if (currentPositionIndex >= positionList.Count)
                {
                    OnStopMoving?.Invoke(this, EventArgs.Empty);
                    ActionComplete();
                }
            }
        }

        public override List<GridPosition> GetValidActionGridPositionList()
        {
            List<GridPosition> validPositions = new List<GridPosition>();
            GridPosition unitPos = unit.GetGridPosition();

            for (int dx = -maxMoveDistance; dx <= maxMoveDistance; dx++)
            {
                for (int dy = -maxMoveDistance; dy <= maxMoveDistance; dy++)
                {
                    GridPosition testPos = unitPos + new GridPosition(dx, dy);
                    if (!LevelGrid.Instance.IsValidGridPosition(testPos) || testPos == unitPos) continue;
                    if (LevelGrid.Instance.HasAnyUnitOnGridPosition(testPos)) continue;

                    Vector3 testWorld = LevelGrid.Instance.GetWorldPosition(testPos);
                    var nearestNode = AstarPath.active.GetNearest(testWorld, NNConstraint.Default).node;
                    if (nearestNode == null || !nearestNode.Walkable) continue;

                    var startNode = AstarPath.active.GetNearest(LevelGrid.Instance.GetWorldPosition(unitPos), NNConstraint.Default).node;
                    if (!PathUtilities.IsPathPossible(startNode, nearestNode)) continue;

                    var path = ABPath.Construct((Vector3)startNode.position, (Vector3)nearestNode.position, null);
                    AstarPath.StartPath(path);
                    path.BlockUntilCalculated();
                    if (path.error) continue;

                    // Path cost: 10 for straight, 14 for diagonal
                    int totalCost = 0;
                    var nodePath = path.path;
                    for (int i = 0; i < nodePath.Count - 1; i++)
                    {
                        var a = LevelGrid.Instance.GetGridPosition((Vector3)nodePath[i].position);
                        var b = LevelGrid.Instance.GetGridPosition((Vector3)nodePath[i + 1].position);
                        totalCost += (a.x != b.x && a.y != b.y) ? 14 : 10;
                    }
                    if (totalCost > maxMoveDistance * 10) continue;

                    validPositions.Add(testPos);
                }
            }
            return validPositions;
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

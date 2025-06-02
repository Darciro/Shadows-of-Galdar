using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace DungeonMaster
{
    public class LevelGrid : MonoBehaviour
    {
        public static LevelGrid Instance { get; private set; }
        public event EventHandler OnAnyUnitMovedGridPosition;

        [SerializeField] private Transform gridDebugObjectPrefab;
        [SerializeField] private int width = 30;
        [SerializeField] private int height = 30;
        [SerializeField] private float isoTileActualWidth = 1f;     // width of a single tile in world units
        [SerializeField] private float isoTileActualHeightStep = 0.5f; // vertical step (isometric grid)

        private GridSystem<GridObject> gridSystem;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            float halfWidth = isoTileActualWidth / 2f;
            float halfHeightStep = isoTileActualHeightStep / 2f;

            gridSystem = new GridSystem<GridObject>(
                width,
                height,
                isoTileActualWidth,
                (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition)
            );

            // Uncomment if you want to visualize debug grid objects
            // gridSystem.CreateDebugObjects(gridDebugObjectPrefab);
        }

        /* private void Start()
        {
            // --- A* Pathfinding GridGraph setup ---
            // NOTE: This assumes you have an AstarPath object in the scene!
            AstarData data = AstarPath.active.data;
            GridGraph grid = data.gridGraph ?? data.AddGraph(typeof(GridGraph)) as GridGraph;
            grid.width = width;
            grid.depth = height;
            grid.nodeSize = isoTileActualWidth / 2f;
            grid.center = transform.position;
            grid.rotation = new Vector3(0, 0, 45); // isometric diamond alignment
            grid.isometricAngle = GridGraph.StandardIsometricAngle;
            grid.aspectRatio = 1.0f;
            grid.neighbours = NumNeighbours.Eight;
            grid.collision.use2D = true;
            // Optionally set: grid.collision.mask = obstaclesLayerMask;

            AstarPath.active.Scan();
        }*/

        // ========== UNIT GRID LOGIC ==========

        public void AddUnitAtGridPosition(GridPosition gridPosition, Unit unit)
        {
            gridSystem.GetGridObject(gridPosition).AddUnit(unit);
        }

        public void RemoveUnitAtGridPosition(GridPosition gridPosition, Unit unit)
        {
            gridSystem.GetGridObject(gridPosition).RemoveUnit(unit);
        }

        public List<Unit> GetUnitListAtGridPosition(GridPosition gridPosition)
        {
            return gridSystem.GetGridObject(gridPosition).GetUnitList();
        }

        public void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition)
        {
            RemoveUnitAtGridPosition(fromGridPosition, unit);
            AddUnitAtGridPosition(toGridPosition, unit);
            OnAnyUnitMovedGridPosition?.Invoke(this, EventArgs.Empty);
        }

        // ========== GRID INFO/HELPERS ==========

        public GridPosition GetGridPosition(Vector3 worldPosition)
            => gridSystem.GetGridPosition(worldPosition);

        public Vector3 GetWorldPosition(GridPosition gridPosition)
            => gridSystem.GetWorldPosition(gridPosition);

        public bool IsValidGridPosition(GridPosition gridPosition)
            => gridSystem.IsValidGridPosition(gridPosition);

        public int GetWidth() => gridSystem.GetWidth();
        public int GetHeight() => gridSystem.GetHeight();

        public bool HasAnyUnitOnGridPosition(GridPosition gridPosition)
            => gridSystem.GetGridObject(gridPosition).HasAnyUnit();

        public Unit GetUnitAtGridPosition(GridPosition gridPosition)
            => gridSystem.GetGridObject(gridPosition).GetUnit();

        // ========== INTERACTABLES ==========

        public IInteractable GetInteractableAtGridPosition(GridPosition gridPosition)
            => gridSystem.GetGridObject(gridPosition).GetInteractable();

        public void SetInteractableAtGridPosition(GridPosition gridPosition, IInteractable interactable)
            => gridSystem.GetGridObject(gridPosition).SetInteractable(interactable);

        // ========== DEBUG ==========

        // Optionally, add methods for debugging or visualization as needed
    }
}

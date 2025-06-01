using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace DungeonMaster
{
    public class LevelGrid : MonoBehaviour
    {

        public static LevelGrid Instance { get; private set; }
        public event EventHandler OnAnyUnitMovedGridPosition;
        [SerializeField] private Transform gridDebugObjectPrefab;
        private GridSystem<GridObject> gridSystem;
        [SerializeField] private int width = 30;
        [SerializeField] private int height = 30;
        // [SerializeField] private float cellSize = .5f; // Remove or repurpose
        [SerializeField] private float isoTileActualWidth = 1f; // Example: Width of your tile sprite (e.g., 64 pixels if 1 unit = 1 pixel)
        [SerializeField] private float isoTileActualHeightStep = 0.5f; // Example: Effective vertical distance between tile centers (e.g., 32 pixels)


        /* private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            gridSystem = new GridSystem<GridObject>(
                width, height, cellSize,
                (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition)
            );
        } */

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Calculate half dimensions to pass to GridSystem
            float halfWidth = isoTileActualWidth / 2f;
            float halfHeightStep = isoTileActualHeightStep / 2f;

            gridSystem = new GridSystem<GridObject>(
                width, height, halfWidth, halfHeightStep, // Pass new dimensions
                (GridSystem<GridObject> g, GridPosition gridPosition) => new GridObject(g, gridPosition)
            );
            // gridSystem.CreateDebugObjects(gridDebugObjectPrefab); // This will now place debug objects isometrically
        }

        /* private void Start()
        {
            Pathfinding.Instance.Setup(width, height, cellSize);
        } */

        private void Start()
        {
            // Pathfinding Setup needs world-space cell size for raycasting against obstacles.
            // This might need careful consideration. If obstacles are aligned with the isometric grid,
            // the existing raycast logic in Pathfinding.cs might still work conceptually,
            // as it uses LevelGrid.GetWorldPosition for the raycast origin.
            // The 'cellSize' for pathfinding's internal grid representation can remain abstract,
            // but if it's used for any world calculations, it should be the isometric step.
            Pathfinding.Instance.Setup(width, height, isoTileActualWidth, isoTileActualHeightStep); // Pass the new dimension fields
        }

        public void AddUnitAtGridPosition(GridPosition gridPosition, Unit unit)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);
            gridObject.AddUnit(unit);
        }

        public List<Unit> GetUnitListAtGridPosition(GridPosition gridPosition)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);
            return gridObject.GetUnitList();
        }

        public void RemoveUnitAtGridPosition(GridPosition gridPosition, Unit unit)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);
            gridObject.RemoveUnit(unit);
        }

        public void UnitMovedGridPosition(Unit unit, GridPosition fromGridPosition, GridPosition toGridPosition)
        {
            RemoveUnitAtGridPosition(fromGridPosition, unit);
            AddUnitAtGridPosition(toGridPosition, unit);
            OnAnyUnitMovedGridPosition?.Invoke(this, EventArgs.Empty);
        }

        public GridPosition GetGridPosition(Vector3 worldPosition) => gridSystem.GetGridPosition(worldPosition);

        public Vector3 GetWorldPosition(GridPosition gridPosition) => gridSystem.GetWorldPosition(gridPosition);

        public bool IsValidGridPosition(GridPosition gridPosition) => gridSystem.IsValidGridPosition(gridPosition);

        public int GetWidth() => gridSystem.GetWidth();

        public int GetHeight() => gridSystem.GetHeight();

        public bool HasAnyUnitOnGridPosition(GridPosition gridPosition)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);
            return gridObject.HasAnyUnit();
        }

        public Unit GetUnitAtGridPosition(GridPosition gridPosition)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);
            return gridObject.GetUnit();
        }

        public IInteractable GetInteractableAtGridPosition(GridPosition gridPosition)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);
            return gridObject.GetInteractable();
        }

        public void SetInteractableAtGridPosition(GridPosition gridPosition, IInteractable interactable)
        {
            GridObject gridObject = gridSystem.GetGridObject(gridPosition);
            gridObject.SetInteractable(interactable);
        }

    }
}
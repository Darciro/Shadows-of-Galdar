using System;
using UnityEngine;

namespace DungeonMaster
{
    public class GridSystem<TGridObject>
    {
        private int width;
        private int height;
        private float cellSize;
        private TGridObject[,] gridObjectArray;

        public GridSystem(int width, int height, float cellSize, Func<GridSystem<TGridObject>, GridPosition, TGridObject> createGridObject)
        {
            this.width = width;
            this.height = height;
            this.cellSize = cellSize;

            gridObjectArray = new TGridObject[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    gridObjectArray[x, y] = createGridObject(this, new GridPosition(x, y));
        }

        public Vector3 GetWorldPosition(GridPosition gridPosition)
        {
            // Isometric diamond mapping: X+Y for X, Y-X for Y (adjust as needed for your layout)
            float worldX = (gridPosition.x - gridPosition.y) * (cellSize / 2f);
            float worldY = (gridPosition.x + gridPosition.y) * (cellSize / 4f);
            return new Vector3(worldX, worldY, 0);
        }

        public GridPosition GetGridPosition(Vector3 worldPosition)
        {
            // Inverse isometric math. Adjust as needed for your setup.
            int x = Mathf.RoundToInt((worldPosition.x / (cellSize / 2f) + worldPosition.y / (cellSize / 4f)) / 2f);
            int y = Mathf.RoundToInt((worldPosition.y / (cellSize / 4f) - worldPosition.x / (cellSize / 2f)) / 2f);
            return new GridPosition(x, y);
        }

        public bool IsValidGridPosition(GridPosition gridPosition)
        {
            return gridPosition.x >= 0 && gridPosition.y >= 0 && gridPosition.x < width && gridPosition.y < height;
        }

        public TGridObject GetGridObject(GridPosition gridPosition)
        {
            return gridObjectArray[gridPosition.x, gridPosition.y];
        }

        public int GetWidth()
        {
            return width;
        }

        public int GetHeight()
        {
            return height;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public class GridSystem<TGridObject>
    {
        private int width;
        private int height;
        private float cellSize;
        private float isoTileHalfWidth;
        private float isoTileHalfHeightStep;
        private TGridObject[,] gridObjectArray;

        public GridSystem(int width, int height, float isoTileHalfWidth, float isoTileHalfHeightStep, Func<GridSystem<TGridObject>, GridPosition, TGridObject> createGridObject)
        {
            this.width = width;
            this.height = height;
            this.isoTileHalfWidth = isoTileHalfWidth;
            this.isoTileHalfHeightStep = isoTileHalfHeightStep;

            gridObjectArray = new TGridObject[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridPosition gridPosition = new GridPosition(x, y);
                    gridObjectArray[x, y] = createGridObject(this, gridPosition);

                }
            }
        }

        public Vector3 GetWorldPosition(GridPosition gridPosition)
        {
            float worldX = (gridPosition.x - gridPosition.y) * this.isoTileHalfWidth;
            float worldY = (gridPosition.x + gridPosition.y) * this.isoTileHalfHeightStep;
            return new Vector3(worldX, worldY); // Assuming Z=0 for now
        }


        public GridPosition GetGridPosition(Vector3 worldPosition)
        {
            float xMinusY = worldPosition.x / this.isoTileHalfWidth;
            float xPlusY = worldPosition.y / this.isoTileHalfHeightStep;

            float gridXFloat = (xMinusY + xPlusY) / 2f;
            float gridYFloat = (xPlusY - xMinusY) / 2f;

            return new GridPosition(
                Mathf.RoundToInt(gridXFloat),
                Mathf.RoundToInt(gridYFloat)
            );
        }

        public void CreateDebugObjects(Transform debugPrefab)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridPosition gridPosition = new GridPosition(x, y);

                    Transform debugTransform = GameObject.Instantiate(debugPrefab, GetWorldPosition(gridPosition), Quaternion.identity);
                    GridDebugObject gridDebugObject = debugTransform.GetComponent<GridDebugObject>();
                    gridDebugObject.SetGridObject(GetGridObject(gridPosition));
                }
            }
        }

        public TGridObject GetGridObject(GridPosition gridPosition)
        {
            return gridObjectArray[gridPosition.x, gridPosition.y];
        }

        public bool IsValidGridPosition(GridPosition gridPosition)
        {
            return gridPosition.x >= 0 &&
                    gridPosition.y >= 0 &&
                    gridPosition.x < width &&
                    gridPosition.y < height;
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
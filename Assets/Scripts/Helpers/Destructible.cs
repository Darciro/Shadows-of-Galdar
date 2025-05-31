using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public class Destructible : MonoBehaviour
    {
        public static event EventHandler OnAnyDestroyed;
        private GridPosition gridPosition;
        [SerializeField] private Transform destroyedPrefab;

        private void Start()
        {
            gridPosition = LevelGrid.Instance.GetGridPosition(transform.position);
        }

        public GridPosition GetGridPosition()
        {
            return gridPosition;
        }

        public void Damage()
        {
            Transform destroyedTransform = Instantiate(destroyedPrefab, transform.position, transform.rotation);
            Destroy(gameObject);
            OnAnyDestroyed?.Invoke(this, EventArgs.Empty);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonMaster
{
    public class PathfindingUpdater : MonoBehaviour
    {
        private void Start()
        {
            Destructible.OnAnyDestroyed += Destructible_OnAnyDestroyed;
        }

        private void Destructible_OnAnyDestroyed(object sender, EventArgs e)
        {
            Destructible destructible = sender as Destructible;
            Pathfinding.Instance.SetIsWalkableGridPosition(destructible.GetGridPosition(), true);
            Debug.Log($"[PathfindingUpdater] Destructible_OnAnyDestroyed: {destructible.GetGridPosition()}");
        }
    }
}

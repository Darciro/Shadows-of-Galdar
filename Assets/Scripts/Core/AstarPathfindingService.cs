using System;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

namespace DungeonMaster
{
    public class AstarPathfindingService : MonoBehaviour
    {
        public static AstarPathfindingService Instance { get; private set; }
        private Seeker seeker;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(this);
            else Instance = this;
            seeker = GetComponent<Seeker>();
            if (seeker == null)
                seeker = gameObject.AddComponent<Seeker>();
        }

        public void FindPath(Vector3 start, Vector3 end, Action<List<Vector3>> callback)
        {
            seeker.StartPath(start, end, (Path p) =>
            {
                if (p.error)
                {
                    callback?.Invoke(null);
                }
                else
                {
                    callback?.Invoke(p.vectorPath);
                }
            });
        }
    }
}

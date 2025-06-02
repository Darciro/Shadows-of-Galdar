using UnityEngine;
using Pathfinding;
using System;
using System.Collections.Generic;

public class AstarPathfindingService : MonoBehaviour
{
    public static AstarPathfindingService Instance;
    private Seeker seeker;
    private void Awake()
    {
        Instance = this;
        seeker = GetComponent<Seeker>();
    }
    public void FindPath(Vector3 startWorldPos, Vector3 endWorldPos, Action<List<Vector3>> onPathComplete)
    {
        seeker.StartPath(startWorldPos, endWorldPos, path =>
        {
            if (!path.error) onPathComplete?.Invoke(path.vectorPath);
            else onPathComplete?.Invoke(null);
        });
    }
}

using UnityEngine;
using Pathfinding;

public class Door : MonoBehaviour
{
    [SerializeField] private Collider2D doorCollider;
    private bool isOpen;

    public void Open()
    {
        isOpen = true;
        doorCollider.enabled = false;
        UpdateAstarNode(true);
    }

    public void Close()
    {
        isOpen = false;
        doorCollider.enabled = true;
        UpdateAstarNode(false);
    }

    private void UpdateAstarNode(bool walkable)
    {
        var guo = new GraphUpdateObject(new Bounds(transform.position, Vector3.one * 0.5f));
        guo.modifyWalkability = true;
        guo.setWalkability = walkable;
        AstarPath.active.UpdateGraphs(guo);
    }
}

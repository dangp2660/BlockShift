using UnityEngine;

public class GridCell : MonoBehaviour
{
    public int x;
    public int y;
    public bool isOccupied = false;
    public GameObject currentCube;

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);
    }
}

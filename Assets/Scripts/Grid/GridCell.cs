using UnityEngine;

public class GridCell : MonoBehaviour
{
    public int x;
    public int y;
    public bool isOccupied = false;
    public BlockHolder currentHolder;
    public Vector2Int[,] subCell; 

    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}

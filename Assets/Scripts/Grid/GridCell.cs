using UnityEngine;

public class GridCell : MonoBehaviour
{
    public int x;
    public int y;
    public bool isOccupied = false;
    public BlockHolder currentHolder;
    public Vector2Int[,] subCell; 
    public int[,] occupancy = new int[2,2];
    public Block[,] subCellBlocks = new Block[2,2];

    private void Awake()
    {
        // Defensive init in case of serialization/reset
        if (occupancy == null) occupancy = new int[2,2];
        if (subCellBlocks == null) subCellBlocks = new Block[2,2];
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}

using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Block : MonoBehaviour
{
    public GridCell currentCell;
    public int blockID;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Update()
    {
        findCurrentCell();
    }

    public void findCurrentCell()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit))
        {
            GridCell cell = hit.collider.gameObject.GetComponent<GridCell>();
            if (cell != null)
            {
                if (!cell.isOccupied)
                {
                    currentCell = cell;
                    cell.isOccupied = true;
                    cell.currentCube = gameObject;
                    Debug.Log($"{name} in cell {cell.x}-{cell.y}");
                }
            }
        }
    }//findCurrentCell

    private void OnDestroy()
    {
        if (currentCell != null)
        {
            currentCell.isOccupied = false;
        }
    }

    private void OnDisable()
    {
        if (currentCell != null)
        {
            currentCell.isOccupied = false;
        }
    }
}

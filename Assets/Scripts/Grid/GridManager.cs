using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GridCell[,] gridCells;
    public int width;
    public int height;

    void Awake()
    {
        LoadExistingGrid();
    }


    public void LoadExistingGrid()
    {
        GridCell[] allCells = FindObjectsOfType<GridCell>();
        if (allCells.Length == 0)
        {
            Debug.LogWarning("Không có GridCell nào trong scene!");
            return;
        }

        // Tính kích thước lưới dựa vào tọa độ các cell
        int maxX = 0, maxY = 0;
        foreach (var c in allCells)
        {
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }

        width = maxX + 1;
        height = maxY + 1;

        gridCells = new GridCell[width, height];

        foreach (var c in allCells)
        {
            gridCells[c.x, c.y] = c;
        }

        Debug.Log($"Tải grid hoàn tất ({width}x{height}) với {allCells.Length} cell.");
    }

    public GridCell GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return null;
        return gridCells[x, y];
    }

    public void RefreshGrid()
    {
        foreach (var cell in gridCells)
        {
            if (cell == null) continue;
            Renderer renderer = cell.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = !cell.isOccupied;
            }
        }
    }
}
